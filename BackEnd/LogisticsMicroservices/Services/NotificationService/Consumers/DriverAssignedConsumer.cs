using MassTransit;
using Logistics.Contracts;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;

namespace NotificationService.Consumers;

public class DriverAssignedConsumer : IConsumer<DriverAssignedEvent>
{
    private readonly IHubContext<LogisticsHub> _hubContext;
    private readonly ILogger<DriverAssignedConsumer> _logger;

    // SignalR IHubContext enjekte ederek WebSocket hatlarına erişiyoruz
    public DriverAssignedConsumer(IHubContext<LogisticsHub> hubContext, ILogger<DriverAssignedConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DriverAssignedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation($"📢 [Bildirim Servisi] Atama haberi alındı! Sipariş: {message.OrderId} -> Kurye: {message.DriverId}");

        // 🚀 BÜYÜLÜ AN: Bağlı olan tüm Angular istemcilerine "SiparişAtandı" sinyalini ve veriyi gönderiyoruz
        await _hubContext.Clients.All.SendAsync("OrderAssigned", new
        {
            orderId = message.OrderId,
            driverId = message.DriverId,
            distance = message.Distance,
            time = message.AssignedAt
        });
    }
}