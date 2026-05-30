using OrderService.Data;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using MassTransit;
using OrderService.Consumer;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderDbConnection")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<DriverAssignedConsumer>();
    x.AddConsumer<OrderDeliveredConsumer>();
    // RabbitMQ kullanacaðýmýzý belirtiyoruz
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

