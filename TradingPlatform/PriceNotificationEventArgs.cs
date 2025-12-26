public class PriceNotificationEventArgs
{
    public double Price { get; }
    public bool IsBuyOrder { get; }

    public PriceNotificationEventArgs(double price, bool isBuyOrder)
    {
        Price = price;
        IsBuyOrder = isBuyOrder;
    }
}