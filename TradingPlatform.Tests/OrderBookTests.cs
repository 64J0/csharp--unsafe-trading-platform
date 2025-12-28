namespace TradingPlatform.Tests;

public unsafe class OrderBookTests : IDisposable
{
    private OrderBook _orderBook;

    public OrderBookTests()
    {
        _orderBook = new OrderBook(10);
    }

    public void Dispose()
    {
        // Ensure proper cleanup
        _orderBook = null!;
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [Fact]
    public void AddOrder_ShouldAddBuyOrderSuccessfully()
    {
        // Arrange
        var order = new Order 
        { 
            Id = 1, 
            Price = 100.0, 
            Quantity = 10, 
            IsBuyOrder = true 
        };

        // Act
        _orderBook.AddOrder(order);

        // Assert - Verify by successfully removing it
        _orderBook.RemoveOrder(1, true); // Should not throw
        
        // Verify it's actually gone
        Assert.Throws<InvalidOperationException>(() => 
            _orderBook.RemoveOrder(1, true));
    }

    [Fact]
    public void AddOrder_ShouldAddSellOrderSuccessfully()
    {
        // Arrange
        var order = new Order 
        { 
            Id = 2, 
            Price = 110.0, 
            Quantity = 5, 
            IsBuyOrder = false 
        };

        // Act
        _orderBook.AddOrder(order);

        // Assert - Verify by successfully removing it
        _orderBook.RemoveOrder(2, false); // Should not throw

        // Verify it's actually gone
        Assert.Throws<InvalidOperationException>(() => 
            _orderBook.RemoveOrder(2, false));
    }

    [Fact]
    public void RemoveOrder_ShouldRemoveExistingOrder()
    {
        // Arrange
        var order = new Order 
        { 
            Id = 1, 
            Price = 100.0, 
            Quantity = 10, 
            IsBuyOrder = true 
        };
        _orderBook.AddOrder(order);

        // Act
        _orderBook.RemoveOrder(1, true);

        // Assert - Removing again should throw
        Assert.Throws<InvalidOperationException>(() => 
            _orderBook.RemoveOrder(1, true));
    }

    [Fact]
    public void RemoveOrder_ShouldThrowWhenOrderNotFound()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            _orderBook.RemoveOrder(999, true));
    }

    [Fact]
    public void ModifyOrder_ShouldUpdateOrderPriceAndQuantity()
    {
        // Arrange
        var order = new Order 
        { 
            Id = 1, 
            Price = 100.0, 
            Quantity = 10, 
            IsBuyOrder = true 
        };
        _orderBook.AddOrder(order);

        var fetchedOrder = _orderBook.GetOrderById(1, true);
        Assert.Equal(100.0, fetchedOrder.Price);
        Assert.Equal(10, fetchedOrder.Quantity);

        // Act
        _orderBook.ModifyOrder(1, 105.0, 15);
        var fetchedOrderMod1 = _orderBook.GetOrderById(1, true);
        Assert.Equal(105.0, fetchedOrderMod1.Price);
        Assert.Equal(15, fetchedOrderMod1.Quantity);

        // Assert - Order should still exist and be modifiable again
        _orderBook.ModifyOrder(1, 110.0, 20);
        var fetchedOrderMod2 = _orderBook.GetOrderById(1, true);
        Assert.Equal(110.0, fetchedOrderMod2.Price);
        Assert.Equal(20, fetchedOrderMod2.Quantity);
    }

    [Fact]
    public void GetLowestSellPrice_ShouldReturnLowestPriceWhenOrdersExist()
    {
        // Arrange
        _orderBook.AddOrder(new Order { Id = 1, Price = 120.0, Quantity = 5, IsBuyOrder = false });
        _orderBook.AddOrder(new Order { Id = 2, Price = 110.0, Quantity = 10, IsBuyOrder = false });
        _orderBook.AddOrder(new Order { Id = 3, Price = 115.0, Quantity = 8, IsBuyOrder = false });

        // Act
        double lowestPrice = _orderBook.GetLowestSellPrice(out int lowestIndex);

        // Assert
        Assert.Equal(110.0, lowestPrice);
        Assert.True(lowestIndex >= 0);
    }

    [Fact]
    public void GetLowestSellPrice_ShouldReturnNegativeWhenNoOrdersExist()
    {
        // Act
        double lowestPrice = _orderBook.GetLowestSellPrice(out int lowestIndex);

        // Assert
        Assert.Equal(-1, lowestPrice);
        Assert.Equal(-1, lowestIndex);
    }

    [Fact]
    public void BuyAtLowestSellPrice_ShouldSucceedWithSufficientFunds()
    {
        // Arrange
        var sellOrder = new Order { Id = 1, Price = 100.0, Quantity = 10, IsBuyOrder = false };
        _orderBook.AddOrder(sellOrder);

        var buyer = new User(1, "TestBuyer") { Balance = 1000.0 };

        // Act
        bool result = _orderBook.BuyAtLowestSellPrice(5, 150.0, buyer);

        // Assert
        Assert.True(result);
        Assert.True(buyer.Balance < 1000.0); // Balance should be reduced
    }

    [Fact]
    public void BuyAtLowestSellPrice_ShouldFailWithInsufficientFunds()
    {
        // Arrange
        var sellOrder = new Order { Id = 1, Price = 100.0, Quantity = 10, IsBuyOrder = false };
        _orderBook.AddOrder(sellOrder);

        var buyer = new User(1, "TestBuyer") { Balance = 50.0 };

        // Act
        bool result = _orderBook.BuyAtLowestSellPrice(5, 150.0, buyer);

        // Assert
        Assert.False(result);
        Assert.Equal(50.0, buyer.Balance); // Balance should remain unchanged
    }

    [Fact]
    public void BuyAtLowestSellPrice_ShouldFailWhenMaxPriceTooLow()
    {
        // Arrange
        var sellOrder = new Order { Id = 1, Price = 100.0, Quantity = 10, IsBuyOrder = false };
        _orderBook.AddOrder(sellOrder);
        _orderBook.ModifyOrder(1, 100.0, 10); // Trigger UpdateAndNotify

        var buyer = new User(1, "TestBuyer") { Balance = 1000.0 };

        // Act
        bool result = _orderBook.BuyAtLowestSellPrice(5, 50.0, buyer);

        // Assert
        Assert.False(result);
        Assert.Equal(1000.0, buyer.Balance); // Balance should remain unchanged
    }

    [Fact]
    public void BulkCancelOrders_ShouldRemoveMultipleOrders()
    {
        // Arrange
        _orderBook.AddOrder(new Order { Id = 1, Price = 100.0, Quantity = 10, IsBuyOrder = true });
        _orderBook.AddOrder(new Order { Id = 2, Price = 110.0, Quantity = 5, IsBuyOrder = false });
        _orderBook.AddOrder(new Order { Id = 3, Price = 105.0, Quantity = 8, IsBuyOrder = true });

        var requests = new OrderCancellationRequest[]
        {
            new OrderCancellationRequest { OrderId = 1 },
            new OrderCancellationRequest { OrderId = 3 }
        };

        // Act
        _orderBook.BulkCancelOrders(requests);

        // Assert - Removing again should throw
        Assert.Throws<InvalidOperationException>(() => _orderBook.RemoveOrder(1, true));
        Assert.Throws<InvalidOperationException>(() => _orderBook.RemoveOrder(3, true));

        // Assert - Order 2 should still exist
        var fetchedOrder = _orderBook.GetOrderById(2, false);
        Assert.Equal(110.0, fetchedOrder.Price);
        Assert.Equal(5, fetchedOrder.Quantity);
    }

    [Fact]
    public void PriceNotification_ShouldTriggerEventWhenPriceUpdates()
    {
        // Arrange
        bool eventTriggered = false;
        double notifiedPrice = 0;
        bool notifiedIsBuyOrder = false;

        _orderBook.PriceNotification += (sender, e) =>
        {
            eventTriggered = true;
            notifiedPrice = e.Price;
            notifiedIsBuyOrder = e.IsBuyOrder;
        };

        // Act - AddOrder calls UpdateAndNotify which should trigger the event
        _orderBook.AddOrder(new Order { Id = 1, Price = 100.0, Quantity = 10, IsBuyOrder = true });

        // Assert
        Assert.True(eventTriggered);
        Assert.Equal(100.0, notifiedPrice);
        Assert.True(notifiedIsBuyOrder);
    }

    [Fact]
    public void OrderBook_ShouldHandleMaxCapacity()
    {
        // Arrange
        var orderBook = new OrderBook(3); // Small capacity for testing

        // Act
        orderBook.AddOrder(new Order { Id = 1, Price = 100.0, Quantity = 10, IsBuyOrder = true });
        orderBook.AddOrder(new Order { Id = 2, Price = 101.0, Quantity = 10, IsBuyOrder = true });
        orderBook.AddOrder(new Order { Id = 3, Price = 102.0, Quantity = 10, IsBuyOrder = true });
        orderBook.AddOrder(new Order { Id = 4, Price = 103.0, Quantity = 10, IsBuyOrder = true }); // Should not throw

        // Assert - Should complete without exception
        Assert.True(true);
    }
}
