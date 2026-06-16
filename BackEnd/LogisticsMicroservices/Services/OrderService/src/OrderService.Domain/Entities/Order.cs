namespace OrderService.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public string ItemDescription { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Pending";
    public DateTime CreatedAt { get; private set; }

    private Order() { } // ORM için gerekli

    public static Order Create(string customerName, double latitude, double longitude, string itemDescription)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = customerName,
            Latitude = latitude,
            Longitude = longitude,
            ItemDescription = itemDescription,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateStatus(string newStatus)
    {
        Status = newStatus;
    }
}
