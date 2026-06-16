using Logistics.Contracts;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;

namespace NotificationService.Consumers
{
    public class OrderDeliveredConsumer : IConsumer<OrderDeliveredEvent>
    {
        private readonly IHubContext<LogisticsHub> _hubContext;

        public OrderDeliveredConsumer(IHubContext<LogisticsHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task Consume(ConsumeContext<OrderDeliveredEvent> context)
        {
            // Angular'daki orderId bekleyen dinleyiciye string formatında gönderiyoruz
            await _hubContext.Clients.All.SendAsync("OrderDelivered", context.Message.OrderId.ToString());
        }
    }
}
