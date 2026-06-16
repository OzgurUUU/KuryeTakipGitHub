using MediatR;

namespace OrderService.Application.Orders.Commands.CreateOrder;

public record CreateOrderCommand(
    string CustomerName,
    double Latitude,
    double Longitude,
    string ItemDescription) : IRequest<Guid>;
