using Logistics.Simulator;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);


builder.Services.AddSingleton<CourierState>();

// Simülatöre MassTransit ekliyoruz ki RabbitMQ'dan görevleri alabilsin
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<DriverAssignedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h => { h.Username("guest"); h.Password("guest"); });

        cfg.ReceiveEndpoint("simulator-driver-assigned", e =>
        {
            e.ConfigureConsumer<DriverAssignedConsumer>(context);
        });
    });
});
builder.Services.AddHostedService<Worker>();
var host = builder.Build();
host.Run();
