using Logistics.Contracts;
using MassTransit;
using System.Linq;
using System.Threading.Tasks;

namespace Logistics.Simulator
{
    public class DriverAssignedConsumer : IConsumer<DriverAssignedEvent>
    {
        private readonly CourierState _state;
        private readonly ILogger<DriverAssignedConsumer> _logger;

        public DriverAssignedConsumer(CourierState state, ILogger<DriverAssignedConsumer> logger)
        {
            _state = state;
            _logger = logger;
        }

        public Task Consume(ConsumeContext<DriverAssignedEvent> context)
        {
            var msg = context.Message;
            var courier = _state.Couriers.FirstOrDefault(c => c.CourierId == msg.DriverId);

            if (courier == null) return Task.CompletedTask;

            lock (courier.Lock)
            {
                if (!courier.ActiveOrderId.HasValue)
                {
                    _logger.LogWarning($"🚨 GÖREV ALINDI! {courier.CourierId} hedefe kilitlendi!");
                    courier.TargetLatitude = msg.TargetLatitude;
                    courier.TargetLongitude = msg.TargetLongitude;
                    courier.ActiveOrderId = msg.OrderId;
                }
                else
                {
                    _logger.LogInformation($"⏳ KUYRUĞA EKLENDİ! {courier.CourierId} meşgul. Sipariş #{msg.OrderId.ToString()[..8]} kuyruğa alındı.");
                    courier.TaskQueue.Enqueue(new CourierTask
                    {
                        OrderId = msg.OrderId,
                        TargetLatitude = msg.TargetLatitude,
                        TargetLongitude = msg.TargetLongitude
                    });
                }
            }

            return Task.CompletedTask;
        }
    }
}