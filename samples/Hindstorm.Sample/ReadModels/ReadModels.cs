using Hindstorm;
using Hindstorm.Sample.Inventory;
using Hindstorm.Sample.Shared;

namespace Hindstorm.Sample.ReadModels;

// Query-side projections. OrderStatusView and CustomerOrderHistory are fed by the [Updates] edges on the
// ordering events. InventoryLevelsProjection is discovered through the handler interface instead, showing
// how ScannerOptions.HandlerInterface recovers event -> handler reaction edges with no annotation.

[ReadModel]
public sealed class OrderStatusView
{
    public Guid OrderId { get; init; }
    public string? Status { get; init; }
}

[ReadModel]
public sealed class CustomerOrderHistory
{
    public Guid CustomerId { get; init; }
}

public sealed class InventoryLevelsProjection : IDomainEventHandler<StockReserved>
{
    public Task HandleAsync(StockReserved domainEvent, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
