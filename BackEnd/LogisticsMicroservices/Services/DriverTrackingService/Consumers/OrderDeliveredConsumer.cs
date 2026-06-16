using Logistics.Contracts;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using DriverTrackingService.Models;

namespace DriverTrackingService.Consumers;

public class OrderDeliveredConsumer : IConsumer<OrderDeliveredEvent>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<OrderDeliveredConsumer> _logger;

    public OrderDeliveredConsumer(IDistributedCache cache, ILogger<OrderDeliveredConsumer> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderDeliveredEvent> context)
    {
        var msg = context.Message;
        var cacheData = await _cache.GetStringAsync(msg.DriverId);
        if (string.IsNullOrEmpty(cacheData)) return;

        var location = JsonSerializer.Deserialize<CourierLocation>(cacheData);
        location.IsAvailable = true;

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        await _cache.SetStringAsync(msg.DriverId, JsonSerializer.Serialize(location), options);
        _logger.LogInformation($"🔓 {msg.DriverId} Redis'te müsait olarak işaretlendi.");
    }
}