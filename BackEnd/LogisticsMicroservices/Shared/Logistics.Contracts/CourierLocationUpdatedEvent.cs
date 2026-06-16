using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistics.Contracts
{
    public record CourierLocationUpdatedEvent
    {
        public string CourierId { get; init; }
        public double Latitude { get; init; }
        public double Longitude { get; init; }
    }
}
