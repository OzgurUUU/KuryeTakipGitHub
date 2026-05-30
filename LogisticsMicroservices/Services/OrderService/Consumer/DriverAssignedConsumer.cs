using Logistics.Contracts;
using MassTransit;
using OrderService.Data;

namespace OrderService.Consumer
{
    public class DriverAssignedConsumer : IConsumer<DriverAssignedEvent>
    {
        private readonly OrderDbContext _dbContext;
        public DriverAssignedConsumer(OrderDbContext dbContext) { _dbContext = dbContext; }

        public async Task Consume(ConsumeContext<DriverAssignedEvent> context)
        {
            var order = await _dbContext.Orders.FindAsync(context.Message.OrderId);
            if (order != null)
            {
                order.Status = "Assigned";  
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"📦 Sipariş {order.Id} statüsü 'Assigned' yapıldı.");
            }
        }
    }
}
