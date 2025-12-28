namespace TradingPlatform.Tests;

public unsafe class IntegrationTests : IDisposable
{
    private OrderBook _orderBook;

    public IntegrationTests()
    {
        _orderBook = new OrderBook(20);
    }

    public void Dispose()
    {
        _orderBook = null!;
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [Fact]
    public void CompleteTradeWorkflow_ShouldMatchBuyerAndSeller()
    {
        // Arrange
        var seller = new User(1, "Seller");
        var buyer = new User(2, "Buyer") { Balance = 2000.0 };

        // Add sell orders
        _orderBook.AddOrder(new Order { Id = 1, Price = 100.0, Quantity = 10, IsBuyOrder = false });
        _orderBook.AddOrder(new Order { Id = 2, Price = 105.0, Quantity = 15, IsBuyOrder = false });

        // Add buy orders
        _orderBook.AddOrder(new Order { Id = 3, Price = 95.0, Quantity = 5, IsBuyOrder = true });

        // Act - Buyer purchases at lowest sell price
        bool result = _orderBook.BuyAtLowestSellPrice(5, 150.0, buyer);

        // Assert
        Assert.True(result);
        Assert.True(buyer.Balance < 2000.0);
        Assert.True(buyer.Balance > 0);
    }

    [Fact]
    public void MultipleOrderOperations_ShouldMaintainConsistency()
    {
        // Arrange & Act
        // Add multiple buy orders
        _orderBook.AddOrder(new Order { Id = 1, Price = 100.0, Quantity = 10, IsBuyOrder = true });
        _orderBook.AddOrder(new Order { Id = 2, Price = 101.0, Quantity = 15, IsBuyOrder = true });
        _orderBook.AddOrder(new Order { Id = 3, Price = 102.0, Quantity = 20, IsBuyOrder = true });

        // Add multiple sell orders
        _orderBook.AddOrder(new Order { Id = 4, Price = 110.0, Quantity = 5, IsBuyOrder = false });
        _orderBook.AddOrder(new Order { Id = 5, Price = 111.0, Quantity = 8, IsBuyOrder = false });

        // Modify an order
        _orderBook.ModifyOrder(1, 99.0, 12);

        // Remove an order
        _orderBook.RemoveOrder(3, true);

        // Assert
        var order1 = _orderBook.GetOrderById(1, true);
        Assert.Equal(99.0, order1.Price);
        Assert.Equal(12, order1.Quantity);

        var order2 = _orderBook.GetOrderById(2, true);
        Assert.Equal(101.0, order2.Price);

        Assert.Throws<InvalidOperationException>(() => _orderBook.GetOrderById(3, true));

        var order4 = _orderBook.GetOrderById(4, false);
        Assert.False(order4.IsBuyOrder);
    }

    [Fact]
    public void PriceNotificationWorkflow_ShouldNotifyUsersOfPriceChanges()
    {
        // Arrange
        var user = new User(1, "Trader");
        user.SubscribeToBuyPrice(102.0);
        user.SubscribeToBuyPrice(103.0);

        int notificationCount = 0;
        List<double> notifiedPrices = new List<double>();

        _orderBook.PriceNotification += (sender, e) =>
        {
            notificationCount++;
            notifiedPrices.Add(e.Price);
        };

        // Act
        _orderBook.AddOrder(new Order { Id = 1, Price = 100.0, Quantity = 10, IsBuyOrder = true });
        _orderBook.AddOrder(new Order { Id = 2, Price = 105.0, Quantity = 15, IsBuyOrder = true });
        _orderBook.AddOrder(new Order { Id = 3, Price = 110.0, Quantity = 5, IsBuyOrder = false });

        // Assert
        Assert.True(notificationCount > 0);
        Assert.NotEmpty(notifiedPrices);
    }

    [Fact]
    public void BulkOperations_ShouldHandleLargeNumberOfOrders()
    {
        // Arrange
        var requests = new List<OrderCancellationRequest>();

        // Act - Add many orders
        for (int i = 1; i <= 15; i++)
        {
            _orderBook.AddOrder(new Order 
            { 
                Id = i, 
                Price = 100.0 + i, 
                Quantity = 10, 
                IsBuyOrder = i % 2 == 0 
            });

            if (i <= 5)
            {
                requests.Add(new OrderCancellationRequest { OrderId = i });
            }
        }

        // Bulk cancel some orders
        _orderBook.BulkCancelOrders(requests.ToArray());

        // Assert - Removed orders should throw when accessed
        foreach (var request in requests)
        {
            Assert.Throws<InvalidOperationException>(() => 
                _orderBook.GetOrderById(request.OrderId, request.OrderId % 2 == 0));
        }
    }

    [Fact]
    public void OrderBookCapacity_ShouldHandleFillAndEmpty()
    {
        // Arrange
        var orderBook = new OrderBook(5);

        // Act & Assert - Fill to capacity
        for (int i = 1; i <= 5; i++)
        {
            var order = new Order { Id = i, Price = 100.0 + i, Quantity = 10, IsBuyOrder = true };
            orderBook.AddOrder(order);
            var retrieved = orderBook.GetOrderById(i, true);
            Assert.Equal(i, retrieved.Id);
            Assert.Equal(100.0 + i, retrieved.Price);
        }

        // Act & Assert - Empty the order book
        for (int i = 1; i <= 5; i++)
        {
            orderBook.RemoveOrder(i, true);
            Assert.Throws<InvalidOperationException>(() => orderBook.GetOrderById(i, true));
        }

        // Act & Assert - Refill with new orders
        for (int i = 6; i <= 10; i++)
        {
            var order = new Order { Id = i, Price = 200.0 + i, Quantity = 20, IsBuyOrder = false };
            orderBook.AddOrder(order);
            var retrieved = orderBook.GetOrderById(i, false);
            Assert.Equal(i, retrieved.Id);
            Assert.Equal(200.0 + i, retrieved.Price);
            Assert.Equal(20, retrieved.Quantity);
            Assert.False(retrieved.IsBuyOrder);
        }

        // Assert - Verify old orders are still gone
        for (int i = 1; i <= 5; i++)
        {
            Assert.Throws<InvalidOperationException>(() => orderBook.GetOrderById(i, true));
        }
    }

    [Fact]
    public void ComplexTradeScenario_WithModificationsAndCancellations()
    {
        // Arrange
        var buyer1 = new User(1, "Buyer1") { Balance = 5000.0 };
        var buyer2 = new User(2, "Buyer2") { Balance = 3000.0 };

        // Act
        // Add initial orders
        _orderBook.AddOrder(new Order { Id = 1, Price = 100.0, Quantity = 10, IsBuyOrder = false });
        _orderBook.AddOrder(new Order { Id = 2, Price = 105.0, Quantity = 15, IsBuyOrder = false });
        _orderBook.AddOrder(new Order { Id = 3, Price = 95.0, Quantity = 20, IsBuyOrder = true });

        // Modify order
        _orderBook.ModifyOrder(1, 98.0, 12);

        // First buyer purchases
        bool trade1 = _orderBook.BuyAtLowestSellPrice(5, 150.0, buyer1);

        // Add more orders
        _orderBook.AddOrder(new Order { Id = 4, Price = 102.0, Quantity = 8, IsBuyOrder = false });

        // Second buyer purchases
        bool trade2 = _orderBook.BuyAtLowestSellPrice(3, 150.0, buyer2);

        // Cancel some orders
        _orderBook.BulkCancelOrders(new[] 
        { 
            new OrderCancellationRequest { OrderId = 3 } 
        });

        // Assert
        Assert.True(trade1);
        Assert.True(trade2);
        Assert.True(buyer1.Balance < 5000.0);
        Assert.True(buyer2.Balance < 3000.0);
    }

    [Fact]
    public void GetLowestSellPrice_InDynamicMarket_ShouldReturnCorrectPrice()
    {
        // Arrange & Act
        _orderBook.AddOrder(new Order { Id = 1, Price = 120.0, Quantity = 10, IsBuyOrder = false });
        double price1 = _orderBook.GetLowestSellPrice(out int index1);

        _orderBook.AddOrder(new Order { Id = 2, Price = 110.0, Quantity = 5, IsBuyOrder = false });
        double price2 = _orderBook.GetLowestSellPrice(out int index2);

        _orderBook.AddOrder(new Order { Id = 3, Price = 115.0, Quantity = 8, IsBuyOrder = false });
        double price3 = _orderBook.GetLowestSellPrice(out int index3);

        _orderBook.RemoveOrder(2, false); // Remove the lowest
        double price4 = _orderBook.GetLowestSellPrice(out int index4);

        // Assert
        Assert.Equal(120.0, price1);
        Assert.Equal(110.0, price2);
        Assert.Equal(110.0, price3); // Still 110 as it's the lowest
        Assert.Equal(115.0, price4); // After removing 110, next lowest is 115
        Assert.True(index1 >= 0);
        Assert.True(index2 >= 0);
        Assert.True(index3 >= 0);
        Assert.True(index4 >= 0);
    }
}
