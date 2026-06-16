using MassTransit;
using Logistics.Contracts;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using DriverTrackingService.Models;
using DriverTrackingService.Helpers;

namespace DriverTrackingService.Consumers;

public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly IDistributedCache _cache;

    // Simülasyondaki aktif kurye listemiz (Gerçek sistemde bu liste DB'den veya Redis Set'ten gelir)
    private readonly List<string> _activeCourierIds = new() { "kurye_ahmet", "kurye_mehmet", "kurye_ayse" };

    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger, IDistributedCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var order = context.Message;
        _logger.LogInformation($"📬 Yeni sipariş için kurye aranıyor... Sipariş ID: {order.OrderId}");

        string closestCourierId = null;
        double shortestDistance = double.MaxValue;

        foreach (var courierId in _activeCourierIds)
        {
            var cacheData = await _cache.GetStringAsync(courierId);
            if (string.IsNullOrEmpty(cacheData)) continue;

            var courierLocation = JsonSerializer.Deserialize<CourierLocation>(cacheData);

            // IsAvailable kontrolü kaldırıldı — meşgul olsa da en yakın kuryeye ata,
            // Simulator kuyruğa alacak
            double distance = DistanceCalculator.CalculateWithHaversine(
                order.DeliveryLatitude, order.DeliveryLongitude,
                courierLocation.Latitude, courierLocation.Longitude
            );

            _logger.LogInformation($"🔍 {courierId} mesafesi: {distance:F2} km");

            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closestCourierId = courierId;
            }
        }

        if (closestCourierId != null)
        {
            _logger.LogInformation($"🎯 Sipariş {order.OrderId} -> {closestCourierId} ({shortestDistance:F2} km)");
            await context.Publish<DriverAssignedEvent>(new DriverAssignedEvent
            {
                OrderId = order.OrderId,
                DriverId = closestCourierId,
                Distance = shortestDistance,
                AssignedAt = DateTime.UtcNow,
                TargetLatitude = order.DeliveryLatitude,
                TargetLongitude = order.DeliveryLongitude
            });
        }
        else
        {
            _logger.LogWarning("❌ Aktif kurye bulunamadı!");
        }
    }
}