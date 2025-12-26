// We're using a struct to represent an order in the trading platform.
// Using structs can help reduce memory overhead and improve performance
// when dealing with large numbers of orders, as they are value types
// and can be allocated on the stack.
public struct Order {
    public int Id { get; set; }
    public double Price { get; set; }
    public int Quantity { get; set; }
    public bool IsBuyOrder { get; set; }
}