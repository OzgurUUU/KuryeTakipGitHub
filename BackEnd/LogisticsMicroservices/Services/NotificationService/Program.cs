using MassTransit;
using NotificationService.Consumers;
using NotificationService.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// 1. SignalR Servisini Kaydediyoruz
builder.Services.AddSignalR();

// 2. CORS Politikasi (Angular uygulamamizin baglanabilmesi icin sart)
builder.Services.AddCors(options => options.AddPolicy("CorsPolicy", policy =>
{
    policy.AllowAnyHeader()
          .AllowAnyMethod()
          .WithOrigins("http://localhost:4200") // Angular varsayilan portu
          .AllowCredentials(); // SignalR icin zorunlu
}));

// 3. MassTransit & RabbitMQ Kaydi
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();
    x.AddConsumer<DriverAssignedConsumer>();
    x.AddConsumer<LocationUpdatedConsumer>();
    x.AddConsumer<OrderDeliveredConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h => { h.Username("guest"); h.Password("guest"); });

        cfg.ReceiveEndpoint("notification-order-created", e =>
        {
            e.ConfigureConsumer<OrderCreatedConsumer>(context);
        });
        cfg.ReceiveEndpoint("notification-driver-assigned", e =>
        {
            e.ConfigureConsumer<DriverAssignedConsumer>(context);
        });
        cfg.ReceiveEndpoint("notification-location-updated", e =>
        {
            e.ConfigureConsumer<LocationUpdatedConsumer>(context);
        });
        cfg.ReceiveEndpoint("notification-order-delivered", e =>
        {
            e.ConfigureConsumer<OrderDeliveredConsumer>(context);
        });
    });
});

var app = builder.Build();

app.UseCors("CorsPolicy");

app.MapControllers();

// 4. SignalR Hub URL rotasini belirliyoruz
app.MapHub<LogisticsHub>("/logisticsHub");

app.Run();
