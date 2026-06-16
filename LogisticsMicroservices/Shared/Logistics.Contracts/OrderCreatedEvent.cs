namespace Logistics.Contracts;

public record OrderCreatedEvent
{
    public Guid OrderId { get; init; }
    public string CustomerName { get; init; }
    public double DeliveryLatitude { get; init; }
    public double DeliveryLongitude { get; init; }
    public string ItemDescription { get; init; }
    public DateTime CreatedAt { get; init; }
}