using System.Text;

namespace Hindstorm;

/// <summary>
/// Renders a <see cref="DomainModel"/> as Graphviz DOT. Concepts are filled by kind, untagged (inferred)
/// nodes are dashed, and edges carry the relation name.
/// </summary>
/// <remarks>
/// An edge is labelled with its explicit <see cref="DomainEdge.Label"/> when set, otherwise the relation's
/// lower-case name (for example <see cref="RelationKind.ReactsTo"/> renders as <c>reacts to</c>).
/// Backslashes and double quotes in a node name are backslash-escaped. An edge whose endpoint has no
/// matching node is rendered against a dashed placeholder node labelled with that id, never thrown on.
/// </remarks>
public static class DotExporter
{
    /// <summary>Renders the model as DOT digraph source.</summary>
    public static string Export(DomainModel model)
    {
        // Lay the graph out from its entry points so the flow reads left to right.
        var layout = GraphLayout.Order(model);

        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        var phantoms = new List<string>();
        var index = 0;
        foreach (var node in layout.Nodes)
            if (!ids.ContainsKey(node.Id))
                ids[node.Id] = $"n{index++}";

        // An edge may reference an id with no declared node (for example a hand-built model). Give it an
        // id so the edge still renders, and emit a dashed placeholder for it rather than throwing.
        foreach (var edge in layout.Edges)
            foreach (var endpoint in new[] { edge.FromId, edge.ToId })
                if (!ids.ContainsKey(endpoint))
                {
                    ids[endpoint] = $"n{index++}";
                    phantoms.Add(endpoint);
                }

        var builder = new StringBuilder();
        builder.AppendLine("digraph DomainModel {");
        builder.AppendLine("    rankdir=LR;");
        // Honor the declared out-edge order so terminal targets (declared first) sit above the flow.
        builder.AppendLine("    ordering=out;");
        builder.AppendLine("    node [shape=box, style=\"filled,rounded\", fontname=\"sans-serif\"];");
        builder.AppendLine();

        // When contexts are declared, draw each as a labelled cluster (a boundary box); uncontexted nodes
        // sit at the top level. Cross-context edges then visibly cross a boundary.
        var grouping = GraphLayout.GroupByContext(layout.Nodes);
        if (grouping.AnyContext)
        {
            var clusterIndex = 0;
            foreach (var group in grouping.Groups)
            {
                builder.AppendLine($"    subgraph cluster_{clusterIndex++} {{");
                if (group.Plane == GraphLayout.Plane.Pipeline)
                {
                    // A pipeline lane reads as the streaming plane: a filled blue-grey band, not the grey
                    // dashed bounded-context box.
                    builder.AppendLine($"        label=\"pipeline: {Escape(group.Label)}\";");
                    builder.AppendLine("        style=\"rounded,filled\";");
                    builder.AppendLine("        color=\"#455A64\";");
                    builder.AppendLine("        fillcolor=\"#ECEFF1\";");
                }
                else
                {
                    builder.AppendLine($"        label=\"{Escape(group.Label)}\";");
                    builder.AppendLine("        style=\"rounded,dashed\";");
                    builder.AppendLine("        color=\"#9E9E9E\";");
                }

                builder.AppendLine("        fontname=\"sans-serif\";");
                foreach (var node in group.Nodes)
                    AppendNode(builder, ids, node, "        ");
                builder.AppendLine("    }");
            }

            foreach (var node in grouping.Ungrouped)
                AppendNode(builder, ids, node, "    ");
        }
        else
        {
            foreach (var node in layout.Nodes)
                AppendNode(builder, ids, node, "    ");
        }

        foreach (var id in phantoms)
            builder.AppendLine(
                $"    {ids[id]} [label=\"{Escape(id)}\", fillcolor=\"#FFFFFF\", color=\"#9E9E9E\", style=\"filled,rounded,dashed\"];");

        // Pin entry points to the leftmost rank, so the flow starts there. Skipped when clustering, since a
        // cross-cluster rank constraint fights Graphviz's cluster layout.
        if (layout.EntryIds.Count > 0 && !grouping.AnyContext)
        {
            var entries = string.Join(" ", layout.EntryIds.Select(id => ids[id] + ";"));
            builder.AppendLine();
            builder.AppendLine($"    {{ rank=source; {entries} }}");
        }

        builder.AppendLine();
        foreach (var edge in layout.Edges)
        {
            // The translation seam (a Translates edge) joins the dataflow plane to the domain plane, so
            // draw it as a highlighted boundary rather than a plain edge.
            var attrs = edge.Relation == RelationKind.Translates
                ? $"label=\"{Escape(EdgeLabel.For(edge))}\", color=\"#AD1457\", penwidth=2"
                : $"label=\"{Escape(EdgeLabel.For(edge))}\"";
            builder.AppendLine($"    {ids[edge.FromId]} -> {ids[edge.ToId]} [{attrs}];");
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static void AppendNode(StringBuilder builder, Dictionary<string, string> ids, DomainNode node, string indent)
    {
        var (fill, stroke) = Palette(node);
        var style = node.Inferred ? "\"filled,rounded,dashed\"" : "\"filled,rounded\"";
        builder.AppendLine(
            $"{indent}{ids[node.Id]} [label=\"{Escape(node.Name)}\", fillcolor=\"{fill}\", color=\"{stroke}\", style={style}];");
    }

    private static (string Fill, string Stroke) Palette(DomainNode node) => node.Inferred
        ? ("#FFFFFF", "#9E9E9E")
        : node.Kind switch
        {
            ConceptKind.Aggregate => ("#FFE082", "#F9A825"),
            ConceptKind.Command => ("#90CAF9", "#1565C0"),
            ConceptKind.DomainEvent => ("#FFB74D", "#E65100"),
            ConceptKind.Policy => ("#CE93D8", "#6A1B9A"),
            ConceptKind.Invariant => ("#80CBC4", "#00695C"),
            ConceptKind.ReadModel => ("#A5D6A7", "#2E7D32"),
            ConceptKind.ValueObject => ("#ECEFF1", "#607D8B"),
            ConceptKind.ExternalSystem => ("#F48FB1", "#AD1457"),
            ConceptKind.Actor => ("#BCAAA4", "#4E342E"),
            ConceptKind.Processor => ("#B0BEC5", "#37474F"),
            ConceptKind.DataEvent => ("#CFD8DC", "#546E7A"),
            _ => ("#ECEFF1", "#607D8B"),
        };

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
