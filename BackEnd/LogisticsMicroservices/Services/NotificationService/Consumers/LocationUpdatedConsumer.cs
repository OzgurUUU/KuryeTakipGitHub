using Logistics.Contracts;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;

namespace NotificationService.Consumers;

public class LocationUpdatedConsumer : IConsumer<CourierLocationUpdatedEvent>
{
    private readonly IHubContext<LogisticsHub> _hubContext;
    private readonly ILogger<LocationUpdatedConsumer> _logger; // ← DriverAssignedConsumer'dan LocationUpdatedConsumer'a düzeltildi

    public LocationUpdatedConsumer(IHubContext<LogisticsHub> hubContext, ILogger<LocationUpdatedConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CourierLocationUpdatedEvent> context)
    {
        var msg = context.Message;

        _logger.LogDebug($"📍 Konum alındı: {msg.CourierId} -> {msg.Latitude}, {msg.Longitude}");

        await _hubContext.Clients.All.SendAsync("ReceiveLocation", msg.CourierId, msg.Latitude, msg.Longitude);
    }
}