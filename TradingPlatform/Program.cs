// Lesson 1:
//
// - Set up the initial project structure for a trading platform using unsafe code in C#.
// - Create a struct to represent an order in the trading platform.
// - Implement basic functionality to create and manage orders using unsafe code.
//
// Lesson 2
//
// - Dive deeper into managing the order book with pointer types.
// - Declare and initialize pointer types for order book entries.
// - Implement methods to add and remove orders using pointer manipulation.
// - Utilize NativeMemory for unmanaged memory allocation and deallocation.
//
// Lesson 3:
//
// - Implement price notifications with general subscriptions using events.
//  - Allow users to subscribe to multiple price notifications using events.
//  - Notify users when the latest sale or buy order reach their desired target prices.
//  - Use the fixed statement to pin variables in memory during unsafe operations to ensure
//    pointers are not relocated by the garbage collector.
//
// Practice activity:
//
// - Simulate a stream of incoming orders using pointers and the fixed statement.
//
// Lesson 4:
//
// - Dive deeper into using pointer conversions and arithmetic for order fulfillment.
// - Implement methods to check the current lowest sell price.
// - Allow users to buy at that price efficiently.
//
// Lesson 5:
//
// - Implement a trade reporting feature that aggregates trade data using fixed-size buffers and generates comprehensive reports on trading activity.
//   - Use fixed-size buffers to temporarily hold the trade data.
//   - Write the trade data to a file for further analysis or record keeping.

void SimulateUserSubscriptionsAndOrders(OrderBook orderBook)
{
    User user1 = new User(1, "Alice");
    user1.SubscribeToBuyPrice(99.0);   // Notify if price rises above 99 for buy orders
    user1.SubscribeToBuyPrice(100.0);  // minimum user is willing to sell for
    user1.SubscribeToSellPrice(105.0); // Notify if price falls below 105 for sell orders
    user1.SubscribeToSellPrice(101.0); // maximum user is willing to pay

    orderBook.PriceNotification += user1.OnPriceNotification;

    Order newBuyOrder = new Order
    {
        Id = 1,
        IsBuyOrder = true,
        Quantity = 100,
        Price = 101.5
    };
    orderBook.AddOrder(newBuyOrder);

    Order newSellOrder = new Order
    {
        Id = 2,
        IsBuyOrder = false,
        Quantity = 50,
        Price = 102.5
    };
    orderBook.AddOrder(newSellOrder);

    orderBook.PrintOrders();

    orderBook.RemoveOrder(1, true);

    orderBook.PrintOrders();
}

void SimulateIncomingOrders(OrderBook orderBook)
{
    int size = 10;
    Order[] orders = new Order[size];

    unsafe {
        fixed (Order* ordersPtr = orders)
        {
            for (int i = 0; i < size; i++)
            {
                if (i < size / 2)
                {
                    ordersPtr[i] = new Order
                    {
                        Id = i + 1,
                        IsBuyOrder = true,
                        Quantity = 10 + i,
                        Price = 100 + i
                    };
                }
                else
                {
                    ordersPtr[i] = new Order
                    {
                        Id = i + 1,
                        IsBuyOrder = false,
                        Quantity = 20 + i,
                        Price = 200 + i
                    };
                }
                orderBook.AddOrder(ordersPtr[i]);
            }
        }
    }
}

void PracticeActivity()
{
    int orderBookSize = 10;
    OrderBook orderBook = new (orderBookSize);
    SimulateIncomingOrders(orderBook);
    orderBook.PrintOrders();
    orderBook.ModifyOrder(1, 99.0, 120);
    orderBook.ModifyOrder(6, 104.9, 120);
    orderBook.PrintOrders();
}

void RunLessonFour()
{
    int size = 10;
    OrderBook orderBook = new (size);
    SimulateIncomingOrders(orderBook);

    User buyer = new User(1, "John Doe");
    buyer.Balance = 10_000.0;

    int lowestSellIndex;
    double lowestSellPrice = orderBook.GetLowestSellPrice(out lowestSellIndex);
    Console.WriteLine($"Current lowest sell price: {lowestSellPrice}");

    orderBook.PrintOrders();

    bool purchaseSuccess = orderBook.BuyAtLowestSellPrice(50, 150.0, buyer);

    orderBook.PrintOrders();
}

// RunLessonFour();

void RunLessonFive()
{
    TradeLogger tradeLogger = new();

    // Simulate trades
    Random random = new();
    for (int i = 0; i < 10; i++)
    {
        Trade trade = new Trade
        {
            TradeId = i + 1,
            OrderId = (i % 50) + 1,
            Price = 100.0 + (i % 20),
            Quantity = 10 + (i % 5),
            Timestamp = DateTime.Now.AddDays(-random.Next(0, 30))
        };
        tradeLogger.LogTrade(trade);
    }

    tradeLogger.FinalizeLogging();
}

RunLessonFive();