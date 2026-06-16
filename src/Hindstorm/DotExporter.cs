using System.Text;

namespace Hindstorm;

/// <summary>
/// Renders a <see cref="DomainModel"/> as Graphviz DOT. Concepts are filled by kind, untagged (inferred)
/// nodes are dashed, and edges carry the relation name.
/// </summary>
public static class DotExporter
{
    /// <summary>Renders the model as DOT digraph source.</summary>
    public static string Export(DomainModel model)
    {
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        var index = 0;
        foreach (var node in model.Nodes)
            ids[node.Id] = $"n{index++}";

        var builder = new StringBuilder();
        builder.AppendLine("digraph DomainModel {");
        builder.AppendLine("    rankdir=LR;");
        builder.AppendLine("    node [shape=box, style=\"filled,rounded\", fontname=\"sans-serif\"];");
        builder.AppendLine();

        foreach (var node in model.Nodes)
        {
            var (fill, stroke) = Palette(node);
            var style = node.Inferred ? "\"filled,rounded,dashed\"" : "\"filled,rounded\"";
            builder.AppendLine(
                $"    {ids[node.Id]} [label=\"{Escape(node.Name)}\", fillcolor=\"{fill}\", color=\"{stroke}\", style={style}];");
        }

        builder.AppendLine();
        foreach (var edge in model.Edges)
            builder.AppendLine($"    {ids[edge.FromId]} -> {ids[edge.ToId]} [label=\"{Escape(EdgeLabel.For(edge))}\"];");

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static (string Fill, string Stroke) Palette(DomainNode node) => node.Inferred
        ? ("#FFFFFF", "#9E9E9E")
        : node.Kind switch
        {
            ConceptKind.Aggregate => ("#FFE082", "#F9A825"),
            ConceptKind.Command => ("#90CAF9", "#1565C0"),
            ConceptKind.DomainEvent => ("#FFB74D", "#E65100"),
            ConceptKind.Policy => ("#CE93D8", "#6A1B9A"),
            ConceptKind.ReadModel => ("#A5D6A7", "#2E7D32"),
            ConceptKind.ValueObject => ("#ECEFF1", "#607D8B"),
            ConceptKind.ExternalSystem => ("#F48FB1", "#AD1457"),
            ConceptKind.Actor => ("#FFF59D", "#F9A825"),
            _ => ("#ECEFF1", "#607D8B"),
        };

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
