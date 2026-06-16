using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Repositories;
using OrderService.Infrastructure.Messaging;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Persistence.Repositories;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrderDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IMessagePublisher, RabbitMqPublisher>();

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                // In production, get from configuration
                cfg.Host("localhost", "/", h => { h.Username("guest"); h.Password("guest"); });
            });
        });

        return services;
    }
}
