using MediatR;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Order?>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Order?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        return await _orderRepository.GetByIdAsync(request.Id, cancellationToken);
    }
}
