using Hindstorm;

namespace Hindstorm.Sample.Shared;

// Building blocks shared across the bounded contexts. The handler interface stands in for the host
// application's own event-handling seam; Hindstorm is pointed at it via ScannerOptions.HandlerInterface.

/// <summary>A generic event-handler interface, standing in for the host application's own building block.</summary>
public interface IDomainEventHandler<in TEvent>
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}

[ValueObject]
public sealed record Money(decimal Amount, string Currency);
