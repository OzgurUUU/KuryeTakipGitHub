using FluentAssertions;
using Moq;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Orders.Commands.CreateOrder;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;
using Xunit;

namespace OrderService.Application.Tests;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IMessagePublisher> _messagePublisherMock;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _messagePublisherMock = new Mock<IMessagePublisher>();
        _handler = new CreateOrderCommandHandler(_orderRepositoryMock.Object, _messagePublisherMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldSaveOrderAndPublishEvent()
    {
        // Arrange
        var command = new CreateOrderCommand("Jane Doe", 41.0, 28.0, "Electronics");

        // Act
        var resultId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultId.Should().NotBeEmpty();

        // Verify that AddAsync was called exactly once
        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify that PublishAsync was called exactly once
        _messagePublisherMock.Verify(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
