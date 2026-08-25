using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Jobs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Orders.Checkout;

public class CheckoutCommandHandler : ICommandHandler<CheckoutCommand, CheckoutResultDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobQueue _jobQueue;

    public CheckoutCommandHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository,
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IBackgroundJobQueue jobQueue)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _jobQueue = jobQueue;
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

        // 4. Load authoritative product data and validate prices
        var orderItems = new List<OrderItem>();
        decimal subtotal = 0;

        foreach (var cartItem in cart.Items)
        {
            var product = await _productRepository.GetByIdAsync(cartItem.ProductId, cancellationToken);

            if (product is null || !product.IsActive)
            {
                return Result.Failure<CheckoutResultDto>(Error.Validation(
                    "Checkout.ProductUnavailable",
                    $"Product '{cartItem.Product?.Name ?? cartItem.ProductId.ToString()}' is no longer available."));
            }

            // Validate inventory
            var inventory = await _inventoryRepository.GetByProductIdAsync(product.Id, cancellationToken);
            if (inventory is null || !inventory.HasAvailableStock(cartItem.Quantity))
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
            subtotal += lineTotal;
        }

        // 5. Reserve inventory (atomic within transaction)
        foreach (var cartItem in cart.Items)
        {
            var inventory = await _inventoryRepository.GetByProductIdAsync(cartItem.ProductId, cancellationToken);
            if (inventory is not null)
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
            Subtotal = subtotal,
            DiscountAmount = 0,
            TaxAmount = 0,
            ShippingAmount = 0,
            GrandTotal = subtotal,
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
