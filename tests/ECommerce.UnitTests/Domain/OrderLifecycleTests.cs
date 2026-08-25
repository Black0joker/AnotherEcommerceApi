using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;

namespace ECommerce.UnitTests.Domain;

public class OrderLifecycleTests
{
    private static Order CreateOrder(OrderStatus status = OrderStatus.Pending) => new()
    {
        UserId = Guid.NewGuid(),
        OrderNumber = $"ORD-{Guid.NewGuid():N}",
        Status = status
    };

    [Theory]
    [InlineData(OrderStatus.Pending, true)]
    [InlineData(OrderStatus.Confirmed, true)]
    [InlineData(OrderStatus.Processing, false)]
    [InlineData(OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Cancelled, false)]
    public void CanCancel_Only_For_Pending_And_Confirmed(OrderStatus status, bool expected)
    {
        var order = CreateOrder(status);

        Assert.Equal(expected, order.CanCancel);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    public void Cancel_Sets_Cancelled_State(OrderStatus status)
    {
        var order = CreateOrder(status);

        order.Cancel("customer request");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.NotNull(order.CancelledAt);
        Assert.Equal("customer request", order.CancellationReason);
    }

    [Fact]
    public void Cancel_Shipped_Order_Throws_BusinessRuleViolation()
    {
        var order = CreateOrder(OrderStatus.Shipped);

        Assert.Throws<BusinessRuleViolationException>(() => order.Cancel());
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Fact]
    public void Happy_Path_Lifecycle_Pending_Through_Delivered()
    {
        var order = CreateOrder(OrderStatus.Pending);

        order.TransitionTo(OrderStatus.Confirmed);
        order.TransitionTo(OrderStatus.Processing);
        order.TransitionTo(OrderStatus.Shipped);
        order.TransitionTo(OrderStatus.Delivered);

        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Processing)]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Processing, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending)]
    public void Invalid_Transitions_Throw_BusinessRuleViolation(OrderStatus from, OrderStatus to)
    {
        var order = CreateOrder(from);

        Assert.Throws<BusinessRuleViolationException>(() => order.TransitionTo(to));
        Assert.Equal(from, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Processing)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Processing, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered)]
    public void Valid_Transitions_Succeed(OrderStatus from, OrderStatus to)
    {
        var order = CreateOrder(from);

        order.TransitionTo(to);

        Assert.Equal(to, order.Status);
    }
}
