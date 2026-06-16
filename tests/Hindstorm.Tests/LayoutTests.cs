using Hindstorm;
using Xunit;

namespace Hindstorm.Tests;

// Behavior of the left-to-right layout ordering shared by the Mermaid and DOT exporters:
// entry points (nodes with no incoming edge that start a flow) lead the output and, in DOT, are
// pinned to the source rank. Isolated nodes are not treated as entry points.
public sealed class LayoutTests
{
    // actor -> command -> aggregate; "actor" is the only entry point.
    private static DomainModel Flow() => new(
        new[]
        {
            new DomainNode("Ns.Aggregate", "Agg", ConceptKind.Aggregate, "Ns", null),
            new DomainNode("Ns.Command", "Cmd", ConceptKind.Command, "Ns", null),
            new DomainNode("Ns.Actor", "Actor", ConceptKind.Actor, "Ns", null),
        },
        new[]
        {
            new DomainEdge("Ns.Actor", "Ns.Command", RelationKind.Issues),
            new DomainEdge("Ns.Command", "Ns.Aggregate", RelationKind.Handles),
        });

    [Fact]
    public void Mermaid_declares_the_entry_point_before_its_downstream_nodes()
    {
        var mermaid = MermaidExporter.Export(Flow());

        var actor = mermaid.IndexOf("\"Actor\"", StringComparison.Ordinal);
        var command = mermaid.IndexOf("\"Cmd\"", StringComparison.Ordinal);
        var aggregate = mermaid.IndexOf("\"Agg\"", StringComparison.Ordinal);

        Assert.True(actor < command, "entry actor should be declared before the command");
        Assert.True(command < aggregate, "the command should be declared before the aggregate");
    }

    [Fact]
    public void Dot_pins_the_entry_point_to_the_source_rank()
    {
        var dot = DotExporter.Export(Flow());

        // The entry point is declared first, so it is n0, and only it is in the source rank.
        Assert.Contains("n0 [label=\"Actor\"", dot);
        Assert.Contains("{ rank=source; n0; }", dot);
    }

    [Fact]
    public void An_isolated_node_is_not_pinned_as_an_entry_point()
    {
        var model = new DomainModel(
            new[]
            {
                new DomainNode("Ns.Actor", "Actor", ConceptKind.Actor, "Ns", null),
                new DomainNode("Ns.Command", "Cmd", ConceptKind.Command, "Ns", null),
                new DomainNode("Ns.Lonely", "Lonely", ConceptKind.ValueObject, "Ns", null),
            },
            new[] { new DomainEdge("Ns.Actor", "Ns.Command", RelationKind.Issues) });

        var dot = DotExporter.Export(model);

        // Only the actor (n0) is an entry point; the isolated value object is not pinned.
        Assert.Contains("{ rank=source; n0; }", dot);
    }
}
