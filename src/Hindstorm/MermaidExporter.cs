using System.Text;

namespace Hindstorm;

/// <summary>
/// Renders a <see cref="DomainModel"/> as a Mermaid <c>flowchart</c>. Concepts are colored by kind in the
/// spirit of a storming wall, untagged (inferred) nodes are dashed so missing labels stand out, and edges
/// carry the relation name.
/// </summary>
/// <remarks>
/// An edge is labelled with its explicit <see cref="DomainEdge.Label"/> when set, otherwise the relation's
/// lower-case name (for example <see cref="RelationKind.ReactsTo"/> renders as <c>reacts to</c>). A double
/// quote in a node name is escaped to <c>&amp;quot;</c>. An edge whose endpoint has no matching node is
/// rendered against a dashed placeholder node labelled with that id, never thrown on.
/// </remarks>
public static class MermaidExporter
{
    /// <summary>Renders the model as Mermaid flowchart source, laid out with the ELK engine.</summary>
    public static string Export(DomainModel model) => Export(model, MermaidLayout.Elk);

    /// <summary>Renders the model as Mermaid flowchart source using the given layout engine.</summary>
    public static string Export(DomainModel model, MermaidLayout layout)
    {
        // Order nodes and edges from the graph's entry points so the flow reads left to right.
        var ordered = GraphLayout.Order(model);
        var (ids, phantoms) = AssignIds(ordered);
        var builder = new StringBuilder();

        // ELK lays cyclic, layered flows out far more cleanly than Mermaid's default (dagre) renderer; it
        // is the default. The directive must come first. Pass MermaidLayout.Dagre to opt out.
        if (layout == MermaidLayout.Elk)
            builder.AppendLine("%%{init: {\"layout\": \"elk\"}}%%");
        builder.AppendLine("flowchart LR");

        // When contexts are declared, wrap each in a labelled subgraph so the wall reads as bounded
        // contexts; uncontexted nodes sit at the top level.
        var grouping = GraphLayout.GroupByContext(ordered.Nodes);
        if (grouping.AnyContext)
        {
            var contextIndex = 0;
            foreach (var (context, contextNodes) in grouping.Groups)
            {
                builder.AppendLine($"    subgraph ctx{contextIndex++}[\"{Escape(context)}\"]");
                foreach (var node in contextNodes)
                    builder.AppendLine($"        {NodeLine(ids, node)}");
                builder.AppendLine("    end");
            }

            foreach (var node in grouping.Ungrouped)
                builder.AppendLine($"    {NodeLine(ids, node)}");
        }
        else
        {
            foreach (var node in ordered.Nodes)
                builder.AppendLine($"    {NodeLine(ids, node)}");
        }

        // An edge may reference an id with no declared node (for example a hand-built model). Render it
        // as a dashed placeholder rather than throwing, so the edge survives and the gap is visible.
        foreach (var id in phantoms)
            builder.AppendLine($"    {ids[id]}[\"{Escape(id)}\"]:::inferred");

        if (ordered.Edges.Count > 0)
            builder.AppendLine();

        foreach (var edge in ordered.Edges)
            builder.AppendLine($"    {ids[edge.FromId]} -->|{EdgeLabel.For(edge)}| {ids[edge.ToId]}");

        builder.AppendLine();
        foreach (var line in ClassDefs)
            builder.AppendLine($"    {line}");

        return builder.ToString();
    }

    private static string NodeLine(Dictionary<string, string> ids, DomainNode node)
        => $"{ids[node.Id]}[\"{Escape(node.Name)}\"]:::{ClassFor(node)}";

    private static string ClassFor(DomainNode node) => node.Inferred ? "inferred" : node.Kind.ToString();

    private static (Dictionary<string, string> Ids, List<string> Phantoms) AssignIds(GraphLayout.Ordered layout)
    {
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        var phantoms = new List<string>();
        var index = 0;
        foreach (var node in layout.Nodes)
            if (!ids.ContainsKey(node.Id))
                ids[node.Id] = $"n{index++}";

        foreach (var edge in layout.Edges)
            foreach (var endpoint in new[] { edge.FromId, edge.ToId })
                if (!ids.ContainsKey(endpoint))
                {
                    ids[endpoint] = $"n{index++}";
                    phantoms.Add(endpoint);
                }

        return (ids, phantoms);
    }

    private static string Escape(string value) => value.Replace("\"", "&quot;");

    private static readonly string[] ClassDefs =
    [
        "classDef Aggregate fill:#FFE082,stroke:#F9A825,color:#000;",
        "classDef Command fill:#90CAF9,stroke:#1565C0,color:#000;",
        "classDef DomainEvent fill:#FFB74D,stroke:#E65100,color:#000;",
        "classDef Policy fill:#CE93D8,stroke:#6A1B9A,color:#000;",
        "classDef Invariant fill:#80CBC4,stroke:#00695C,color:#000;",
        "classDef ReadModel fill:#A5D6A7,stroke:#2E7D32,color:#000;",
        "classDef ValueObject fill:#ECEFF1,stroke:#607D8B,color:#000;",
        "classDef ExternalSystem fill:#F48FB1,stroke:#AD1457,color:#000;",
        "classDef Actor fill:#BCAAA4,stroke:#4E342E,color:#000;",
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
