using System.Runtime.InteropServices;

public unsafe class TradeLogger
{
    const int bufferSize = 100;
    Trade* tradeBuffer;
    int tradeCount;

    public TradeLogger()
    {
        unsafe
        {
            // The reason for using fixed-size buffer is predictable memory usafe, 
            // as the fixed-size buffers allocate a predetermined amount of memory. 
            // This predicatability helps in managing and optimizing memory usage, 
            // effectively avoiding unexpected memory growth that can lead to
            // fragmentation and performance degradation.
            // Allocating memory in fixed-size chunks can be more efficient than
            // dynamic allocations. It reduces the overhead associated with frequent
            // memory allocations and deallocations, which can be costly in terms
            // of performance.
            // Fixed-size buffers provide faster memory access compared to managed
            // arrays as they bypass the .NET runtime memory management.
            // And yet another reason is that we are reducing the GC overhead by 
            // using unmanaged memory allocation.
            tradeBuffer = (Trade*)NativeMemory.Alloc((nuint)(bufferSize * sizeof(Trade)));
        }
    }

    ~TradeLogger()
    {
        unsafe
        {
            NativeMemory.Free(tradeBuffer);
        }
    }

    public unsafe void LogTrade(Trade trade)
    {
        if (tradeCount < bufferSize)
        {
            tradeBuffer[tradeCount] = trade;
            tradeCount++;
        }
        else
        {
            ProcessAndClearBuffer();
            tradeBuffer[0] = trade;
            tradeCount = 1;
        }
    }

    unsafe void ProcessAndClearBuffer()
    {
        using(StreamWriter writer = new("TradeReport.txt", append: true))
        {
            for (int i = 0; i < tradeCount; i++)
            {
                Trade trade = tradeBuffer[i];
                writer.WriteLine($"TradeId: {trade.TradeId}, OrderId: {trade.OrderId}, Price: {trade.Price}, Quantity: {trade.Quantity}, Timestamp: {trade.Timestamp}");
            }
        }
        tradeCount = 0;
    }

    public unsafe void FinalizeLogging()
    {
        if (tradeCount > 0)
        {
            ProcessAndClearBuffer();
        }
    }
}