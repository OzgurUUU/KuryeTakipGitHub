using Logistics.Contracts;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            if (courier != null)
            {
                if (!courier.ActiveOrderId.HasValue)
                {
                    _logger.LogWarning($"🚨 GÖREV ALINDI! {courier.CourierId}, hedefe kilitlendi!");
                    courier.TargetLatitude = msg.TargetLatitude;
                    courier.TargetLongitude = msg.TargetLongitude;
                    courier.ActiveOrderId = msg.OrderId;
                } 
                else
            {
                _logger.LogInformation($"⏳ KUYRUĞA EKLENDİ! {courier.CourierId} şu an meşgul. Sipariş #{msg.OrderId.ToString().Substring(0, 8)} kuryenin yerel kuyruğuna alındı.");

                // Yeni görevi kuryenin arkadaki kuyruğuna insanca ekliyoruz
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
