using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Hindstorm.Tests;

public class ExporterTests
{
    private static DomainModel SampleModel() => DomainModelScanner.Scan(
        [Assembly.GetExecutingAssembly()],
        o => o.TypeFilter = t => t.Namespace?.StartsWith("Hindstorm.Tests.Sample", StringComparison.Ordinal) == true);

    [Fact]
    public void Mermaid_renders_a_flowchart_with_classes_and_edges()
    {
        var mermaid = MermaidExporter.Export(SampleModel());

        Assert.StartsWith("flowchart LR", mermaid);
        Assert.Contains("classDef Aggregate", mermaid);
        Assert.Contains("classDef inferred", mermaid);
        Assert.Contains("|raises|", mermaid);
        Assert.Contains("|reacts to|", mermaid);
    }

    [Fact]
    public void Dot_renders_a_digraph_with_edges()
    {
        var dot = DotExporter.Export(SampleModel());

        Assert.StartsWith("digraph DomainModel {", dot);
        Assert.Contains("rankdir=LR;", dot);
        Assert.Contains("-> ", dot);
        Assert.Contains("label=\"raises\"", dot);
    }

    [Fact]
    public void Json_round_trips_to_a_model_with_the_same_node_count()
    {
        var model = SampleModel();

        var json = JsonExporter.Export(model);
        using var document = JsonDocument.Parse(json);

        Assert.Equal(model.Nodes.Count, document.RootElement.GetProperty("nodes").GetArrayLength());
        Assert.Equal(model.Edges.Count, document.RootElement.GetProperty("edges").GetArrayLength());
        // Enums serialize as names, not numbers.
        Assert.Contains("\"Aggregate\"", json);
    }
}
