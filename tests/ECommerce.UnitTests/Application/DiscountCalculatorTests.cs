using ECommerce.Application.Abstractions;
using ECommerce.Application.Pricing;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.UnitTests.Application;

/// <summary>
/// Hand-rolled fake repository (no mocking framework required).
/// </summary>
internal sealed class FakeDiscountRepository : IDiscountRepository
{
    private readonly Dictionary<string, Discount> _byCode = new(StringComparer.OrdinalIgnoreCase);

    public void Add(Discount discount) => _byCode[discount.Code] = discount;

    public Task<Discount?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => Task.FromResult(_byCode.TryGetValue(code, out var discount) ? discount : null);

    public Task<IReadOnlyList<Discount>> GetActiveDiscountsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Discount>>(_byCode.Values.Where(d => d.IsActive).ToList());

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
        => Task.FromResult(_byCode.ContainsKey(code));

    // Unused IRepository members.
    public Task<Discount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Discount?>(null);
    public Task<IReadOnlyList<Discount>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Discount>>(_byCode.Values.ToList());
    public Task AddAsync(Discount entity, CancellationToken cancellationToken = default) { Add(entity); return Task.CompletedTask; }
    public void Update(Discount entity) { }
    public void Delete(Discount entity) { }
}

public class DiscountCalculatorTests
{
    private readonly FakeDiscountRepository _repository = new();
    private readonly DiscountCalculator _calculator;

    public DiscountCalculatorTests()
    {
        _calculator = new DiscountCalculator(_repository);
    }

    private static PricingLine Line(decimal unitPrice, int quantity, Guid? productId = null, params Guid[] categoryIds)
        => new(productId ?? Guid.NewGuid(), categoryIds, unitPrice, quantity);

    private static Discount PercentageDiscount(decimal percent, decimal? minimumOrderValue = null)
        => new()
        {
            Code = "PCT",
            Name = "Percentage",
            Type = DiscountType.Percentage,
            Value = percent,
            IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            MinimumOrderValue = minimumOrderValue
        };

    [Fact]
    public async Task No_Discount_Code_Returns_Straight_Breakdown()
    {
        var lines = new[] { Line(10m, 2), Line(5m, 1) };

        var result = await _calculator.CalculateAsync(lines, discountCode: null, taxAmount: 3m, shippingAmount: 2m);

        Assert.True(result.IsSuccess);
        Assert.Equal(25m, result.Value.Subtotal);
        Assert.Equal(0m, result.Value.DiscountAmount);
        Assert.Equal(3m, result.Value.TaxAmount);
        Assert.Equal(2m, result.Value.ShippingAmount);
        Assert.Equal(30m, result.Value.GrandTotal);
        Assert.Null(result.Value.AppliedDiscountCode);
    }

    [Fact]
    public async Task Unknown_Discount_Code_Fails_Validation()
    {
        var lines = new[] { Line(10m, 1) };

        var result = await _calculator.CalculateAsync(lines, "DOES-NOT-EXIST");

        Assert.True(result.IsFailure);
        Assert.Equal("Discount.NotFound", result.Error!.Code);
    }

    [Fact]
    public async Task Percentage_Discount_Applies_To_Entire_Order_Without_Restrictions()
    {
        _repository.Add(PercentageDiscount(10m));
        var lines = new[] { Line(100m, 1) };

        var result = await _calculator.CalculateAsync(lines, "PCT");

        Assert.True(result.IsSuccess);
        Assert.Equal(100m, result.Value.Subtotal);
        Assert.Equal(10m, result.Value.DiscountAmount);
        Assert.Equal(90m, result.Value.GrandTotal);
        Assert.Equal("PCT", result.Value.AppliedDiscountCode);
    }

    [Fact]
    public async Task Inactive_Discount_Is_Rejected()
    {
        var discount = PercentageDiscount(10m);
        discount.IsActive = false;
        _repository.Add(discount);

        var result = await _calculator.CalculateAsync(new[] { Line(100m, 1) }, "PCT");

        Assert.True(result.IsFailure);
        Assert.Equal("Discount.Invalid", result.Error!.Code);
    }

