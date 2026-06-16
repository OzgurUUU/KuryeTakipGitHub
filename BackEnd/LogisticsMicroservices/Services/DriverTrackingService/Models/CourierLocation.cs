namespace DriverTrackingService.Models;

public class CourierLocation
{
    public string CourierId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime LastUpdated { get; set; }
    public bool IsAvailable { get; set; } = true; // ← yeni alan
}