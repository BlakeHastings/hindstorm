using Hindstorm;
using Hindstorm.Sample.Inventory;
using Hindstorm.Sample.Ordering;
using Hindstorm.Sample.Payments;

namespace Hindstorm.Sample.Shipping;

// The Shipping bounded context, plus the cross-context process policies that drive the saga forward and
// the cancellation/completion paths. This is where most of the "whenever <event> then <command>" wiring
// lives.

[Command]
public sealed record ShipOrder(Guid OrderId);

[Command]
public sealed record SendOrderConfirmation(Guid OrderId);

[DomainEvent]
public sealed record OrderShipped(Guid OrderId);

[DomainEvent]
public sealed record OrderDelivered(Guid OrderId);

[Aggregate]
public sealed class Shipment
{
    [Handles(typeof(ShipOrder))]
    [Raises(typeof(OrderShipped))]
    public OrderShipped Ship(ShipOrder command) => new(command.OrderId);
}

// The carrier notifies us of delivery (an inbound webhook): an external system that raises a domain event.
[ExternalSystem]
[Raises(typeof(OrderDelivered))]
public sealed class Carrier;

// The email provider receives the confirmation command (an outbound call to an external system).
[ExternalSystem]
[Handles(typeof(SendOrderConfirmation))]
public sealed class EmailProvider;

[Policy]
public sealed class FulfillmentPolicy
{
    [ReactsTo(typeof(StockReserved))]
    [Issues(typeof(ShipOrder))]
    public ShipOrder OnStockReserved(StockReserved reserved) => new(reserved.OrderId);
}

[Policy]
public sealed class NotificationPolicy
{
    [ReactsTo(typeof(OrderShipped))]
    [Issues(typeof(SendOrderConfirmation))]
    public SendOrderConfirmation OnOrderShipped(OrderShipped shipped) => new(shipped.OrderId);
}

[Policy]
public sealed class CompletionPolicy
{
    [ReactsTo(typeof(OrderDelivered))]
    [Issues(typeof(CompleteOrder))]
    public CompleteOrder OnOrderDelivered(OrderDelivered delivered) => new(delivered.OrderId);
}

[Policy]
public sealed class CancellationPolicy
{
    [ReactsTo(typeof(PaymentDeclined))]
    [Issues(typeof(CancelOrder))]
    public CancelOrder OnPaymentDeclined(PaymentDeclined declined) => new(declined.OrderId);

    [ReactsTo(typeof(StockShortfall))]
    [Issues(typeof(CancelOrder))]
    public CancelOrder OnStockShortfall(StockShortfall shortfall) => new(shortfall.OrderId);
}
