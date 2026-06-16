using Hindstorm;
using Hindstorm.Sample.ReadModels;
using Hindstorm.Sample.Shared;

namespace Hindstorm.Sample.Ordering;

// The Ordering bounded context: the customer-facing heart of the flow. An order is placed, then driven
// to completion or cancellation by policies reacting to events from the other contexts.

[Actor]
[Issues(typeof(PlaceOrder))]
[Issues(typeof(CancelOrder))]
public sealed class Customer;

[Command]
public sealed record PlaceOrder(Guid CustomerId, IReadOnlyList<Guid> ProductIds, Money Total);

[Command]
public sealed record CancelOrder(Guid OrderId);

[Command]
public sealed record CompleteOrder(Guid OrderId);

[DomainEvent]
[Updates(typeof(OrderStatusView))]
[Updates(typeof(CustomerOrderHistory))]
public sealed record OrderPlaced(Guid OrderId, Guid CustomerId, Money Total);

[DomainEvent]
[Updates(typeof(OrderStatusView))]
[Updates(typeof(CustomerOrderHistory))]
public sealed record OrderCancelled(Guid OrderId);

[DomainEvent]
[Updates(typeof(OrderStatusView))]
[Updates(typeof(CustomerOrderHistory))]
public sealed record OrderCompleted(Guid OrderId);

// Command-time business rules the Order aggregate checks before raising an event: invariants, not policies.
[Invariant]
public static class OrderMustHaveItems
{
    public static bool Holds(PlaceOrder command) => command.ProductIds.Count > 0;
}

[Invariant]
public static class WithinCreditLimit
{
    public static bool Holds(Guid customerId, Money total) => true;
}

[Aggregate]
public sealed class Order
{
    [Handles(typeof(PlaceOrder))]
    [Raises(typeof(OrderPlaced))]
    [Enforces(typeof(OrderMustHaveItems))]
    [Enforces(typeof(WithinCreditLimit))]
    public OrderPlaced Place(PlaceOrder command)
        => new(Guid.NewGuid(), command.CustomerId, command.Total);

    [Handles(typeof(CancelOrder))]
    [Raises(typeof(OrderCancelled))]
    public OrderCancelled Cancel(CancelOrder command) => new(command.OrderId);

    [Handles(typeof(CompleteOrder))]
    [Raises(typeof(OrderCompleted))]
    public OrderCompleted Complete(CompleteOrder command) => new(command.OrderId);
}
