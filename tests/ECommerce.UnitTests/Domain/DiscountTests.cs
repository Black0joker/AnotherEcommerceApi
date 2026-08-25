using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.UnitTests.Domain;

public class DiscountTests
{
    private static Discount CreateDiscount(
        DiscountType type = DiscountType.Percentage,
        decimal value = 10m,
        bool isActive = true,
        DateTime? startsAt = null,
        DateTime? endsAt = null,
        int? usageLimit = null,
        int usedCount = 0,
        decimal? minimumOrderValue = null) => new()
    {
        Code = "TEST",
        Name = "Test discount",
        Type = type,
        Value = value,
        IsActive = isActive,
        StartsAt = startsAt ?? DateTime.UtcNow.AddDays(-1),
        EndsAt = endsAt,
        UsageLimit = usageLimit,
        UsedCount = usedCount,
        MinimumOrderValue = minimumOrderValue
    };

    [Fact]
    public void IsValid_True_For_Active_Discount_Within_Dates_And_Usage()
    {
        var discount = CreateDiscount(usageLimit: 100, usedCount: 5);

        Assert.True(discount.IsValid());
    }

    [Fact]
    public void IsValid_False_When_Inactive()
    {
        Assert.False(CreateDiscount(isActive: false).IsValid());
    }

    [Fact]
    public void IsValid_False_Before_Start_Date()
    {
        var discount = CreateDiscount(startsAt: DateTime.UtcNow.AddDays(1));

        Assert.False(discount.IsValid());
    }

    [Fact]
    public void IsValid_False_After_End_Date()
    {
        var discount = CreateDiscount(endsAt: DateTime.UtcNow.AddDays(-1));

        Assert.False(discount.IsValid());
    }

    [Fact]
    public void IsValid_False_When_Usage_Limit_Reached()
    {
        var discount = CreateDiscount(usageLimit: 10, usedCount: 10);

        Assert.False(discount.IsValid());
    }

    [Fact]
    public void IsValid_Uses_Explicit_Order_Date()
    {
        var discount = CreateDiscount(startsAt: DateTime.UtcNow.AddDays(-10), endsAt: DateTime.UtcNow.AddDays(-5));

        Assert.True(discount.IsValid(DateTime.UtcNow.AddDays(-7)));
        Assert.False(discount.IsValid(DateTime.UtcNow));
    }

    [Theory]
    [InlineData(100.0, true)]
    [InlineData(50.0, true)]
    [InlineData(49.99, false)]
    public void MeetsMinimumOrder_Compares_Subtotal(decimal subtotal, bool expected)
    {
        var discount = CreateDiscount(minimumOrderValue: 50m);

        Assert.Equal(expected, discount.MeetsMinimumOrder(subtotal));
    }

    [Fact]
    public void MeetsMinimumOrder_True_When_No_Minimum()
    {
        var discount = CreateDiscount(minimumOrderValue: null);

        Assert.True(discount.MeetsMinimumOrder(1m));
    }

    [Fact]
    public void CalculateDiscount_Percentage_Applies_Percent_Of_Amount()
    {
        var discount = CreateDiscount(DiscountType.Percentage, value: 15m);

        Assert.Equal(15m, discount.CalculateDiscount(100m));
    }

    [Fact]
    public void CalculateDiscount_Fixed_Caps_At_Amount()
    {
        var discount = CreateDiscount(DiscountType.Fixed, value: 20m);

        Assert.Equal(20m, discount.CalculateDiscount(50m));
        Assert.Equal(10m, discount.CalculateDiscount(10m)); // never discounts below the line total
    }

    [Fact]
    public void IncrementUsage_Increments_Counter()
    {
        var discount = CreateDiscount(usedCount: 0);

        discount.IncrementUsage();
        discount.IncrementUsage();

        Assert.Equal(2, discount.UsedCount);
    }
}
