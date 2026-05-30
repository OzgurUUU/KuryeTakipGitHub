using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistics.Contracts
{
    public record OrderDeliveredEvent
    {
        public Guid OrderId { get; init; }
        public string DriverId { get; init; }
        public DateTime DeliveredAt { get; init; }
    }
}
