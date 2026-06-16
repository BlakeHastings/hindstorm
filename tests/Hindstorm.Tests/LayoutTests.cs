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

    // order -> credit (terminal invariant) and order -> placed (continues: placed -> view).
    private static DomainModel BranchingFlow() => new(
        new[]
        {
            new DomainNode("Ns.Order", "Order", ConceptKind.Aggregate, "Ns", null),
            new DomainNode("Ns.Credit", "Credit", ConceptKind.Invariant, "Ns", null),
            new DomainNode("Ns.Placed", "Placed", ConceptKind.DomainEvent, "Ns", null),
            new DomainNode("Ns.View", "View", ConceptKind.ReadModel, "Ns", null),
        },
        new[]
        {
            new DomainEdge("Ns.Order", "Ns.Placed", RelationKind.Raises),
            new DomainEdge("Ns.Order", "Ns.Credit", RelationKind.Enforces),
            new DomainEdge("Ns.Placed", "Ns.View", RelationKind.Updates),
        });

    [Fact]
    public void Edges_to_terminal_nodes_are_emitted_before_edges_that_continue_the_flow()
    {
        // Even though the raises edge is declared first in the model, the enforces edge (to a terminal
        // invariant) is ordered first so the dead-end floats above the continuing flow.
        var dot = DotExporter.Export(BranchingFlow());
        var mermaid = MermaidExporter.Export(BranchingFlow());

        Assert.True(
            dot.IndexOf("label=\"enforces\"", StringComparison.Ordinal) <
            dot.IndexOf("label=\"raises\"", StringComparison.Ordinal),
            "DOT should emit the terminal (enforces) edge before the continuing (raises) edge");
        Assert.True(
            mermaid.IndexOf("|enforces|", StringComparison.Ordinal) <
            mermaid.IndexOf("|raises|", StringComparison.Ordinal),
            "Mermaid should emit the terminal (enforces) edge before the continuing (raises) edge");
    }

    [Fact]
    public void Dot_declares_out_edge_ordering()
    {
        Assert.Contains("ordering=out;", DotExporter.Export(BranchingFlow()));
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
