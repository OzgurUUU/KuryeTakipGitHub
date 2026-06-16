using System;
using System.Collections.Generic;

namespace Logistics.Simulator
{
    public class CourierSimModel
    {
        public string CourierId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Kuryenin gitmesi gereken hedef koordinatlar
        public double? TargetLatitude { get; set; }
        public double? TargetLongitude { get; set; }
        public Guid? ActiveOrderId { get; set; }

        public readonly object Lock = new();
        public Queue<CourierTask> TaskQueue { get; } = new();
    }

    public class CourierTask
    {
        public Guid OrderId { get; set; }
        public double TargetLatitude { get; set; }
        public double TargetLongitude { get; set; }
    }

    /// <summary>
    /// Simülatörün yönettiği kurye filosu.
    /// 15 kurye, Adana/Çukurova bölgesine yayılmış başlangıç koordinatlarıyla tanımlanmıştır.
    /// Her kurye bağımsız hareket eder; birden fazla sipariş alınca TaskQueue'ya alınır.
    /// </summary>
    public class CourierState
    {
        public List<CourierSimModel> Couriers { get; } = new()
        {
            // ── Merkez kuşak ──────────────────────────────────────────────────
            new CourierSimModel { CourierId = "kurye_ahmet",   Latitude = 37.0020, Longitude = 35.3220 },
            new CourierSimModel { CourierId = "kurye_mehmet",  Latitude = 37.0150, Longitude = 35.3330 },
            new CourierSimModel { CourierId = "kurye_ayse",    Latitude = 36.9950, Longitude = 35.3110 },
            new CourierSimModel { CourierId = "kurye_fatma",   Latitude = 37.0080, Longitude = 35.3450 },
            new CourierSimModel { CourierId = "kurye_mustafa", Latitude = 36.9880, Longitude = 35.3280 },

            // ── Kuzey kuşak ───────────────────────────────────────────────────
            new CourierSimModel { CourierId = "kurye_zeynep",  Latitude = 37.0300, Longitude = 35.3180 },
            new CourierSimModel { CourierId = "kurye_hasan",   Latitude = 37.0250, Longitude = 35.3500 },
            new CourierSimModel { CourierId = "kurye_emine",   Latitude = 37.0400, Longitude = 35.3350 },
            new CourierSimModel { CourierId = "kurye_ali",     Latitude = 37.0350, Longitude = 35.3050 },
            new CourierSimModel { CourierId = "kurye_hatice",  Latitude = 37.0450, Longitude = 35.3600 },

            // ── Güney kuşak ───────────────────────────────────────────────────
            new CourierSimModel { CourierId = "kurye_ibrahim",  Latitude = 36.9700, Longitude = 35.3200 },
            new CourierSimModel { CourierId = "kurye_havva",    Latitude = 36.9750, Longitude = 35.3480 },
            new CourierSimModel { CourierId = "kurye_ismail",   Latitude = 36.9600, Longitude = 35.3350 },
            new CourierSimModel { CourierId = "kurye_rabia",    Latitude = 36.9820, Longitude = 35.3600 },
            new CourierSimModel { CourierId = "kurye_yusuf",    Latitude = 36.9650, Longitude = 35.3080 },
        };
    }
}
