using Logistics.Contracts;
using MassTransit;
using OrderService.Data;

namespace OrderService.Consumer
{
    public class OrderDeliveredConsumer : IConsumer<OrderDeliveredEvent>
    {
        private readonly OrderDbContext _dbContext;
        public OrderDeliveredConsumer(OrderDbContext dbContext) { _dbContext = dbContext; }

        public async Task Consume(ConsumeContext<OrderDeliveredEvent> context)
        {
            var order = await _dbContext.Orders.FindAsync(context.Message.OrderId);
            if (order != null)
            {
                order.Status = "Delivered";
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"✅ Sipariş {order.Id} statüsü 'Delivered' yapıldı.");
            }
        }
    }
}
