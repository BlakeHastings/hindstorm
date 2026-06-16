using Hindstorm;
using Xunit;
using System.Text.Json;

namespace Hindstorm.Tests;

// Contract-only tests for MermaidExporter, DotExporter, JsonExporter.
// Derived solely from the documented contract packet, not from the implementations.
//
// ASSUMES (low-impact, strongly implied by the contract):
//  - classDef block names are the exact ConceptKind member names ("Aggregate", "Command", ...).
//  - the inferred CSS class is literally named "inferred".
//  - Mermaid node lines reference a kind's class by that kind's name and inferred nodes
//    reference "inferred" instead of their kind class.
//  - JSON enum members are written exactly as the C# member name (e.g. "ReactsTo", "Raises").
public sealed class ExporterContractTests
{
    private static DomainNode Node(
        string id,
        string name,
        ConceptKind kind,
        string? ns = "Ns",
        string? description = null,
        bool inferred = false)
        => new DomainNode(id, name, kind, ns, description, inferred);

    private static DomainModel ModelWith(DomainNode node, params DomainEdge[] edges)
        => new DomainModel(new[] { node }, edges);

    // ---------------------------------------------------------------------
    // MERMAID
    // ---------------------------------------------------------------------

    [Fact]
    public void Mermaid_DefaultsToElk_And_ContainsFlowchartLR()
    {
        var model = ModelWith(Node("Ns.Skill", "Skill", ConceptKind.Aggregate));

        string mermaid = MermaidExporter.Export(model);

        Assert.StartsWith("%%{init:", mermaid.TrimStart());
        Assert.Contains("\"layout\": \"elk\"", mermaid);
        Assert.Contains("flowchart LR", mermaid);
    }

    [Fact]
    public void Mermaid_DagreOption_OmitsElkDirective_And_StartsWithFlowchart()
    {
        var model = ModelWith(Node("Ns.Skill", "Skill", ConceptKind.Aggregate));

        string mermaid = MermaidExporter.Export(model, MermaidLayout.Dagre);

        Assert.StartsWith("flowchart LR", mermaid.TrimStart());
        Assert.DoesNotContain("elk", mermaid);
    }

    [Fact]
    public void Mermaid_ColorsActorAndAggregateDistinctly()
    {
        var model = new DomainModel(
            new[]
            {
                Node("Ns.Actor", "Actor", ConceptKind.Actor),
                Node("Ns.Agg", "Agg", ConceptKind.Aggregate),
            },
            System.Array.Empty<DomainEdge>());

        string mermaid = MermaidExporter.Export(model);

        Assert.NotEqual(FillOf(mermaid, "Actor"), FillOf(mermaid, "Aggregate"));
    }

    // Extracts the fill color from a "classDef <kind> fill:#XXXXXX,..." line.
    private static string FillOf(string mermaid, string kind)
    {
        var marker = $"classDef {kind} fill:";
        var start = mermaid.IndexOf(marker, System.StringComparison.Ordinal) + marker.Length;
        var end = mermaid.IndexOf(',', start);
        return mermaid.Substring(start, end - start);
    }

    [Fact]
    public void Mermaid_EmitsClassDef_ForKindPresent_And_NodeReferencesThatClass()
    {
        var model = ModelWith(Node("Ns.Skill", "Skill", ConceptKind.Aggregate));

        string mermaid = MermaidExporter.Export(model);

        Assert.Contains("classDef Aggregate", mermaid);
        // The node line should reference the Aggregate class for this kind.
        Assert.Contains("Aggregate", mermaid);
    }

    [Fact]
    public void Mermaid_DistinctKinds_EachEmitClassDef()
    {
        var model = new DomainModel(
            new[]
            {
                Node("Ns.Skill", "Skill", ConceptKind.Aggregate),
                Node("Ns.Push", "Push", ConceptKind.Command),
            },
            System.Array.Empty<DomainEdge>());

        string mermaid = MermaidExporter.Export(model);

        Assert.Contains("classDef Aggregate", mermaid);
        Assert.Contains("classDef Command", mermaid);
    }

    [Fact]
    public void Mermaid_InferredNode_UsesInferredClass()
    {
        var model = ModelWith(
            Node("Ns.Guess", "Guess", ConceptKind.Aggregate, inferred: true));

        string mermaid = MermaidExporter.Export(model);

        // The inferred class must be defined / referenced for an inferred node.
        Assert.Contains("inferred", mermaid);
    }

