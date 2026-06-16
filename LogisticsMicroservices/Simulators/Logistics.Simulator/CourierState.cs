using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistics.Simulator
{

    public class CourierSimModel
    {
        public string CourierId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // 🚀 YENİ: Kuryenin gitmesi gereken hedef koordinatlar
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
    public class CourierState
    {
        public List<CourierSimModel> Couriers { get; } = new()
    {
        new CourierSimModel { CourierId = "kurye_ahmet", Latitude = 37.0020, Longitude = 35.3220 },
        new CourierSimModel { CourierId = "kurye_mehmet", Latitude = 37.0150, Longitude = 35.3330 },
        new CourierSimModel { CourierId = "kurye_ayse", Latitude = 36.9950, Longitude = 35.3110 }
    };
        
    }
}

