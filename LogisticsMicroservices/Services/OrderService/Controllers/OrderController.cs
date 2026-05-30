using Logistics.Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using MassTransit; 
using Logistics.Contracts;
namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint; // MassTransit yayın ucu

    public OrdersController(OrderDbContext context, IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto dto)
    {
        var newOrder = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = dto.CustomerName,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            ItemDescription = dto.ItemDescription,
            Status = "New",
            CreatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(newOrder);
        await _context.SaveChangesAsync();

        // 🚀 BÜYÜLÜ DOKUNUŞ: RabbitMQ'ya siparişin oluştuğunu bildiriyoruz
        await _publishEndpoint.Publish<OrderCreatedEvent>(new OrderCreatedEvent
        {
            OrderId = newOrder.Id,
            CustomerName = newOrder.CustomerName,
            DeliveryLatitude = newOrder.Latitude,
            DeliveryLongitude = newOrder.Longitude,
            ItemDescription = newOrder.ItemDescription,
            CreatedAt = newOrder.CreatedAt
        });

        return Ok(new { Message = "Sipariş alındı ve sisteme duyuruldu.", OrderId = newOrder.Id });
    }
}