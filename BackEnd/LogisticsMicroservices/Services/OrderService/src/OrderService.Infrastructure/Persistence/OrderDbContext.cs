using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Persistence;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.CustomerName).HasMaxLength(100).IsRequired();
            builder.Property(o => o.ItemDescription).HasMaxLength(500).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
