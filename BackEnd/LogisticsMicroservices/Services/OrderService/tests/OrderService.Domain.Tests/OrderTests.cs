using OrderService.Domain.Entities;
using Xunit;

namespace OrderService.Domain.Tests;

public class OrderTests
{
    [Fact]
    public void Create_ValidParameters_ShouldCreateOrderWithPendingStatus()
    {
        // Arrange
        var customerName = "John Doe";
        var latitude = 40.0;
        var longitude = 35.0;
        var itemDescription = "Test item";

        // Act
        var order = Order.Create(customerName, latitude, longitude, itemDescription);

        // Assert
        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(customerName, order.CustomerName);
        Assert.Equal(latitude, order.Latitude);
        Assert.Equal(longitude, order.Longitude);
        Assert.Equal(itemDescription, order.ItemDescription);
        Assert.Equal("Pending", order.Status);
        Assert.True(order.CreatedAt <= DateTime.UtcNow);
    }
}
