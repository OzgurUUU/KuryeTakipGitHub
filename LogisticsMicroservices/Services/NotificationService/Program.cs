using MassTransit;
using NotificationService.Consumers;
using NotificationService.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// 1. SignalR Servisini Kaydediyoruz
builder.Services.AddSignalR();

// 2. CORS Politikasý (Angular uygulamamýzýn baðlanabilmesi için þart)
builder.Services.AddCors(options => options.AddPolicy("CorsPolicy", policy =>
{
    policy.AllowAnyHeader()
          .AllowAnyMethod()
          .WithOrigins("http://localhost:4200") // Angular varsayýlan portu
          .AllowCredentials(); // SignalR için zorunlu
}));

// 3. MassTransit & RabbitMQ Kaydý
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<DriverAssignedConsumer>();
    x.AddConsumer<LocationUpdatedConsumer>();
    x.AddConsumer<OrderDeliveredConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.UseCors("CorsPolicy");

app.MapControllers();

// 4. SignalR Hub URL rotasýný belirliyoruz
app.MapHub<LogisticsHub>("/logisticsHub");

app.Run();