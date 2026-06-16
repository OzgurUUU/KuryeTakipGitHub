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

    // Aktif kurye filosu — Simulator'daki CourierState.Couriers ile senkron olmalı.
    // Gerçek sistemde bu liste DB'den veya Redis Set'ten dinamik olarak okunur.
    private readonly List<string> _activeCourierIds = new()
    {
        // Merkez kuşak
        "kurye_ahmet", "kurye_mehmet", "kurye_ayse", "kurye_fatma", "kurye_mustafa",
        // Kuzey kuşak
        "kurye_zeynep", "kurye_hasan", "kurye_emine", "kurye_ali", "kurye_hatice",
        // Güney kuşak
        "kurye_ibrahim", "kurye_havva", "kurye_ismail", "kurye_rabia", "kurye_yusuf"
    };

    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger, IDistributedCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var order = context.Message;
        _logger.LogInformation($"📬 Yeni sipariş için kurye aranıyor... Sipariş ID: {order.OrderId}");

        string closestAvailableCourierId = null;
        double shortestAvailableDistance = double.MaxValue;

        string closestBusyCourierId = null;
        double shortestBusyDistance = double.MaxValue;

        foreach (var courierId in _activeCourierIds)
        {
            var cacheData = await _cache.GetStringAsync(courierId);
            if (string.IsNullOrEmpty(cacheData)) continue;

            var courierLocation = JsonSerializer.Deserialize<CourierLocation>(cacheData);

            double distance = DistanceCalculator.CalculateWithHaversine(
                order.DeliveryLatitude, order.DeliveryLongitude,
                courierLocation.Latitude, courierLocation.Longitude
            );

            _logger.LogInformation($"🔍 {courierId} mesafesi: {distance:F2} km - Müsait: {courierLocation.IsAvailable}");

            if (courierLocation.IsAvailable)
            {
                if (distance < shortestAvailableDistance)
                {
                    shortestAvailableDistance = distance;
                    closestAvailableCourierId = courierId;
                }
            }
            else
            {
                if (distance < shortestBusyDistance)
                {
                    shortestBusyDistance = distance;
                    closestBusyCourierId = courierId;
                }
            }
        }

        string closestCourierId = closestAvailableCourierId ?? closestBusyCourierId;
        double shortestDistance = closestAvailableCourierId != null ? shortestAvailableDistance : shortestBusyDistance;

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