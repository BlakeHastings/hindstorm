using System;
using System.Text.Json;
using Hindstorm;
using Xunit;

namespace Hindstorm.Tests;

// The dataflow (streaming) plane: processors and data events render with their own colors, a declared
// pipeline draws a distinct lane (not a bounded-context box), and a Translates edge is highlighted as the
// seam joining the dataflow plane to the transactional domain.
public sealed class DataflowTests
{
    // sensor -> (transforms) frame -> (feeds) endpointer -> (translates) UtteranceTranscribed.
    // The processors sit in a declared pipeline; the domain event sits in a bounded context.
    private static DomainModel Pipeline() => new(
        new[]
        {
            new DomainNode("Ns.Sensor", "Sensor", ConceptKind.Processor, "Ns", null, Pipeline: "AudioIngest"),
            new DomainNode("Ns.Frame", "Frame", ConceptKind.DataEvent, "Ns", null, Pipeline: "AudioIngest", AbstractionLevel: 0),
            new DomainNode("Ns.Endpointer", "Endpointer", ConceptKind.Processor, "Ns", null, Pipeline: "AudioIngest"),
            new DomainNode("Ns.Utterance", "UtteranceTranscribed", ConceptKind.DomainEvent, "Ns", null, Context: "Conversation", AbstractionLevel: 3),
        },
        new[]
        {
            new DomainEdge("Ns.Sensor", "Ns.Frame", RelationKind.Transforms),
            new DomainEdge("Ns.Sensor", "Ns.Endpointer", RelationKind.Feeds),
            new DomainEdge("Ns.Endpointer", "Ns.Utterance", RelationKind.Translates),
        });

    [Fact]
    public void Mermaid_colors_processor_and_data_event_distinctly_from_aggregate()
    {
        var mermaid = MermaidExporter.Export(Pipeline());

        Assert.Contains("classDef Processor", mermaid);
        Assert.Contains("classDef DataEvent", mermaid);
    }

    [Fact]
    public void Mermaid_draws_a_pipeline_lane_labelled_as_a_pipeline()
    {
        var mermaid = MermaidExporter.Export(Pipeline());

        // A pipeline lane is named "pipeline: <name>" so it reads apart from a bounded context.
        Assert.Contains("subgraph pipe0[\"pipeline: AudioIngest\"]", mermaid);
        // The transactional context still draws as a plain ctx box.
        Assert.Contains("subgraph ctx0[\"Conversation\"]", mermaid);
    }

    [Fact]
    public void Mermaid_highlights_the_translation_seam_edge()
    {
        var mermaid = MermaidExporter.Export(Pipeline());

        Assert.Contains("|translates|", mermaid);
        // The seam is styled with a linkStyle so it stands out as the plane boundary.
        Assert.Contains("linkStyle", mermaid);
    }

    [Fact]
    public void Mermaid_friendly_labels_for_the_dataflow_relations()
    {
        var mermaid = MermaidExporter.Export(Pipeline());

        Assert.Contains("|transforms|", mermaid);
        Assert.Contains("|feeds|", mermaid);
    }

    [Fact]
    public void Dot_draws_a_filled_pipeline_cluster_distinct_from_a_context_cluster()
    {
        var dot = DotExporter.Export(Pipeline());

        Assert.Contains("label=\"pipeline: AudioIngest\";", dot);
        Assert.Contains("label=\"Conversation\";", dot);
        // The pipeline lane is filled; the context box is dashed.
        Assert.Contains("fillcolor=\"#ECEFF1\";", dot);
        Assert.Contains("style=\"rounded,dashed\";", dot);
    }

    [Fact]
    public void Dot_highlights_the_translation_seam_edge_with_a_heavier_pen()
    {
        var dot = DotExporter.Export(Pipeline());

        Assert.Contains("label=\"translates\"", dot);
        Assert.Contains("penwidth=2", dot);
    }

    [Fact]
    public void Json_carries_pipeline_and_abstraction_level_and_omits_a_null_pipeline()
    {
        var model = new DomainModel(
            new[]
            {
                new DomainNode("Ns.Frame", "Frame", ConceptKind.DataEvent, "Ns", null, Pipeline: "AudioIngest", AbstractionLevel: 1),
                new DomainNode("Ns.Order", "Order", ConceptKind.Aggregate, "Ns", null),
            },
            Array.Empty<DomainEdge>());

        using var doc = JsonDocument.Parse(JsonExporter.Export(model));
        var nodes = doc.RootElement.GetProperty("nodes");

        Assert.Equal("AudioIngest", nodes[0].GetProperty("pipeline").GetString());
        Assert.Equal(1, nodes[0].GetProperty("abstractionLevel").GetInt32());
        // A node with no pipeline omits the property (null is not written).
        Assert.False(nodes[1].TryGetProperty("pipeline", out _));
    }

    [Fact]
    public void A_pipeline_wins_over_a_context_when_both_are_declared()
    {
        // A streaming concept that somehow declares both lands in its pipeline lane, not the domain box.
        var model = new DomainModel(
            new[]
            {
                new DomainNode("Ns.P", "P", ConceptKind.Processor, "Ns", null, Context: "Conversation", Pipeline: "AudioIngest"),
            },
            Array.Empty<DomainEdge>());

        var mermaid = MermaidExporter.Export(model);

        Assert.Contains("subgraph pipe0[\"pipeline: AudioIngest\"]", mermaid);
        Assert.DoesNotContain("subgraph ctx0[\"Conversation\"]", mermaid);
    }
}
