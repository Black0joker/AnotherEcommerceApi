using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;

namespace ECommerce.UnitTests.Domain;

public class ReviewAndCartTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void ValidateRating_Accepts_1_Through_5(int rating)
    {
        var exception = Record.Exception(() => Review.ValidateRating(rating));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void ValidateRating_Rejects_Out_Of_Range_Values(int rating)
    {
        Assert.Throws<BusinessRuleViolationException>(() => Review.ValidateRating(rating));
    }

    [Fact]
    public void Empty_Cart_IsEmpty_And_Has_Zero_Totals()
    {
        var cart = new Cart { UserId = Guid.NewGuid() };

        Assert.True(cart.IsEmpty);
        Assert.Equal(0, cart.TotalItems);
        Assert.Equal(0m, cart.Subtotal);
    }

    [Fact]
    public void Cart_Subtotal_And_TotalItems_Computed_Server_Side()
    {
        var cart = new Cart { UserId = Guid.NewGuid() };
        cart.Items.Add(new CartItem { ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice = 10.50m });
        cart.Items.Add(new CartItem { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice = 4.00m });

        Assert.False(cart.IsEmpty);
        Assert.Equal(3, cart.TotalItems);
        Assert.Equal(25.00m, cart.Subtotal);
    }
}
