using System.Text;

namespace Hindstorm;

/// <summary>
/// Renders a <see cref="DomainModel"/> as a Mermaid <c>flowchart</c>. Concepts are colored by kind in the
/// spirit of a storming wall, untagged (inferred) nodes are dashed so missing labels stand out, and edges
/// carry the relation name.
/// </summary>
public static class MermaidExporter
{
    /// <summary>Renders the model as Mermaid flowchart source.</summary>
    public static string Export(DomainModel model)
    {
        var ids = AssignIds(model);
        var builder = new StringBuilder();
        builder.AppendLine("flowchart LR");

        foreach (var node in model.Nodes)
        {
            var label = Escape(node.Name);
            builder.AppendLine($"    {ids[node.Id]}[\"{label}\"]:::{ClassFor(node)}");
        }

        if (model.Edges.Count > 0)
            builder.AppendLine();

        foreach (var edge in model.Edges)
            builder.AppendLine($"    {ids[edge.FromId]} -->|{EdgeLabel.For(edge)}| {ids[edge.ToId]}");

        builder.AppendLine();
        foreach (var line in ClassDefs)
            builder.AppendLine($"    {line}");

        return builder.ToString();
    }

    private static string ClassFor(DomainNode node) => node.Inferred ? "inferred" : node.Kind.ToString();

    private static Dictionary<string, string> AssignIds(DomainModel model)
    {
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        var index = 0;
        foreach (var node in model.Nodes)
            ids[node.Id] = $"n{index++}";
        return ids;
    }

    private static string Escape(string value) => value.Replace("\"", "&quot;");

    private static readonly string[] ClassDefs =
    [
        "classDef Aggregate fill:#FFE082,stroke:#F9A825,color:#000;",
        "classDef Command fill:#90CAF9,stroke:#1565C0,color:#000;",
        "classDef DomainEvent fill:#FFB74D,stroke:#E65100,color:#000;",
        "classDef Policy fill:#CE93D8,stroke:#6A1B9A,color:#000;",
        "classDef ReadModel fill:#A5D6A7,stroke:#2E7D32,color:#000;",
        "classDef ValueObject fill:#ECEFF1,stroke:#607D8B,color:#000;",
        "classDef ExternalSystem fill:#F48FB1,stroke:#AD1457,color:#000;",
        "classDef Actor fill:#FFF59D,stroke:#F9A825,color:#000;",
        "classDef inferred fill:#FFFFFF,stroke:#9E9E9E,stroke-dasharray:4 3,color:#616161;",
    ];
}

internal static class EdgeLabel
{
    public static string For(DomainEdge edge) => edge.Label ?? Friendly(edge.Relation);

    public static string Friendly(RelationKind relation) => relation switch
    {
        RelationKind.Raises => "raises",
        RelationKind.Handles => "handles",
        RelationKind.ReactsTo => "reacts to",
        RelationKind.Issues => "issues",
        RelationKind.Enforces => "enforces",
        RelationKind.Updates => "updates",
        _ => relation.ToString(),
    };
}
