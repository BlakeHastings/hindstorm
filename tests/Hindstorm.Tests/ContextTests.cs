using System.Reflection;
using System.Text.Json;
using Hindstorm.Tests.ContextFixtures;
using Xunit;

namespace Hindstorm.Tests;

// Bounded-context resolution (explicit attribute vs the namespace rule) and the boundary-box clustering
// the exporters draw from it.
public sealed class ContextTests
{
    private const string FixtureNamespace = "Hindstorm.Tests.ContextFixtures";

    private static DomainModel ScanFixtures(System.Action<ScannerOptions>? extra = null) =>
        DomainModelScanner.Scan([Assembly.GetExecutingAssembly()], o =>
        {
            o.TypeFilter = t => t.Namespace == FixtureNamespace;
            extra?.Invoke(o);
        });

    private static DomainNode Node<T>(DomainModel model) =>
        model.Nodes.Single(n => n.Id == typeof(T).FullName);

    [Fact]
    public void Explicit_context_on_the_attribute_is_used()
    {
        var model = ScanFixtures();

        Assert.Equal("Billing", Node<Invoice>(model).Context);
    }

    [Fact]
    public void A_concept_without_a_context_has_none_when_no_namespace_rule_is_set()
    {
        var model = ScanFixtures();

        Assert.Null(Node<IssueInvoice>(model).Context);
    }

    [Fact]
    public void Namespace_rule_supplies_context_only_when_not_declared_explicitly()
    {
        var model = ScanFixtures(o => o.ContextFromNamespace = _ => "FromNamespace");

        // The explicit attribute wins; the rule fills in the one that did not declare a context.
        Assert.Equal("Billing", Node<Invoice>(model).Context);
        Assert.Equal("FromNamespace", Node<IssueInvoice>(model).Context);
    }

    [Fact]
    public void Json_includes_context_when_set_and_omits_it_when_null()
    {
        var model = new DomainModel(
            new[]
            {
                new DomainNode("Ns.A", "A", ConceptKind.Aggregate, "Ns", null, Context: "Sales"),
                new DomainNode("Ns.B", "B", ConceptKind.Command, "Ns", null),
            },
            System.Array.Empty<DomainEdge>());

        using var doc = JsonDocument.Parse(JsonExporter.Export(model));
        var nodes = doc.RootElement.GetProperty("nodes");

        Assert.Equal("Sales", nodes[0].GetProperty("context").GetString());
        Assert.False(nodes[1].TryGetProperty("context", out _));
    }

    private static DomainModel TwoContexts() => new(
        new[]
        {
            new DomainNode("Ns.Order", "Order", ConceptKind.Aggregate, "Ns", null, Context: "Ordering"),
            new DomainNode("Ns.Pay", "Pay", ConceptKind.Aggregate, "Ns", null, Context: "Payments"),
            new DomainNode("Ns.Loose", "Loose", ConceptKind.ValueObject, "Ns", null),
        },
        System.Array.Empty<DomainEdge>());

    [Fact]
    public void Mermaid_wraps_each_context_in_a_titled_subgraph()
    {
        var mermaid = MermaidExporter.Export(TwoContexts());

        Assert.Contains("subgraph ctx0[\"Ordering\"]", mermaid);
        Assert.Contains("subgraph ctx1[\"Payments\"]", mermaid);
        Assert.Contains("end", mermaid);
    }

    [Fact]
    public void Dot_wraps_each_context_in_a_labelled_cluster()
    {
        var dot = DotExporter.Export(TwoContexts());

        Assert.Contains("subgraph cluster_0 {", dot);
        Assert.Contains("label=\"Ordering\";", dot);
        Assert.Contains("label=\"Payments\";", dot);
    }

    [Fact]
    public void No_boundaries_are_drawn_when_no_context_is_declared()
    {
        var model = new DomainModel(
            new[] { new DomainNode("Ns.A", "A", ConceptKind.Aggregate, "Ns", null) },
            System.Array.Empty<DomainEdge>());

        Assert.DoesNotContain("subgraph", MermaidExporter.Export(model));
        Assert.DoesNotContain("cluster_", DotExporter.Export(model));
    }
}
