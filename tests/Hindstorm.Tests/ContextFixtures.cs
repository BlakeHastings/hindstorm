using Hindstorm;

namespace Hindstorm.Tests.ContextFixtures;

// Fixtures for bounded-context resolution: one concept states its context explicitly, one leaves it
// to a namespace rule.

[Aggregate(Context = "Billing")]
public sealed class Invoice;

[Command]
public sealed record IssueInvoice;
