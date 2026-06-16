using Hindstorm;
using Hindstorm.Sample.Ordering;

namespace Hindstorm.Sample.Payments;

// The Payments bounded context. Authorization is delegated to an external gateway; refunds are recorded
// on an internal Payment aggregate. Policies bridge ordering events to payment commands.

[Command]
public sealed record AuthorizePayment(Guid OrderId);

[Command]
public sealed record RefundPayment(Guid OrderId);

[DomainEvent]
public sealed record PaymentAuthorized(Guid OrderId);

[DomainEvent]
public sealed record PaymentDeclined(Guid OrderId, string Reason);

[DomainEvent]
public sealed record PaymentRefunded(Guid OrderId);

// The external payment provider: it receives the authorization command and reports the outcome back as
// an event. Modeling both directions shows how an external system sits at the edge of the flow.
[ExternalSystem]
[Handles(typeof(AuthorizePayment))]
[Raises(typeof(PaymentAuthorized))]
[Raises(typeof(PaymentDeclined))]
public sealed class PaymentGateway;

[Aggregate]
public sealed class Payment
{
    [Handles(typeof(RefundPayment))]
    [Raises(typeof(PaymentRefunded))]
    public PaymentRefunded Refund(RefundPayment command) => new(command.OrderId);
}

[Policy]
public sealed class PaymentPolicy
{
    [ReactsTo(typeof(OrderPlaced))]
    [Issues(typeof(AuthorizePayment))]
    public AuthorizePayment OnOrderPlaced(OrderPlaced placed) => new(placed.OrderId);
}

[Policy]
public sealed class RefundPolicy
{
    [ReactsTo(typeof(OrderCancelled))]
    [Issues(typeof(RefundPayment))]
    public RefundPayment OnOrderCancelled(OrderCancelled cancelled) => new(cancelled.OrderId);
}
