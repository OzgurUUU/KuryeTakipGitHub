using DriverTrackingService.Models;
using Logistics.Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace DriverTrackingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrackingController : ControllerBase
{
    private readonly IDistributedCache _cache;
    private readonly IPublishEndpoint _publishEndpoint;

    public TrackingController(IDistributedCache cache, IPublishEndpoint publishEndpoint)
    {
        _cache = cache;
        _publishEndpoint = publishEndpoint;
    }

    [HttpPost("update-location")]
    public async Task<IActionResult> UpdateLocation([FromBody] CourierLocation dto)
    {
        dto.LastUpdated = DateTime.UtcNow;

        // Modeli JSON formatına çeviriyoruz
        var locationJson = JsonSerializer.Serialize(dto);

        // Kurye 5 dakika boyunca konum atmazsa Redis'ten otomatik düşsün (Expire)
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        // Veriyi Redis'e yazıyoruz (Haritadan o anki son konumu almak için ana depo)
        await _cache.SetStringAsync(dto.CourierId, locationJson, options);

        // --- THROTTLING (FRENLEME) MANTIĞI ---
        var throttleKey = $"throttle_location_{dto.CourierId}";
        var isThrottled = await _cache.GetStringAsync(throttleKey);

        if (string.IsNullOrEmpty(isThrottled))
        {
            // Eğer throttle key YOKSA (yani 3 saniye geçmişse veya ilk defa konum atıyorsa)
            // 1. Olayı fırlat (SignalR'a gitsin)
            await _publishEndpoint.Publish(new CourierLocationUpdatedEvent
            {
                CourierId = dto.CourierId,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude
            });

            // 2. Yeni bir Throttle kilidi oluştur (TTL = 3 saniye)
            var throttleOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3)
            };
            await _cache.SetStringAsync(throttleKey, "locked", throttleOptions);
        }
        else
        {
            // Eğer kilit (throttle key) VARSA, hiçbir event fırlatmıyoruz, konumu sadece veritabanında (Redis ana deposunda) güncelledik.
        }
        // TODO: İleride burada RabbitMQ'ya "Kurye Konum Değiştirdi" event'i fırlatacağız!
        // _publishEndpoint.Publish<DriverLocationUpdatedEvent>(...);

        return Ok(new { Message = "Konum başarıyla Redis'e işlendi.", CourierId = dto.CourierId });
    }

    [HttpGet("{courierId}")]
    public async Task<IActionResult> GetLocation(string courierId)
    {
        var locationData = await _cache.GetStringAsync(courierId);

        if (string.IsNullOrEmpty(locationData))
            return NotFound("Kurye bulunamadı veya aktif değil.");

        var location = JsonSerializer.Deserialize<CourierLocation>(locationData);
        return Ok(location);
    }
}