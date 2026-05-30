namespace OrderService.Models;

public class Order
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string ItemDescription { get; set; }
    public string Status { get; set; } = "Pending";  // New, Assigned, OnTheWay, Delivered
    public DateTime CreatedAt { get; set; }
}
