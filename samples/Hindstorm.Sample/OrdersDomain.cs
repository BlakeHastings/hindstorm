using Hindstorm;

namespace Hindstorm.Sample;

// A small e-commerce ordering flow, labelled as an event-storming model. Read top to bottom it is the
// classic storming wall: a customer places an order, the Order aggregate raises OrderPlaced, a policy
// reacts and issues ShipOrder, the Shipment aggregate ships it, and a read model is kept up to date.

/// <summary>A generic event-handler interface, standing in for the host application's own building block.</summary>
public interface IDomainEventHandler<in TEvent>
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}

[Actor]
[Issues(typeof(PlaceOrder))]
public sealed class Customer;

[Command]
public sealed record PlaceOrder(Guid CustomerId, IReadOnlyList<Guid> ProductIds);

[Command]
public sealed record ShipOrder(Guid OrderId);

[DomainEvent]
[Updates(typeof(OrderSummaryView))]
public sealed record OrderPlaced(Guid OrderId, Guid CustomerId, Money Total);

[DomainEvent]
public sealed record OrderShipped(Guid OrderId, DateTimeOffset ShippedAt);

[ValueObject]
public sealed record Money(decimal Amount, string Currency);

[Policy]
public static class StockPolicy
{
    public static bool InStock(Guid productId) => true;
}

[Policy]
public sealed class FulfillmentPolicy
{
    [ReactsTo(typeof(OrderPlaced))]
    [Issues(typeof(ShipOrder))]
    public ShipOrder OnOrderPlaced(OrderPlaced placed) => new(placed.OrderId);
}

[ReadModel]
public sealed class OrderSummaryView
{
    public Guid OrderId { get; init; }
    public Money? Total { get; init; }
}

[Aggregate]
public sealed class Order
{
    [Handles(typeof(PlaceOrder))]
    [Raises(typeof(OrderPlaced))]
    [Enforces(typeof(StockPolicy))]
    public OrderPlaced Place(PlaceOrder command)
        => new(Guid.NewGuid(), command.CustomerId, new Money(0m, "USD"));
}

[Aggregate]
public sealed class Shipment
{
    [Handles(typeof(ShipOrder))]
    [Raises(typeof(OrderShipped))]
    public OrderShipped Ship(ShipOrder command) => new(command.OrderId, DateTimeOffset.UtcNow);
}

// Untagged handler, discovered through the handler interface rather than a concept attribute.
public sealed class OrderSummaryProjection : IDomainEventHandler<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced domainEvent, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
