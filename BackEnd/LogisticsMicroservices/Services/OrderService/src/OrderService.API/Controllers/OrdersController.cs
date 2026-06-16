using Microsoft.AspNetCore.Mvc;
using MediatR;
using OrderService.Application.Orders.Commands.CreateOrder;
using OrderService.Application.Orders.Queries.GetOrderById;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
    {
        var orderId = await _sender.Send(command);
        return Ok(new { OrderId = orderId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderById(Guid id)
    {
        var query = new GetOrderByIdQuery(id);
        var order = await _sender.Send(query);

        if (order is null)
        {
            return NotFound(new { Message = $"Order with ID {id} not found." });
        }

        return Ok(order);
    }
}
