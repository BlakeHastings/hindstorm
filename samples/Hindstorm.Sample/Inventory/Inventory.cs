using Hindstorm;
using Hindstorm.Sample.Payments;

namespace Hindstorm.Sample.Inventory;

// The Inventory bounded context. Once payment is authorized, stock is reserved; if it cannot be, a
// shortfall event sends the order down the cancellation path.

[Command]
public sealed record ReserveStock(Guid OrderId);

[DomainEvent]
public sealed record StockReserved(Guid OrderId);

[DomainEvent]
public sealed record StockShortfall(Guid OrderId);

[Invariant]
public static class SufficientStock
{
    public static bool Holds(ReserveStock command) => true;
}

[Aggregate]
public sealed class Warehouse
{
    [Handles(typeof(ReserveStock))]
    [Raises(typeof(StockReserved))]
    [Raises(typeof(StockShortfall))]
    [Enforces(typeof(SufficientStock))]
    public StockReserved Reserve(ReserveStock command) => new(command.OrderId);
}

[Policy]
public sealed class ReservationPolicy
{
    [ReactsTo(typeof(PaymentAuthorized))]
    [Issues(typeof(ReserveStock))]
    public ReserveStock OnPaymentAuthorized(PaymentAuthorized authorized) => new(authorized.OrderId);
}
