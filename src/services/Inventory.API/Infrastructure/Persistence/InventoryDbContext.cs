using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext : DbContext
{
    private readonly IPublisher? _publisher;

    public InventoryDbContext(
        DbContextOptions<InventoryDbContext> options,
        IPublisher? publisher = null) : base(options)
    {
        _publisher = publisher;
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);

        if (_publisher is null)
        {
            return result;
        }

        var domainEvents = ChangeTracker.Entries<Product>()
            .SelectMany(entry => entry.Entity.DomainEvents)
            .ToList();

        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }

        foreach (var entry in ChangeTracker.Entries<Product>())
        {
            entry.Entity.ClearDomainEvents();
        }

        return result;
    }
}
