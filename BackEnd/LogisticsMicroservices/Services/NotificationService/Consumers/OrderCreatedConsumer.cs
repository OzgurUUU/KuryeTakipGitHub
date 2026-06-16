using Logistics.Contracts;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;

namespace NotificationService.Consumers;

public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly IHubContext<LogisticsHub> _hubContext;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(IHubContext<LogisticsHub> hubContext, ILogger<OrderCreatedConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation($"📦 [Bildirim Servisi] Yeni sipariş oluşturuldu! Sipariş ID: {msg.OrderId}");

        // Frontend'e SignalR üzerinden yayın yapıyoruz
        await _hubContext.Clients.All.SendAsync("OrderCreated", new
        {
            orderId = msg.OrderId,
            customerName = msg.CustomerName,
            latitude = msg.DeliveryLatitude,
            longitude = msg.DeliveryLongitude,
            itemDescription = msg.ItemDescription,
            createdAt = msg.CreatedAt
        });
    }
}
