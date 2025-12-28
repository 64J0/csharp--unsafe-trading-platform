# Trading Platform Performance Benchmarks

This project contains performance benchmarks for the Unsafe Trading Platform using BenchmarkDotNet.

## Benchmarks Included

### 1. Stack vs Heap Allocation

Compares performance of:

- Heap allocation using managed arrays
- Stack allocation using `stackalloc`
- Unmanaged memory allocation using `NativeMemory`

### 2. Pointer vs Array Access

Compares different access patterns:

- Traditional array indexing
- Pointer arithmetic with index
- Pointer increment iteration

### 3. OrderBook Operations

Measures performance of:

- Adding orders
- Adding and removing orders
- Modifying orders

### 4. Fixed Statement Overhead

Compares:

- Regular array access
- Fixed pointer with indexing
- Fixed pointer with arithmetic

## Running the Benchmarks

Run all benchmarks:

```bash
dotnet run -c Release --project TradingPlatform.Benchmarks
```

Run specific benchmark:

```bash
dotnet run -c Release --project TradingPlatform.Benchmarks --filter "*StackVsHeapAllocation*"
```

## Expected Results

- **Stack allocation** should be faster than heap allocation due to no GC pressure
- **Unmanaged memory** should have similar performance to stack allocation but with manual cleanup
- **Pointer arithmetic** may be slightly faster than array indexing due to bounds check elimination
- **Fixed statement** has minimal overhead for accessing managed arrays with pointers

## Notes

- All benchmarks run with memory diagnostics enabled
- Each benchmark performs 3 warmup iterations and 5 measured iterations
- Results include execution time, memory allocation, and GC collections
