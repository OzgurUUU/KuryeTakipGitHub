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
        _logger.LogInformation($"📬 [Kurye Servisi] Yeni sipariş için kurye aranıyor... Sipariş ID: {order.OrderId}");

        string closestCourierId = null;
        double shortestDistance = double.MaxValue;

        // 1. Redis'teki tüm aktif kuryelerin konumlarını dönüyoruz
        foreach (var courierId in _activeCourierIds)
        {
            var cacheData = await _cache.GetStringAsync(courierId);
            if (string.IsNullOrEmpty(cacheData)) continue; // Kurye aktif değilse geç

            var courierLocation = JsonSerializer.Deserialize<CourierLocation>(cacheData);

            // 2. Haversine ile sipariş noktası ile kurye arasındaki mesafeyi ölçüyoruz
            double distance = DistanceCalculator.CalculateWithHaversine(
                order.DeliveryLatitude, order.DeliveryLongitude,
                courierLocation.Latitude, courierLocation.Longitude
            );

            _logger.LogInformation($"🔍 {courierId} mesafesi: {distance:F2} km");

            // 3. En yakındaki kuryeyi güncelliyoruz
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closestCourierId = courierId;
            }
        }

        // 4. Sonucu ekrana basıyoruz ve atamayı gerçekleştiriyoruz
        if (closestCourierId != null)
        {
            _logger.LogInformation($"🎯 ATAŞMA BAŞARILI! Sipariş {order.OrderId} için en yakın kurye: {closestCourierId} ({shortestDistance:F2} km uzakta) --- enlemi: {order.DeliveryLatitude}");
            order.Status = "Assigned";
            await context.Publish<DriverAssignedEvent>(new DriverAssignedEvent
            {
                OrderId = order.OrderId,
                DriverId = closestCourierId,
                Distance = shortestDistance,
                AssignedAt = DateTime.UtcNow,
                TargetLatitude = order.DeliveryLatitude,
                TargetLongitude = order.DeliveryLongitude
            });
            // TODO: Bir sonraki adımda burada RabbitMQ'ya "Kurye Atandı" (DriverAssignedEvent) fırlatacağız!
        }
        else
        {
            _logger.LogWarning("❌ Siparişe atanacak aktif kurye bulunamadı!");
        }
    }
}