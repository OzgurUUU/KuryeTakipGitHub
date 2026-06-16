using MediatR;
using Logistics.Contracts;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMessagePublisher _messagePublisher;

    public CreateOrderCommandHandler(IOrderRepository orderRepository, IMessagePublisher messagePublisher)
    {
        _orderRepository = orderRepository;
        _messagePublisher = messagePublisher;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Create the domain entity
        var order = Order.Create(
            request.CustomerName,
            request.Latitude,
            request.Longitude,
            request.ItemDescription);

        // 2. Persist it
        await _orderRepository.AddAsync(order, cancellationToken);

        // 3. Publish Domain Event (Integration Event)
        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerName = order.CustomerName,
            DeliveryLatitude = order.Latitude,
            DeliveryLongitude = order.Longitude,
            ItemDescription = order.ItemDescription,
            CreatedAt = order.CreatedAt
        };

        await _messagePublisher.PublishAsync(orderCreatedEvent, cancellationToken);

        return order.Id;
    }
}
