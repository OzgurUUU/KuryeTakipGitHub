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

        // Veriyi Redis'e yazıyoruz (Örn Key: DriverTracking_Courier123)
        await _cache.SetStringAsync(dto.CourierId, locationJson, options);

        await _publishEndpoint.Publish(new CourierLocationUpdatedEvent
        {
            CourierId = dto.CourierId,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        });
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