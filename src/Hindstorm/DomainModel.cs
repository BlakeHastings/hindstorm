namespace Hindstorm;

/// <summary>
/// A node in the recovered model: one tagged domain concept, the equivalent of a single sticky on the
/// storming wall.
/// </summary>
/// <param name="Id">A stable identity for the node, the concept type's full name.</param>
/// <param name="Name">The display label, the type name unless overridden on the attribute.</param>
/// <param name="Kind">The storming concept this node represents.</param>
/// <param name="Namespace">The concept type's namespace, used to group nodes by bounded context.</param>
/// <param name="Description">An optional one-line description from the attribute.</param>
/// <param name="Inferred">
/// True when the node was synthesized from the other end of an edge rather than found by its own
/// concept attribute. An inferred node is a hint that a type is referenced in a relationship but was
/// never tagged.
/// </param>
/// <param name="Context">
/// The bounded context this concept belongs to, or null when none was declared. Comes from the concept
/// attribute's <c>Context</c> or, failing that, <c>ScannerOptions.ContextFromNamespace</c>. Used to draw
/// boundary boxes that group the wall by context.
/// </param>
public sealed record DomainNode(
    string Id,
    string Name,
    ConceptKind Kind,
    string? Namespace,
    string? Description,
    bool Inferred = false,
    string? Context = null);

/// <summary>
/// A directed edge in the recovered model: one relationship between two concepts, already oriented in
/// the direction the flow runs.
/// </summary>
/// <param name="FromId">The <see cref="DomainNode.Id"/> the edge runs from.</param>
/// <param name="ToId">The <see cref="DomainNode.Id"/> the edge runs to.</param>
/// <param name="Relation">The kind of relationship.</param>
/// <param name="Member">The method that declared the edge, when it was declared on a member.</param>
/// <param name="Label">An optional label from the attribute, shown in place of the relation name.</param>
public sealed record DomainEdge(
    string FromId,
    string ToId,
    RelationKind Relation,
    string? Member = null,
    string? Label = null);

/// <summary>
/// A domain model recovered from a set of assemblies: the nodes and edges of the storming wall, derived
/// from code rather than drawn in a workshop. Pass it to an exporter to render it.
/// </summary>
/// <param name="Nodes">Every concept found, ordered by namespace then name.</param>
/// <param name="Edges">Every relationship found, oriented in flow direction.</param>
public sealed record DomainModel(IReadOnlyList<DomainNode> Nodes, IReadOnlyList<DomainEdge> Edges)
{
    /// <summary>An empty model.</summary>
    public static DomainModel Empty { get; } = new([], []);
}
