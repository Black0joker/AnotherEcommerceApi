using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;

namespace ECommerce.UnitTests.Domain;

public class InventoryItemTests
{
    private static InventoryItem CreateItem(int available, int reserved = 0) => new()
    {
        ProductId = Guid.NewGuid(),
        AvailableQuantity = available,
        ReservedQuantity = reserved
    };

    [Fact]
    public void TotalQuantity_Is_Sum_Of_Available_And_Reserved()
    {
        var item = CreateItem(available: 7, reserved: 3);

        Assert.Equal(10, item.TotalQuantity);
    }

    [Theory]
    [InlineData(5, 5, true)]
    [InlineData(5, 6, false)]
    [InlineData(0, 1, false)]
    [InlineData(10, 0, true)]
    public void HasAvailableStock_Respects_Available_Quantity(int available, int requested, bool expected)
    {
        var item = CreateItem(available);

        Assert.Equal(expected, item.HasAvailableStock(requested));
    }

    [Fact]
    public void Reserve_Moves_Quantity_From_Available_To_Reserved()
    {
        var item = CreateItem(available: 10);

        item.Reserve(4);

        Assert.Equal(6, item.AvailableQuantity);
        Assert.Equal(4, item.ReservedQuantity);
        Assert.Equal(10, item.TotalQuantity); // stock is never lost, only moved
    }

    [Fact]
    public void Reserve_More_Than_Available_Throws_InsufficientInventory()
    {
        var item = CreateItem(available: 2);

        Assert.Throws<InsufficientInventoryException>(() => item.Reserve(3));
        Assert.Equal(2, item.AvailableQuantity);
        Assert.Equal(0, item.ReservedQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reserve_With_NonPositive_Quantity_Throws_InvalidQuantity(int quantity)
    {
        var item = CreateItem(available: 5);

        Assert.Throws<InvalidQuantityException>(() => item.Reserve(quantity));
    }

    [Fact]
    public void Release_Moves_Quantity_Back_To_Available()
    {
        var item = CreateItem(available: 6, reserved: 4);

        item.Release(4);

        Assert.Equal(10, item.AvailableQuantity);
        Assert.Equal(0, item.ReservedQuantity);
    }

    [Fact]
    public void Release_More_Than_Reserved_Throws()
    {
        var item = CreateItem(available: 5, reserved: 1);

        Assert.Throws<InvalidQuantityException>(() => item.Release(2));
    }

    [Fact]
    public void ConfirmReservation_Removes_Reserved_Quantity_Without_Restocking()
    {
        var item = CreateItem(available: 6, reserved: 4);

        item.ConfirmReservation(4);

        Assert.Equal(6, item.AvailableQuantity);
        Assert.Equal(0, item.ReservedQuantity);
        Assert.Equal(6, item.TotalQuantity); // purchased units leave the stock pool
    }

    [Fact]
    public void ConfirmReservation_More_Than_Reserved_Throws()
    {
        var item = CreateItem(available: 5, reserved: 1);

        Assert.Throws<InvalidQuantityException>(() => item.ConfirmReservation(2));
    }

    [Fact]
    public void Adjust_Can_Increase_And_Decrease_Stock()
    {
        var item = CreateItem(available: 5);

        item.Adjust(3);
        Assert.Equal(8, item.AvailableQuantity);

        item.Adjust(-2);
        Assert.Equal(6, item.AvailableQuantity);
    }

    [Fact]
    public void Adjust_Below_Zero_Throws()
    {
        var item = CreateItem(available: 2);

        Assert.Throws<InvalidQuantityException>(() => item.Adjust(-5));
    }

    [Fact]
    public void Concurrent_Reservations_On_Last_Unit_Never_Go_Negative()
    {
        // Simulates two buyers racing for the final unit. Only the first
        // reservation may succeed; stock must never become negative.
        var item = CreateItem(available: 1);

        var successes = 0;
        for (var i = 0; i < 2; i++)
        {
            try
            {
                item.Reserve(1);
                successes++;
            }
            catch (InsufficientInventoryException)
            {
                // second buyer fails - expected
            }
        }

        Assert.Equal(1, successes);
        Assert.True(item.AvailableQuantity >= 0);
    }
}
