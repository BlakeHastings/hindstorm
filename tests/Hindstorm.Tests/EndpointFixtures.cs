using Hindstorm;

namespace Hindstorm.Tests.EndpointFixtures;

// A minimal annotated slice so the endpoint serves a non-empty model.
[Aggregate]
public sealed class Account
{
    [Raises(typeof(AccountOpened))]
    public AccountOpened Open() => new();
}

[DomainEvent]
public sealed record AccountOpened;
