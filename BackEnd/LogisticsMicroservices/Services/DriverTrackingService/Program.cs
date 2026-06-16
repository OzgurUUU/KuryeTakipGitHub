using MassTransit;
using DriverTrackingService.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();
    x.AddConsumer<DriverAssignedConsumer>();   // ? yeni
    x.AddConsumer<OrderDeliveredConsumer>();   // ? yeni

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("tracking-order-created", e =>
        {
            e.ConfigureConsumer<OrderCreatedConsumer>(context);
        });
        cfg.ReceiveEndpoint("tracking-driver-assigned", e =>
        {
            e.ConfigureConsumer<DriverAssignedConsumer>(context);
        });
        cfg.ReceiveEndpoint("tracking-order-delivered", e =>
        {
            e.ConfigureConsumer<OrderDeliveredConsumer>(context);
        });
    });
});

builder.Services.AddControllers();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "DriverTracking_";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();