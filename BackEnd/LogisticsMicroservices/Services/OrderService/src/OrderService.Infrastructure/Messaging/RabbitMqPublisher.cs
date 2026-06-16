using MassTransit;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Infrastructure.Messaging;

public class RabbitMqPublisher : IMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public RabbitMqPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        await _publishEndpoint.Publish(message, cancellationToken);
    }
}
