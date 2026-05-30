using Logistics.Contracts;
using MassTransit;
using System.Text;
using System.Text.Json;

namespace Logistics.Simulator;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _httpClient;
    private readonly Random _random = new();
    private readonly CourierState _state;
    private readonly IBus _bus;

    public Worker(ILogger<Worker> logger, CourierState state , IBus bus)
    {
        _logger = logger;

        // 🚀 SSL BYPASS: Geliştirme ortamında HttpClient'ın sertifika hatalarına takılmasını engelliyoruz!
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        };
        _httpClient = new HttpClient(handler);
        _state = state;
        _bus = bus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Akıllı Kurye Simülasyonu Başlatıldı!");

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var courier in _state.Couriers)
            {
                
                // 🧠 EĞER KURYE HEDEFE KİLİTLENDİYSE:
                if (courier.TargetLatitude.HasValue && courier.TargetLongitude.HasValue)
                {
                    _logger.LogInformation($"✅ {courier.CourierId}, siparişe yöneldi!");
                    double diffLat = courier.TargetLatitude.Value - courier.Latitude;
                    double diffLon = courier.TargetLongitude.Value - courier.Longitude;
                    double distance = Math.Sqrt(diffLat * diffLat + diffLon * diffLon);
                    _logger.LogInformation($"mesafe {distance}");
                    // Hedefe çok yaklaştıysa (yaklaşık 10-20 metre) teslimatı yap
                    if (distance < 0.0009)
                    {
                        lock (courier.Lock)
                        {
                            _logger.LogInformation($"✅ {courier.CourierId}, siparişi hedefe teslim etti!");

                            var deliveredOrderId = courier.ActiveOrderId;

                            if (courier.TaskQueue.Count > 0)
                            {
                                var nextTask = courier.TaskQueue.Dequeue();
                                _logger.LogWarning($"🔄 SIRA SANA GELDİ! {courier.CourierId} kuyruktaki yeni siparişe: #{nextTask.OrderId.ToString()[..8]}");
                                courier.TargetLatitude = nextTask.TargetLatitude;
                                courier.TargetLongitude = nextTask.TargetLongitude;
                                courier.ActiveOrderId = nextTask.OrderId;
                            }
                            else
                            {
                                courier.TargetLatitude = null;
                                courier.TargetLongitude = null;
                                courier.ActiveOrderId = null;
                            }

                            // Publish lock dışında yapılacak (await kullanamayız lock içinde)
                            _ = _bus.Publish(new OrderDeliveredEvent
                            {
                                OrderId = deliveredOrderId!.Value,
                                DriverId = courier.CourierId,
                                DeliveredAt = DateTime.UtcNow
                            }, stoppingToken);
                        }
                    }
                    else
                    {
                        // Hedefe doğru direkt yürü (Hız çarpanı: 0.0015)
                        courier.Latitude += (diffLat / distance) * 0.0015;
                        courier.Longitude += (diffLon / distance) * 0.0015;
                    }
                }
                else
                {
                    // 💤 SİPARİŞ YOKSA: Olduğu yerde mahalle etrafında rastgele (serseri) gezinsin
                    courier.Latitude += (_random.NextDouble() - 0.5) * 0.001;
                    courier.Longitude += (_random.NextDouble() - 0.5) * 0.001;
                }

                // Gateway üzerinden Tracking servisine konumu bildir
                var payload = new { courierId = courier.CourierId, latitude = courier.Latitude, longitude = courier.Longitude };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try { await _httpClient.PostAsync("http://localhost:5229/api/tracking/update-location", content, stoppingToken); }
                catch { /* Sessiz hata */ }
            }

            try { await Task.Delay(2000, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }
}