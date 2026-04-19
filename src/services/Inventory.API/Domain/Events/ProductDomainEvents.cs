using MediatR;

namespace Inventory.Domain.Events;

/// <summary>
/// Base marker interface for all domain events.
/// Domain events are raised inside the aggregate and dispatched
/// via MediatR after the transaction commits.
/// </summary>
public interface IDomainEvent : INotification { }

/// <summary>Raised when a new product is added to inventory.</summary>
public record ProductCreatedEvent(
    Guid ProductId,
    string Name,
    string SKU,
    string Category,
    DateTime OccurredAt
) : IDomainEvent;

/// <summary>Raised when stock quantity changes (positive or negative).</summary>
public record StockAdjustedEvent(
    Guid ProductId,
    string SKU,
    int PreviousQuantity,
    int NewQuantity,
    int Delta,
    string Reason,
    DateTime OccurredAt
) : IDomainEvent;

/// <summary>Raised when stock falls at or below the reorder threshold.</summary>
public record LowStockDetectedEvent(
    Guid ProductId,
    string ProductName,
    string SKU,
    string Category,
    int CurrentQuantity,
    int ReorderLevel,
    DateTime OccurredAt
) : IDomainEvent;

/// <summary>Raised when a product is discontinued / removed from catalogue.</summary>
public record ProductDiscontinuedEvent(
    Guid ProductId,
    string SKU,
    DateTime OccurredAt
) : IDomainEvent;

/// <summary>Raised when the unit price of a product is updated.</summary>
public record ProductPriceUpdatedEvent(
    Guid ProductId,
    string SKU,
    decimal OldPrice,
    decimal NewPrice,
    string Currency,
    DateTime OccurredAt
) : IDomainEvent;
