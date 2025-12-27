// TIP: Managed arrays in .NET add bounds checking and garbage collection overhead.
// By using pointer types, we can directly manipulate memory for better performance.

using System.Runtime.InteropServices;

public unsafe class OrderBook
{
    // The order book will hold a fixed number of orders for simplicity and performance.
    // In a real-world scenario, dynamic resizing and more complex data structures would be needed.
    int size = 10;
    Order* buyOrders;
    Order* sellOrders;
    public delegate void PriceNotificationEventHandler(object sender, PriceNotificationEventArgs e);
    public event PriceNotificationEventHandler PriceNotification;
    private double highestBuyPrice;
    private double lowestSellPrice;

    public OrderBook(int size)
    {
        this.size = size;

        // Allocate unmanaged memory for buy and sell orders.
        buyOrders = (Order*)NativeMemory.Alloc((nuint)(sizeof(Order) * size));
        sellOrders = (Order*)NativeMemory.Alloc((nuint)(sizeof(Order) * size));

        for (int i = 0; i < size; i++)
        {
            buyOrders[i] = new Order {Id=0};
            sellOrders[i] = new Order {Id=0};
        }

        highestBuyPrice = double.MinValue;
        lowestSellPrice = double.MaxValue;
    }

    protected virtual void OnPriceNotification(PriceNotificationEventArgs e)
    {
        PriceNotification?.Invoke(this, e);
    }

    // Destructor to free unmanaged memory.
    ~OrderBook()
    {
        // Free unmanaged memory to prevent memory leaks.
        NativeMemory.Free(buyOrders);
        NativeMemory.Free(sellOrders);
    }

    public unsafe void AddOrder(Order newOrder)
    {
        Order* orders = newOrder.IsBuyOrder ? buyOrders : sellOrders;

        for (int i = 0; i < size; i++)
        {
            if (orders[i].Id == 0) // Assuming Id=0 means empty slot
            {
                orders[i] = newOrder;
                UpdateAndNotify();
                return;
            }
        }
    }

    public unsafe void RemoveOrder(int orderId, bool isBuyOrder)
    {
        Order* orders = isBuyOrder ? buyOrders : sellOrders;

        for (int i = 0; i < size; i++)
        {
            if (orders[i].Id == orderId)
            {
                orders[i] = new Order {Id= 0}; // Mark as empty
                return;
            }
        }

        throw new InvalidOperationException("Order not found.");
    }

    public unsafe void ModifyOrder(int orderId, double newPrice, int newQuantity)
    {
        for (int i = 0; i < size; i++)
        {
            if (buyOrders[i].Id == orderId)
            {
                buyOrders[i].Price = newPrice;
                buyOrders[i].Quantity = newQuantity;
                UpdateAndNotify();
                return;
            }
            else if (sellOrders[i].Id == orderId)
            {
                sellOrders[i].Price = newPrice;
                sellOrders[i].Quantity = newQuantity;
                UpdateAndNotify();
                return;
            }
        }
    }

    public unsafe void UpdateAndNotify()
    {
        // Pin the variables in memory to prevent the garbage collector from relocating them during unsafe operations.
        // This ensures that pointers to these variables remain valid, and provide
        // a stable memory address for unsafe operations.
        fixed(double* fixedHighestBuyPrice = &highestBuyPrice, fixedLowestSellPrice = &lowestSellPrice)
        {
            *fixedHighestBuyPrice = double.MinValue;
            *fixedLowestSellPrice = double.MaxValue;

            for (int i = 0; i < size; i++)
            {
                if (buyOrders[i].Price > *fixedHighestBuyPrice)
                {
                    *fixedHighestBuyPrice = buyOrders[i].Price;
                    OnPriceNotification(new PriceNotificationEventArgs(*fixedHighestBuyPrice, true));
                }

                if (sellOrders[i].Price < *fixedLowestSellPrice)
                {
                    *fixedLowestSellPrice = sellOrders[i].Price;
                    OnPriceNotification(new PriceNotificationEventArgs(*fixedLowestSellPrice, false));
                }
            }
        }
    }

    public unsafe double GetLowestSellPrice(out int lowestSellIndex)
    {
        lowestSellIndex = -1;
        double lowestPrice = double.MaxValue;

        for (int i = 0; i < size; i++)
        {
            if (sellOrders[i].Id != 0 && sellOrders[i].Price < lowestPrice)
            {
                lowestPrice = sellOrders[i].Price;
                lowestSellIndex = i;
            }
        }

        return lowestSellPrice == double.MaxValue ? -1 : lowestSellPrice;
    }

    public unsafe bool BuyAtLowestSellPrice(int buyQuantity, double maxPrice, User buyer)
    {
        int lowestSellIndex;
        double lowestPrice = GetLowestSellPrice(out lowestSellIndex);

        if (lowestSellIndex == -1 || lowestPrice > maxPrice)
        {
            Console.WriteLine("No suitable sell orders available.");
            return false;
        }

        double totalCost = lowestPrice * buyQuantity;
        if (buyer.Balance < totalCost)
        {
            Console.WriteLine("Insufficient funds to complete the purchase.");
            return false;
        }

        Order* matchedSellOrder = &sellOrders[lowestSellIndex];

        int matchedQuantity = Math.Min(buyQuantity, matchedSellOrder->Quantity);
        matchedSellOrder->Quantity -= matchedQuantity;
        buyer.Balance -= totalCost;

        Console.WriteLine($"Order fulfilled: Buyer matched with Sell Order {matchedSellOrder->Id} at Price {matchedSellOrder->Price} for Quantity {matchedQuantity}.");

        if (matchedSellOrder->Quantity == 0)
        {
            RemoveOrder(matchedSellOrder->Id, false);
        }

        return true;
    }

    public unsafe void BulkCancelOrders(OrderCancellationRequest[] requests)
    {
        Span<int> buffer = stackalloc int[requests.Length];

        for (int i = 0; i < requests.Length; i++)
        {
            buffer[i] = requests[i].OrderId;
        }

        RemoveOrders(buffer);
    }

    private unsafe void RemoveOrders(Span<int> orderIds)
    {
        fixed(int* orderIdsPtr = orderIds)
        {
            for (int i = 0; i < orderIds.Length; i++)
            {
                int orderId = orderIdsPtr[i];
                int orderIndex = -1;
                Order* orders = null;

                for (int j = 0; j < size; j++)
                {
                    if (buyOrders[j].Id == orderId)
                    {
                        orders = buyOrders;
                        orderIndex = j;
                        break;
                    }
                    else if (sellOrders[j].Id == orderId)
                    {
                        orders = sellOrders;
                        orderIndex = j;
                        break;
                    }
                }

                if (orderIndex != -1)
                {
                    orders[orderIndex] = new Order {Id= 0}; // Mark as empty
                }
            }
        }
    }

    public unsafe void PrintOrders()
    {
        Console.WriteLine("Buy Orders:");
        for (int i = 0; i < size; i++)
        {
            if (buyOrders[i].Id != 0)
            {
                Console.WriteLine($"Order {buyOrders[i].Id}: Price={buyOrders[i].Price}, Quantity={buyOrders[i].Quantity}");
            }
        }

        Console.WriteLine("Sell Orders:");
        for (int i = 0; i < size; i++)
        {
            if (sellOrders[i].Id != 0)
            {
                Console.WriteLine($"Order {sellOrders[i].Id}: Price={sellOrders[i].Price}, Quantity={sellOrders[i].Quantity}");
            }
        }
    }
}