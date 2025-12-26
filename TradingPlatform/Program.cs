// Task 1:
//
// - Set up the initial project structure for a trading platform using unsafe code in C#.
// - Create a struct to represent an order in the trading platform.
// - Implement basic functionality to create and manage orders using unsafe code.
//
// Task 2
//
// - Dive deeper into managing the order book with pointer types.
// - Declare and initialize pointer types for order book entries.
// - Implement methods to add and remove orders using pointer manipulation.
// - Utilize NativeMemory for unmanaged memory allocation and deallocation.
//
// Task 3:
//
// - Implement price notifications with general subscriptions using events.
//  - Allow users to subscribe to multiple price notifications using events.
//  - Notify users when the latest sale or buy order reach their desired target prices.
//  - Use the fixed statement to pin variables in memory during unsafe operations to ensure
//    pointers are not relocated by the garbage collector.

User user1 = new User(1, "Alice");
user1.SubscribeToBuyPrice(99.0);   // Notify if price rises above 99 for buy orders
user1.SubscribeToBuyPrice(100.0);  // minimum user is willing to sell for
user1.SubscribeToSellPrice(105.0); // Notify if price falls below 105 for sell orders
user1.SubscribeToSellPrice(101.0); // maximum user is willing to pay

OrderBook orderBook = new OrderBook();
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