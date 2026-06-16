namespace OrderService.Application.Common.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}