    [Fact]
    public void Mermaid_Edge_WithoutLabel_RendersLowerCaseRelation_Raises()
    {
        var model = new DomainModel(
            new[] { Node("Ns.Skill", "Skill", ConceptKind.Aggregate) },
            new[] { new DomainEdge("Ns.Skill", "Ns.Pushed", RelationKind.Raises, "PushVersion") });

        string mermaid = MermaidExporter.Export(model);

        Assert.Contains("|raises|", mermaid);
    }

    [Fact]
    public void Mermaid_Edge_WithoutLabel_RendersLowerCaseRelation_ReactsTo_WithSpace()
    {
        var model = new DomainModel(
            new[] { Node("Ns.Policy", "Policy", ConceptKind.Policy) },
            new[] { new DomainEdge("Ns.Policy", "Ns.Event", RelationKind.ReactsTo, "OnEvent") });

        string mermaid = MermaidExporter.Export(model);

        Assert.Contains("|reacts to|", mermaid);
    }

    [Fact]
    public void Mermaid_Edge_WithExplicitLabel_RendersLabel_NotRelationName()
    {
        var model = new DomainModel(
            new[] { Node("Ns.Skill", "Skill", ConceptKind.Aggregate) },
            new[] { new DomainEdge("Ns.Skill", "Ns.Pushed", RelationKind.Raises, "PushVersion", "custom-label") });

        string mermaid = MermaidExporter.Export(model);

        Assert.Contains("|custom-label|", mermaid);
        Assert.DoesNotContain("|raises|", mermaid);
    }

    [Fact]
    public void Mermaid_NodeName_WithDoubleQuote_IsEscapedTo_Quot_Entity()
    {
        // Name carries the quote; Id and namespace are quote-free so any raw quote
        // in the output can only have come from the node name.
        var model = ModelWith(Node("Ns.Quoted", "Sa\"id", ConceptKind.Aggregate));

        string mermaid = MermaidExporter.Export(model);

        Assert.Contains("&quot;", mermaid);
        // The raw name with an unescaped quote must not survive.
        Assert.DoesNotContain("Sa\"id", mermaid);
    }

    [Fact]
    public void Mermaid_EmptyModel_DoesNotThrow_And_StartsWithFlowchart()
    {
        string mermaid = MermaidExporter.Export(DomainModel.Empty);

        Assert.Contains("flowchart LR", mermaid);
    }

    // ---------------------------------------------------------------------
    // DOT
    // ---------------------------------------------------------------------

    [Fact]
    public void Dot_Output_StartsWith_DigraphHeader()
    {
        var model = ModelWith(Node("Ns.Skill", "Skill", ConceptKind.Aggregate));

        string dot = DotExporter.Export(model);

        Assert.StartsWith("digraph DomainModel {", dot.TrimStart());
    }

    [Fact]
    public void Dot_Output_Contains_RankdirLR()
    {
        var model = ModelWith(Node("Ns.Skill", "Skill", ConceptKind.Aggregate));

        string dot = DotExporter.Export(model);

        Assert.Contains("rankdir=LR;", dot);
    }

    [Fact]
    public void Dot_Edge_Renders_Arrow_And_LabelFromRelation()
    {
        var model = new DomainModel(
            new[] { Node("Ns.Skill", "Skill", ConceptKind.Aggregate) },
            new[] { new DomainEdge("Ns.Skill", "Ns.Pushed", RelationKind.Raises, "PushVersion") });

        string dot = DotExporter.Export(model);

        Assert.Contains("->", dot);
        Assert.Contains("label=\"raises\"", dot);
    }

    [Fact]
    public void Dot_Edge_WithExplicitLabel_UsesLabel_NotRelationName()
    {
        var model = new DomainModel(
            new[] { Node("Ns.Skill", "Skill", ConceptKind.Aggregate) },
            new[] { new DomainEdge("Ns.Skill", "Ns.Pushed", RelationKind.Raises, "PushVersion", "edge-text") });

        string dot = DotExporter.Export(model);

        Assert.Contains("label=\"edge-text\"", dot);
        Assert.DoesNotContain("label=\"raises\"", dot);
    }

    [Fact]
    public void Dot_NodeName_WithDoubleQuote_IsBackslashEscaped()
    {
        var model = ModelWith(Node("Ns.Quoted", "Sa\"id", ConceptKind.Aggregate));

        string dot = DotExporter.Export(model);

        // Backslash-escaped quote sequence must be present.
        Assert.Contains("\\\"", dot);
        // The raw unescaped name must not survive verbatim.
        Assert.DoesNotContain("Sa\"id", dot);
    }

