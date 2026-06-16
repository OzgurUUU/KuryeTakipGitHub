using Logistics.Contracts;
using MassTransit;
using System.Text;
using System.Text.Json;

namespace Logistics.Simulator;

/// <summary>
/// Ana simülasyon döngüsü. İki sorumluluğu vardır:
///   1. Kurye hareketi  — Her 2 saniyede tüm kuryeler hedefe doğru ilerler.
///   2. Sipariş üretimi — Her 5 saniyede rastgele bir sipariş OrderService'e gönderilir.
///      Siparişler Gateway üzerinden OrderService'e HTTP POST olarak iletilir;
///      bu sayede gerçek kullanıcı davranışının tamamen aynısı tetiklenir:
///      DB kaydı → OrderCreatedEvent → DriverAssignedEvent → Kurye hareketi.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _httpClient;
    private readonly Random _random = new();
    private readonly CourierState _state;
    private readonly IBus _bus;

    // ── Sipariş üretim zamanlaması ────────────────────────────────────────────
    private DateTime _lastOrderCreatedAt = DateTime.MinValue;
    private static readonly TimeSpan OrderInterval = TimeSpan.FromSeconds(5);

    // ── Sipariş için rastgele veri havuzları ──────────────────────────────────
    private static readonly string[] CustomerNames =
    [
        "Ahmet Yılmaz",  "Fatma Kaya",    "Mehmet Demir",  "Ayşe Çelik",
        "Mustafa Şahin", "Zeynep Aydın",  "Hasan Arslan",  "Emine Doğan",
        "Ali Çetin",     "Hatice Kurt",   "İbrahim Öztürk","Havva Yıldız",
        "İsmail Güneş",  "Rabia Polat",   "Yusuf Erdoğan", "Hacer Koç",
        "Kadir Özdemir", "Şerife Aksoy",  "Ömer Akar",     "Gülsüm Tekin"
    ];

    private static readonly string[] ItemDescriptions =
    [
        "Elektronik Paket",   "Kırtasiye Malzemesi", "Medikal Ürün",
        "Gıda Siparişi",      "Tekstil Ürünü",       "Kitap ve Dergi",
        "Ev Eşyası",          "Kozmetik Paket",      "Spor Malzemesi",
        "Endüstriyel Parça",  "Eczane Ürünü",        "Bijuteri Paketi"
    ];

    // Adana/Çukurova coğrafi sınırları
    private const double LatMin = 36.95, LatMax = 37.05;
    private const double LonMin = 35.28, LonMax = 35.38;

    // Gateway HTTP adresi (Simulator → Gateway → OrderService)
    private const string OrderEndpoint      = "http://localhost:5229/api/orders";
    private const string LocationEndpoint   = "http://localhost:5229/api/tracking/update-location";

    public Worker(ILogger<Worker> logger, CourierState state, IBus bus)
    {
        _logger = logger;
        _state  = state;
        _bus    = bus;

        // Geliştirme ortamında SSL sertifika hatalarını atla
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        _httpClient = new HttpClient(handler);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Otonom Lojistik Simülasyonu Başlatıldı! ({Count} kurye aktif)", _state.Couriers.Count);

        while (!stoppingToken.IsCancellationRequested)
        {
            // ── 1. OTOMATİK SİPARİŞ ÜRETİMİ (her 5 saniyede bir) ─────────────
            if (DateTime.UtcNow - _lastOrderCreatedAt >= OrderInterval)
            {
                await CreateRandomOrderAsync(stoppingToken);
                _lastOrderCreatedAt = DateTime.UtcNow;
            }

            // ── 2. KURYE HAREKETİ (her iterasyonda tüm kuryeler) ──────────────
            foreach (var courier in _state.Couriers)
            {
                if (courier.TargetLatitude.HasValue && courier.TargetLongitude.HasValue)
                {
                    // Kurye hedefe kilitlenmiş — hedefe doğru ilerle
                    double diffLat  = courier.TargetLatitude.Value  - courier.Latitude;
                    double diffLon  = courier.TargetLongitude.Value - courier.Longitude;
                    double distance = Math.Sqrt(diffLat * diffLat + diffLon * diffLon);

                    if (distance < 0.0009)
                    {
                        // ── Hedefe ulaşıldı → Teslimat ──────────────────────
                        Guid? deliveredOrderId;

                        lock (courier.Lock)
                        {
                            _logger.LogInformation("✅ {CourierId} siparişi teslim etti! Kuyruk: {QueueCount} bekliyor.",
                                courier.CourierId, courier.TaskQueue.Count);

                            deliveredOrderId = courier.ActiveOrderId;

                            if (courier.TaskQueue.Count > 0)
                            {
                                var nextTask = courier.TaskQueue.Dequeue();
                                _logger.LogWarning("📦 SIRA SANA GELDİ! {CourierId} → #{OrderId}",
                                    courier.CourierId, nextTask.OrderId.ToString()[..8]);
                                courier.TargetLatitude  = nextTask.TargetLatitude;
                                courier.TargetLongitude = nextTask.TargetLongitude;
                                courier.ActiveOrderId   = nextTask.OrderId;
                            }
                            else
                            {
                                courier.TargetLatitude  = null;
                                courier.TargetLongitude = null;
                                courier.ActiveOrderId   = null;
                            }
                        }

                        if (deliveredOrderId.HasValue)
                        {
                            await _bus.Publish(new OrderDeliveredEvent
                            {
                                OrderId     = deliveredOrderId.Value,
                                DriverId    = courier.CourierId,
                                DeliveredAt = DateTime.UtcNow
                            }, stoppingToken);
                        }
                    }
                    else
                    {
                        // Hedefe doğru adım at (hız çarpanı: 0.0015 ≈ ~166m/iterasyon)
                        courier.Latitude  += (diffLat / distance) * 0.0015;
                        courier.Longitude += (diffLon / distance) * 0.0015;
                    }
                }
                else
                {
                    // Sipariş yoksa bölgede rastgele dolaş
                    courier.Latitude  += (_random.NextDouble() - 0.5) * 0.001;
                    courier.Longitude += (_random.NextDouble() - 0.5) * 0.001;
                }

                // ── Konum güncellemesini Gateway üzerinden Tracking servisine bildir ──
                var payload = new
                {
                    courierId   = courier.CourierId,
                    latitude    = courier.Latitude,
                    longitude   = courier.Longitude,
                    isAvailable = !courier.ActiveOrderId.HasValue
                };

                var json    = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    await _httpClient.PostAsync(LocationEndpoint, content, stoppingToken);
                }
                catch
                {
                    // Sessiz hata — tek bir konum kaybı kritik değil
                }
            }

            try { await Task.Delay(2000, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }

    /// <summary>
    /// Rastgele müşteri verisi ve Adana bölgesinde rastgele teslimat koordinatı ile
    /// Gateway üzerinden OrderService'e sipariş oluşturur.
    /// Tam HTTP akışı frontend ile özdeş olduğundan DB kaydı ve tüm event zinciri tetiklenir.
    /// </summary>
    private async Task CreateRandomOrderAsync(CancellationToken ct)
    {
        var order = new
        {
            customerName    = CustomerNames[_random.Next(CustomerNames.Length)],
            latitude        = LatMin + _random.NextDouble() * (LatMax - LatMin),
            longitude       = LonMin + _random.NextDouble() * (LonMax - LonMin),
            itemDescription = ItemDescriptions[_random.Next(ItemDescriptions.Length)]
        };

        var json    = JsonSerializer.Serialize(order);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(OrderEndpoint, content, ct);
            if (response.IsSuccessStatusCode)
                _logger.LogInformation("🛒 Yeni sipariş oluşturuldu → Müşteri: {Name} | Ürün: {Item}",
                    order.customerName, order.itemDescription);
            else
                _logger.LogWarning("⚠️ Sipariş oluşturulamadı. HTTP {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Sipariş isteği başarısız: {Message}", ex.Message);
        }
    }
}