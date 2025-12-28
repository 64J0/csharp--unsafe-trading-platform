# [Coursera] Unsafe Code in .NET: Boosting Performance in a Trading Application

This repository contains code from the Coursera course "Unsafe Code in .NET: Boosting Performance in a Trading Application". The course covers low-level .NET concepts like unsafe code, unmanaged memory, pointers, and value types (which are stack-allocated and faster than heap-allocated reference types). The example project is a basic [order book](https://en.wikipedia.org/wiki/Order_book) for a trading app.

Check the [Key_Takeaways_Document](./assets/Key_Takeaways_Document.pdf) PDF document for more details.

## Projects

### TradingPlatform

Main application demonstrating unsafe code (for performance reasons), pointer manipulation, and unmanaged memory allocation for high-performance order book operations.

What is unsafe code?

> Most of the C# code you write is "verifiably safe code." Verifiably safe code means .NET tools can verify that the code is safe. In general, safe code doesn't directly access memory using pointers. It also doesn't allocate raw memory. It creates managed objects instead.
>
> C# supports an unsafe context, in which you can write unverifiable code. In an unsafe context, code can use pointers, allocate and free blocks of memory, and call methods using function pointers. Unsafe code in C# isn't necessarily dangerous; it's just code whose safety can't be verified.
>
> --- [Unsafe code, pointer types, and function pointers](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code)

And from this same reference, we have:

> - In some cases, unsafe code can increase an application's performance by enabling direct memory access through pointers to avoid array bounds checks.
> [...]
> - Using unsafe code introduces security and stability risks.
>
> --- [Unsafe code, pointer types, and function pointers](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code)

When inspecting the codebase, you will notice that some values are "fixed". We do it to prevent the garbage collector from relocating them during unsafe operations. This ensures that pointers to these variables remain valid, and provide a stable memory address for unsafe operations:

> The garbage collector doesn't keep track of whether an object is being pointed to by any pointer types. If the referrant is an object in the managed heap (including local variables captured by lambda expressions or anonymous delegates), the object must be pinned for as long as the pointer is used.
>
> --- [Unsafe code, pointer types, and function pointers](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code)

### TradingPlatform.Tests

Unit and integration tests using xUnit to verify the correctness of OrderBook operations, order management, and event notifications.

**Run tests:**

```bash
dotnet test
```

- *Tests were added with the help of AI.*

### TradingPlatform.Benchmarks

Performance benchmarks using BenchmarkDotNet comparing:

- Stack vs heap allocation
- Pointer manipulation vs array indexing
- Fixed statement overhead
- OrderBook operation performance

**Run benchmarks:**

```bash
dotnet run -c Release --project TradingPlatform.Benchmarks
```

- *Benchmarks were added with the help of AI.*

## References

- [Unsafe code, pointer types, and function pointers](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code)
- [fixed statement - pin a variable for pointer operations](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/fixed)
- [stackalloc expression (C# reference)](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/stackalloc)
- [System.Runtime.InteropServices Namespace](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices?view=net-10.0)