    [Fact]
    public void Dot_EmptyModel_DoesNotThrow_And_ProducesDigraph()
    {
        string dot = DotExporter.Export(DomainModel.Empty);

        Assert.StartsWith("digraph DomainModel {", dot.TrimStart());
    }

    // ---------------------------------------------------------------------
    // JSON
    // ---------------------------------------------------------------------

    [Fact]
    public void Json_Output_ParsesAsValidJson()
    {
        var model = ModelWith(Node("Ns.Skill", "Skill", ConceptKind.Aggregate));

        string json = JsonExporter.Export(model);

        // Throws if invalid; assertion is the successful parse.
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
    }

    [Fact]
    public void Json_NodesAndEdges_ArrayLengths_MatchModel()
    {
        var model = new DomainModel(
            new[]
            {
                Node("Ns.Skill", "Skill", ConceptKind.Aggregate),
                Node("Ns.Push", "Push", ConceptKind.Command),
            },
            new[]
            {
                new DomainEdge("Ns.Skill", "Ns.Push", RelationKind.Issues, "Issue"),
            });

        string json = JsonExporter.Export(model);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(2, doc.RootElement.GetProperty("nodes").GetArrayLength());
        Assert.Equal(1, doc.RootElement.GetProperty("edges").GetArrayLength());
    }

    [Fact]
    public void Json_Relation_SerializesAs_MemberName_String()
    {
        var model = new DomainModel(
            new[] { Node("Ns.Policy", "Policy", ConceptKind.Policy) },
            new[] { new DomainEdge("Ns.Policy", "Ns.Event", RelationKind.ReactsTo, "OnEvent") });

        string json = JsonExporter.Export(model);

        using var doc = JsonDocument.Parse(json);
        var relation = doc.RootElement.GetProperty("edges")[0].GetProperty("relation");
        Assert.Equal(JsonValueKind.String, relation.ValueKind);
        Assert.Equal("ReactsTo", relation.GetString());
    }

    [Fact]
    public void Json_Relation_IsNotFriendlyLowerCase_And_NotBareNumber()
    {
        var model = new DomainModel(
            new[] { Node("Ns.Skill", "Skill", ConceptKind.Aggregate) },
            new[] { new DomainEdge("Ns.Skill", "Ns.Pushed", RelationKind.Raises, "PushVersion") });

        string json = JsonExporter.Export(model);

        Assert.Contains("\"Raises\"", json);
        // Must not use the friendly Mermaid/DOT lower-case form for the relation value.
        Assert.DoesNotContain("\"raises\"", json);

        using var doc = JsonDocument.Parse(json);
        var relation = doc.RootElement.GetProperty("edges")[0].GetProperty("relation");
        Assert.NotEqual(JsonValueKind.Number, relation.ValueKind);
    }

    [Fact]
    public void Json_NullDescription_OmitsDescriptionProperty()
    {
        var model = ModelWith(Node("Ns.Skill", "Skill", ConceptKind.Aggregate, description: null));

        string json = JsonExporter.Export(model);

        using var doc = JsonDocument.Parse(json);
        var node = doc.RootElement.GetProperty("nodes")[0];
        Assert.False(node.TryGetProperty("description", out _));
    }

    [Fact]
    public void Json_SetDescription_IncludesDescriptionProperty()
    {
        var model = ModelWith(Node("Ns.Skill", "Skill", ConceptKind.Aggregate, description: "A skill aggregate"));

        string json = JsonExporter.Export(model);

        using var doc = JsonDocument.Parse(json);
        var node = doc.RootElement.GetProperty("nodes")[0];
        Assert.True(node.TryGetProperty("description", out var desc));
        Assert.Equal("A skill aggregate", desc.GetString());
    }

    [Fact]
    public void Json_PropertyNames_AreCamelCase_OnEdge()
    {
        var model = new DomainModel(
            new[] { Node("Ns.Skill", "Skill", ConceptKind.Aggregate) },
            new[] { new DomainEdge("Ns.Skill", "Ns.Pushed", RelationKind.Raises, "PushVersion") });

        string json = JsonExporter.Export(model);

        using var doc = JsonDocument.Parse(json);
        var edge = doc.RootElement.GetProperty("edges")[0];
        Assert.True(edge.TryGetProperty("fromId", out _));
        Assert.False(edge.TryGetProperty("FromId", out _));
    }

    [Fact]
    public void Json_EmptyModel_SerializesToEmptyArrays_DoesNotThrow()
    {
        string json = JsonExporter.Export(DomainModel.Empty);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(0, doc.RootElement.GetProperty("nodes").GetArrayLength());
        Assert.Equal(0, doc.RootElement.GetProperty("edges").GetArrayLength());
    }
}