    [Fact]
    public async Task Expired_Discount_Is_Rejected()
    {
        var discount = PercentageDiscount(10m);
        discount.EndsAt = DateTime.UtcNow.AddDays(-1);
        _repository.Add(discount);

        var result = await _calculator.CalculateAsync(new[] { Line(100m, 1) }, "PCT");

        Assert.True(result.IsFailure);
        Assert.Equal("Discount.Invalid", result.Error!.Code);
    }

    [Fact]
    public async Task Usage_Limit_Exhausted_Is_Rejected()
    {
        var discount = PercentageDiscount(10m);
        discount.UsageLimit = 5;
        discount.UsedCount = 5;
        _repository.Add(discount);

        var result = await _calculator.CalculateAsync(new[] { Line(100m, 1) }, "PCT");

        Assert.True(result.IsFailure);
        Assert.Equal("Discount.Invalid", result.Error!.Code);
    }

    [Fact]
    public async Task Minimum_Order_Value_Not_Met_Is_Rejected()
    {
        _repository.Add(PercentageDiscount(10m, minimumOrderValue: 200m));

        var result = await _calculator.CalculateAsync(new[] { Line(100m, 1) }, "PCT");

        Assert.True(result.IsFailure);
        Assert.Equal("Discount.MinimumOrderNotMet", result.Error!.Code);
    }

    [Fact]
    public async Task Fixed_Discount_Never_Exceeds_Eligible_Subtotal()
    {
        _repository.Add(new Discount
        {
            Code = "BIG",
            Name = "Big fixed",
            Type = DiscountType.Fixed,
            Value = 500m,
            IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(-1)
        });

        var result = await _calculator.CalculateAsync(new[] { Line(100m, 1) }, "BIG");

        Assert.True(result.IsSuccess);
        Assert.Equal(100m, result.Value.DiscountAmount); // capped at eligible subtotal
        Assert.Equal(0m, result.Value.GrandTotal);        // never negative
    }

    [Fact]
    public async Task Product_Restriction_Limits_Discount_To_Matching_Lines()
    {
        var restrictedProductId = Guid.NewGuid();
        var discount = PercentageDiscount(50m);
        discount.DiscountProducts.Add(new DiscountProduct { Discount = discount, ProductId = restrictedProductId });
        _repository.Add(discount);

        var lines = new[]
        {
            Line(100m, 1, productId: restrictedProductId), // eligible: 100
            Line(100m, 1)                                    // not eligible
        };

        var result = await _calculator.CalculateAsync(lines, "PCT");

        Assert.True(result.IsSuccess);
        Assert.Equal(200m, result.Value.Subtotal);
        Assert.Equal(50m, result.Value.DiscountAmount); // 50% of eligible 100 only
        Assert.Equal(150m, result.Value.GrandTotal);
    }

    [Fact]
    public async Task Category_Restriction_Limits_Discount_To_Matching_Lines()
    {
        var eligibleCategoryId = Guid.NewGuid();
        var discount = PercentageDiscount(25m);
        discount.DiscountCategories.Add(new DiscountCategory { Discount = discount, CategoryId = eligibleCategoryId });
        _repository.Add(discount);

        var lines = new[]
        {
            Line(80m, 1, categoryIds: new[] { eligibleCategoryId }), // eligible
            Line(20m, 1)                                               // not eligible
        };

        var result = await _calculator.CalculateAsync(lines, "PCT");

        Assert.True(result.IsSuccess);
        Assert.Equal(100m, result.Value.Subtotal);
        Assert.Equal(20m, result.Value.DiscountAmount); // 25% of 80
        Assert.Equal(80m, result.Value.GrandTotal);
    }

    [Fact]
    public async Task Grand_Total_Includes_Tax_And_Shipping_After_Discount()
    {
        _repository.Add(PercentageDiscount(10m));

        var result = await _calculator.CalculateAsync(
            new[] { Line(100m, 1) }, "PCT", taxAmount: 7m, shippingAmount: 5m);

        Assert.True(result.IsSuccess);
        Assert.Equal(10m, result.Value.DiscountAmount);
        Assert.Equal(102m, result.Value.GrandTotal); // 100 - 10 + 7 + 5
    }
}
