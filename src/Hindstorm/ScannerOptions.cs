using System.Reflection;

namespace Hindstorm;

/// <summary>
/// Tunes how <see cref="DomainModelScanner"/> reads a set of assemblies. The scanner stays unopinionated
/// about your building blocks: it knows only Hindstorm's attributes, and learns the rest of your domain's
/// shape through these options.
/// </summary>
public sealed class ScannerOptions
{
    /// <summary>
    /// An open generic event-handler interface, for example <c>typeof(IDomainEventHandler&lt;&gt;)</c>.
    /// When set, every closed implementation found becomes a "reacts to" edge from the event to the
    /// handler, so the reaction half of the flow is recovered for free without annotating each handler.
    /// </summary>
    public Type? HandlerInterface { get; set; }

    /// <summary>
    /// The kind given to a handler discovered through <see cref="HandlerInterface"/> that carries no
    /// concept attribute of its own. Defaults to <see cref="ConceptKind.Policy"/>.
    /// </summary>
    public ConceptKind DefaultHandlerKind { get; set; } = ConceptKind.Policy;

    /// <summary>
    /// When true (the default), a relation whose target type is not tagged still produces a node, with a
    /// kind inferred from the relation (a <c>Raises</c> target becomes a <see cref="ConceptKind.DomainEvent"/>,
    /// and so on). The node is marked <see cref="DomainNode.Inferred"/> so missing tags stay visible.
    /// When false, edges to untagged types are dropped.
    /// </summary>
    public bool InferUntaggedEndpoints { get; set; } = true;

    /// <summary>An optional filter to limit which types are scanned, for example by namespace.</summary>
    public Func<Type, bool>? TypeFilter { get; set; }
}
