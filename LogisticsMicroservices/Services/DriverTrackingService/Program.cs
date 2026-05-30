using MassTransit;
using DriverTrackingService.Consumers;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddMassTransit(x =>
{
    // Oluþturduðumuz Consumer sýnýfýný MassTransit'e tanýtýyoruz
    x.AddConsumer<OrderCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // RabbitMQ üzerinde otomatik bir kuyruk (queue) oluþturur ve consumer'ý baðlar
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddControllers();

builder.Services.AddStackExchangeRedisCache(options =>
{
    // Þimdilik localhost, Docker'a geçince "redis:6379" olacak
    options.Configuration = "localhost:6379";
    options.InstanceName = "DriverTracking_";
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
