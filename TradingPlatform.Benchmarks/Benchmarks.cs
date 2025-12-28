using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Runtime.InteropServices;

namespace TradingPlatform.Benchmarks;

/// <summary>
/// Benchmarks comparing stack allocation (stackalloc) vs heap allocation for Order arrays
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public unsafe class StackVsHeapAllocation
{
    private const int OrderCount = 1000;

    [Benchmark(Baseline = true)]
    public void HeapAllocation()
    {
        // Allocate orders on the heap using managed array
        var orders = new Order[OrderCount];
        
        for (int i = 0; i < OrderCount; i++)
        {
            orders[i] = new Order 
            { 
                Id = i, 
                Price = 100.0 + i, 
                Quantity = 10, 
                IsBuyOrder = i % 2 == 0 
            };
        }

        // Simulate processing
        double totalPrice = 0;
        for (int i = 0; i < OrderCount; i++)
        {
            totalPrice += orders[i].Price;
        }
    }

    [Benchmark]
    public void StackAllocation()
    {
        // Allocate orders on the stack using stackalloc
        Span<Order> orders = stackalloc Order[OrderCount];
        
        for (int i = 0; i < orders.Length; i++)
        {
            orders[i] = new Order 
            { 
                Id = i, 
                Price = 100.0 + i, 
                Quantity = 10, 
                IsBuyOrder = i % 2 == 0 
            };
        }

        // Simulate processing
        double totalPrice = 0;
        for (int i = 0; i < orders.Length; i++)
        {
            totalPrice += orders[i].Price;
        }
    }

    [Benchmark]
    public void UnmanagedMemoryAllocation()
    {
        // Allocate orders using unmanaged memory (NativeMemory)
        Order* orders = (Order*)NativeMemory.Alloc((nuint)(sizeof(Order) * OrderCount));
        
        try
        {
            for (int i = 0; i < OrderCount; i++)
            {
                orders[i] = new Order 
                { 
                    Id = i, 
                    Price = 100.0 + i, 
                    Quantity = 10, 
                    IsBuyOrder = i % 2 == 0 
                };
            }

            // Simulate processing
            double totalPrice = 0;
            for (int i = 0; i < OrderCount; i++)
            {
                totalPrice += orders[i].Price;
            }
        }
        finally
        {
            NativeMemory.Free(orders);
        }
    }
}

/// <summary>
/// Benchmarks comparing pointer manipulation vs array indexing
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public unsafe class PointerVsArrayAccess
{
    private const int OrderCount = 1000;
    private Order[] managedOrders = null!;
    private Order* unmanagedOrders;

    [GlobalSetup]
    public void Setup()
    {
        // Setup managed array
        managedOrders = new Order[OrderCount];
        for (int i = 0; i < OrderCount; i++)
        {
            managedOrders[i] = new Order 
            { 
                Id = i, 
                Price = 100.0 + i, 
                Quantity = 10, 
                IsBuyOrder = i % 2 == 0 
            };
        }

        // Setup unmanaged memory
        unmanagedOrders = (Order*)NativeMemory.Alloc((nuint)(sizeof(Order) * OrderCount));
        for (int i = 0; i < OrderCount; i++)
        {
            unmanagedOrders[i] = new Order 
            { 
                Id = i, 
                Price = 100.0 + i, 
                Quantity = 10, 
                IsBuyOrder = i % 2 == 0 
            };
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        NativeMemory.Free(unmanagedOrders);
    }

    [Benchmark(Baseline = true)]
    public double ArrayIndexing()
    {
        double totalPrice = 0;
        for (int i = 0; i < OrderCount; i++)
        {
            totalPrice += managedOrders[i].Price;
        }
        return totalPrice;
    }

    [Benchmark]
    public double PointerArithmetic()
    {
        double totalPrice = 0;
        Order* ptr = unmanagedOrders;
        for (int i = 0; i < OrderCount; i++)
        {
            totalPrice += (ptr + i)->Price;
        }
        return totalPrice;
    }

    [Benchmark]
    public double PointerIncrement()
    {
        double totalPrice = 0;
        Order* ptr = unmanagedOrders;
        Order* end = ptr + OrderCount;
        while (ptr < end)
        {
            totalPrice += ptr->Price;
            ptr++;
        }
        return totalPrice;
    }
}

/// <summary>
/// Benchmarks for OrderBook operations
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public unsafe class OrderBookOperations
{
    private OrderBook orderBook = null!;
    private const int Size = 100;

    [GlobalSetup]
    public void Setup()
    {
        orderBook = new OrderBook(Size);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // Force cleanup
        orderBook = null!;
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [Benchmark]
    public void AddOrders()
    {
        var book = new OrderBook(Size);
        
        for (int i = 0; i < Size / 2; i++)
        {
            book.AddOrder(new Order 
            { 
                Id = i, 
                Price = 100.0 + i, 
                Quantity = 10, 
                IsBuyOrder = true 
            });
        }
        
        book = null!;
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [Benchmark]
    public void AddAndRemoveOrders()
    {
        var book = new OrderBook(Size);
        
        for (int i = 0; i < Size / 2; i++)
        {
            book.AddOrder(new Order 
            { 
                Id = i, 
                Price = 100.0 + i, 
                Quantity = 10, 
                IsBuyOrder = true 
            });
        }

        for (int i = 0; i < Size / 4; i++)
        {
            book.RemoveOrder(i, true);
        }
        
        book = null!;
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [Benchmark]
    public void ModifyOrders()
    {
        var book = new OrderBook(Size);
        
        for (int i = 0; i < Size / 2; i++)
        {
            book.AddOrder(new Order 
            { 
                Id = i, 
                Price = 100.0 + i, 
                Quantity = 10, 
                IsBuyOrder = true 
            });
        }

        for (int i = 0; i < Size / 4; i++)
        {
            book.ModifyOrder(i, 105.0 + i, 15);
        }
        
        book = null!;
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}

/// <summary>
/// Benchmarks comparing fixed statement overhead
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public unsafe class FixedStatementOverhead
{
    private double[] prices = new double[1000];

    [GlobalSetup]
    public void Setup()
    {
        for (int i = 0; i < prices.Length; i++)
        {
            prices[i] = 100.0 + i;
        }
    }

    [Benchmark(Baseline = true)]
    public double SumWithArrayAccess()
    {
        double sum = 0;
        for (int i = 0; i < prices.Length; i++)
        {
            sum += prices[i];
        }
        return sum;
    }

    [Benchmark]
    public double SumWithFixedPointer()
    {
        double sum = 0;
        fixed (double* ptr = prices)
        {
            for (int i = 0; i < prices.Length; i++)
            {
                sum += ptr[i];
            }
        }
        return sum;
    }

    [Benchmark]
    public double SumWithFixedPointerArithmetic()
    {
        double sum = 0;
        fixed (double* ptr = prices)
        {
            double* p = ptr;
            double* end = ptr + prices.Length;
            while (p < end)
            {
                sum += *p;
                p++;
            }
        }
        return sum;
    }
}

/// <summary>
/// Main program class to run benchmarks
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Trading Platform Performance Benchmarks");
        Console.WriteLine("========================================");
        Console.WriteLine();
        Console.WriteLine("Running benchmarks...");
        Console.WriteLine();

        // Run all benchmarks in the assembly
        var config = BenchmarkDotNet.Configs.DefaultConfig.Instance;
        var summary = BenchmarkRunner.Run(typeof(Program).Assembly, config, args);

        Console.WriteLine();
        Console.WriteLine("Benchmarks completed. Check the results above.");
    }
}
