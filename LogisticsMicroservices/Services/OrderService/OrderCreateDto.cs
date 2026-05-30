namespace OrderService.Models;

public class OrderCreateDto
{
    public string CustomerName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public string ItemDescription { get; set; }
}