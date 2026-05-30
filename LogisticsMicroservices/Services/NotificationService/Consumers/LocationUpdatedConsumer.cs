using Logistics.Contracts;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;

namespace NotificationService.Consumers
{
    public class LocationUpdatedConsumer : IConsumer<CourierLocationUpdatedEvent>
    {
        private readonly IHubContext<LogisticsHub> _hubContext;
        private readonly ILogger<DriverAssignedConsumer> _logger;

        // SignalR IHubContext enjekte ederek WebSocket hatlarına erişiyoruz
        public LocationUpdatedConsumer(IHubContext<LogisticsHub> hubContext, ILogger<DriverAssignedConsumer> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CourierLocationUpdatedEvent> context)
        {
            var msg = context.Message;

            // Konsolda verinin aktığını görebilmek için minik bir log (Çok hızlı akacağı için istersen yoruma alabilirsin)
            // _logger.LogInformation($"📍 Sinyal Alındı: Kurye {msg.CourierId} -> {msg.Latitude}, {msg.Longitude}");

            // 🚀 BÜYÜLÜ AN: RabbitMQ'dan gelen anlık konumu, SignalR üzerinden Angular'daki 'ReceiveLocation' metoduna fırlatıyoruz!
            await _hubContext.Clients.All.SendAsync("ReceiveLocation", msg.CourierId, msg.Latitude, msg.Longitude, "Motorcycle");
        }
    }
}
