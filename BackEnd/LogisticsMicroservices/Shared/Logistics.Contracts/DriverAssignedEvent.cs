namespace Logistics.Contracts;

public record DriverAssignedEvent
{
    public Guid OrderId { get; init; }
    public string DriverId { get; init; }
    public double Distance { get; init; }
    public DateTime AssignedAt { get; init; }

    public double TargetLatitude { get; init; }
    public double TargetLongitude { get; init; }
}
