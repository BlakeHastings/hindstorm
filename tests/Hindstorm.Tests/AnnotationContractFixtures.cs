using Hindstorm;

namespace Hindstorm.Tests.AnnotationContract.Fixtures;

// Fixture concepts/relations applied to real types/methods so the reflection read-back tests
// exercise the same path the production scanner uses.
public sealed class OrderPlaced
{
}

public sealed class OrderRejected
{
}

[Aggregate(Name = "Order")]
public sealed class OrderAggregate
{
    // ASSUMES: AllowMultiple==true on relation attributes; two [Raises] on one method.
    [Raises(typeof(OrderPlaced))]
    [Raises(typeof(OrderRejected))]
    public void Place()
    {
    }
}
