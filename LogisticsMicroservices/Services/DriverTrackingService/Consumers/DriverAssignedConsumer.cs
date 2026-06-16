using Logistics.Contracts;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using DriverTrackingService.Models;

namespace DriverTrackingService.Consumers;

public class DriverAssignedConsumer : IConsumer<DriverAssignedEvent>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DriverAssignedConsumer> _logger;

    public DriverAssignedConsumer(IDistributedCache cache, ILogger<DriverAssignedConsumer> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DriverAssignedEvent> context)
    {
        var msg = context.Message;
        await SetAvailability(msg.DriverId, isAvailable: false);
        _logger.LogInformation($"🔒 {msg.DriverId} Redis'te meşgul olarak işaretlendi.");
    }

    private async Task SetAvailability(string courierId, bool isAvailable)
    {
        var cacheData = await _cache.GetStringAsync(courierId);
        if (string.IsNullOrEmpty(cacheData)) return;

        var location = JsonSerializer.Deserialize<CourierLocation>(cacheData);
        location.IsAvailable = isAvailable;

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        await _cache.SetStringAsync(courierId, JsonSerializer.Serialize(location), options);
    }
}