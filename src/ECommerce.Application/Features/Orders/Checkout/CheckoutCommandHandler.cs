using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Jobs;
using ECommerce.Application.Pricing;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Orders.Checkout;

public class CheckoutCommandHandler : ICommandHandler<CheckoutCommand, CheckoutResultDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IDiscountRepository _discountRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobQueue _jobQueue;
    private readonly IDiscountCalculator _discountCalculator;

    public CheckoutCommandHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository,
        IOrderRepository orderRepository,
        IDiscountRepository discountRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IBackgroundJobQueue jobQueue,
        IDiscountCalculator discountCalculator)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _orderRepository = orderRepository;
        _discountRepository = discountRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _jobQueue = jobQueue;
        _discountCalculator = discountCalculator;
    }

    public async Task<Result<CheckoutResultDto>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate authentication
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return Result.Failure<CheckoutResultDto>(Error.Unauthorized(
                "Checkout.Unauthorized",
                "You must be authenticated to checkout."));
        }

        // 2. Check idempotency - return existing result if key was already processed
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingOrder = await _orderRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
            if (existingOrder is not null)
            {
                var existingDto = new CheckoutResultDto(
                    existingOrder.Id,
                    existingOrder.OrderNumber,
                    existingOrder.Status,
                    existingOrder.Subtotal,
                    existingOrder.DiscountAmount,
                    existingOrder.TaxAmount,
                    existingOrder.ShippingAmount,
                    existingOrder.GrandTotal,
                    existingOrder.Items.Count,
                    existingOrder.CreatedAt);
                return Result.Success(existingDto);
            }
        }

        // 3. Validate cart exists and is not empty
        var cart = await _cartRepository.GetWithItemsByUserIdAsync(userId.Value, cancellationToken);
        if (cart is null || cart.IsEmpty)
        {
            return Result.Failure<CheckoutResultDto>(Error.Validation(
                "Checkout.EmptyCart",
                "Your cart is empty. Add items before checking out."));
        }

        // 4. Batch-load authoritative product and inventory data - one query
        // each regardless of cart size (previously 2 round-trips per cart line,
        // with heavy Reviews/OrderItems includes on every product fetch).
        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();

        var productsById = (await _productRepository.GetByIdsAsync(productIds, cancellationToken))
            .ToDictionary(p => p.Id);

        var inventoryByProductId = (await _inventoryRepository.GetByProductIdsAsync(productIds, cancellationToken))
            .ToDictionary(i => i.ProductId);

        var orderItems = new List<OrderItem>();
        var pricingLines = new List<PricingLine>();

        foreach (var cartItem in cart.Items)
        {
            if (!productsById.TryGetValue(cartItem.ProductId, out var product) || !product.IsActive)
            {
                return Result.Failure<CheckoutResultDto>(Error.Validation(
                    "Checkout.ProductUnavailable",
                    $"Product '{cartItem.Product?.Name ?? cartItem.ProductId.ToString()}' is no longer available."));
            }

            // Validate inventory
            if (!inventoryByProductId.TryGetValue(product.Id, out var inventory) || !inventory.HasAvailableStock(cartItem.Quantity))
            {
                return Result.Failure<CheckoutResultDto>(Error.Validation(
                    "Checkout.InsufficientInventory",
                    $"Insufficient stock for product '{product.Name}'. Requested: {cartItem.Quantity}, Available: {inventory?.AvailableQuantity ?? 0}."));
            }

            // Use server-side price (never trust client)
            var unitPrice = product.Price;
            var lineTotal = unitPrice * cartItem.Quantity;

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                SKU = product.SKU,
                UnitPrice = unitPrice,
                Quantity = cartItem.Quantity,
                DiscountAmount = 0, // Discounts applied later
                Total = lineTotal
            };

            orderItems.Add(orderItem);

            // Capture category IDs for discount product/category restrictions.
            var categoryIds = product.ProductCategories.Select(pc => pc.CategoryId).ToList();
            pricingLines.Add(new PricingLine(product.Id, categoryIds, unitPrice, cartItem.Quantity));
        }

        // 4b. Calculate authoritative totals server-side (never trust client totals).
        var pricingResult = await _discountCalculator.CalculateAsync(
            pricingLines,
            request.DiscountCode,
            taxAmount: 0m,
            shippingAmount: 0m,
            cancellationToken);

        if (pricingResult.IsFailure)
        {
            return Result.Failure<CheckoutResultDto>(pricingResult.Error!);
        }

        var pricing = pricingResult.Value;

        // 5. Reserve inventory (atomic within transaction). Reuses the
        // inventory items already loaded in step 4 - no extra round-trips.
        foreach (var cartItem in cart.Items)
        {
            if (inventoryByProductId.TryGetValue(cartItem.ProductId, out var inventory))
            {
                inventory.Reserve(cartItem.Quantity);

                // Create audit trail
                var transaction = new InventoryTransaction
                {
                    InventoryItemId = inventory.Id,
                    ProductId = cartItem.ProductId,
                    Type = InventoryTransactionType.Reservation,
                    Quantity = -cartItem.Quantity,
                    Reference = "Checkout",
                    Notes = $"Reserved for checkout"
                };

                await _inventoryRepository.AddTransactionAsync(transaction, cancellationToken);
                _inventoryRepository.Update(inventory);
            }
        }

        // 6. Create Order
        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var order = new Order
        {
            UserId = userId.Value,
            OrderNumber = orderNumber,
            Status = OrderStatus.Pending,
            Subtotal = pricing.Subtotal,
            DiscountAmount = pricing.DiscountAmount,
            TaxAmount = pricing.TaxAmount,
            ShippingAmount = pricing.ShippingAmount,
            GrandTotal = pricing.GrandTotal,
            IdempotencyKey = request.IdempotencyKey,
            Items = orderItems,
            ShippingAddress = new OrderAddress
            {
                FirstName = request.ShippingAddress.FirstName,
                LastName = request.ShippingAddress.LastName,
                StreetLine1 = request.ShippingAddress.StreetLine1,
                StreetLine2 = request.ShippingAddress.StreetLine2,
                City = request.ShippingAddress.City,
                State = request.ShippingAddress.State,
                PostalCode = request.ShippingAddress.PostalCode,
                Country = request.ShippingAddress.Country,
                PhoneNumber = request.ShippingAddress.PhoneNumber
            }
        };

        await _orderRepository.AddAsync(order, cancellationToken);

        // 6b. Record discount usage (atomic within the same transaction).
        if (!string.IsNullOrWhiteSpace(pricing.AppliedDiscountCode))
        {
            var appliedDiscount = await _discountRepository.GetByCodeAsync(pricing.AppliedDiscountCode, cancellationToken);
            if (appliedDiscount is not null)
            {
                appliedDiscount.IncrementUsage();
                _discountRepository.Update(appliedDiscount);
            }
        }

        // 7. Clear cart
        cart.Items.Clear();
        _cartRepository.Update(cart);

        // 8. Commit transaction (atomic - all or nothing)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 9. Enqueue order confirmation email (background job, does not block response)
        await _jobQueue.EnqueueAsync(new SendOrderConfirmationJob(order.Id), cancellationToken);

        var resultDto = new CheckoutResultDto(
            order.Id,
            order.OrderNumber,
            order.Status,
            order.Subtotal,
            order.DiscountAmount,
            order.TaxAmount,
            order.ShippingAmount,
            order.GrandTotal,
            order.Items.Count,
            order.CreatedAt);

        return Result.Success(resultDto);
    }
}
