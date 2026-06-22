namespace Hindstorm;

/// <summary>
/// Base for the attributes that label a type as an event-storming concept. Scanners find every concept
/// in an assembly by looking for this one base, so all node kinds are discovered through a single query
/// rather than a different rule per kind. Use the derived attributes (<see cref="AggregateAttribute"/>,
/// <see cref="DomainEventAttribute"/>, and so on) at call sites; this type carries the shared metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public abstract class DomainConceptAttribute : Attribute
{
    /// <param name="kind">The storming concept this type represents.</param>
    protected DomainConceptAttribute(ConceptKind kind) => Kind = kind;

    /// <summary>The concept this type represents.</summary>
    public ConceptKind Kind { get; }

    /// <summary>
    /// A display name for the node. Defaults to the type name when null, so set this only to override the
    /// label that appears on the diagram (for example a human-readable command name).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>An optional one-line description shown alongside the node.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// The bounded context this concept belongs to, named explicitly (for example <c>"Payments"</c>).
    /// Exporters draw a labelled boundary around each context. Left null, a scanner can derive one from
    /// the namespace via <c>ScannerOptions.ContextFromNamespace</c>; an explicit value set here wins over
    /// that. The context is a declared strategic boundary, never inferred from project structure on its own.
    /// </summary>
    public string? Context { get; set; }
}

/// <summary>Labels a type as an <see cref="ConceptKind.Aggregate"/>.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class AggregateAttribute : DomainConceptAttribute
{
    /// <summary>Labels the type as an aggregate.</summary>
    public AggregateAttribute() : base(ConceptKind.Aggregate) { }
}

/// <summary>Labels a type as a <see cref="ConceptKind.Command"/>.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class CommandAttribute : DomainConceptAttribute
{
    /// <summary>Labels the type as a command.</summary>
    public CommandAttribute() : base(ConceptKind.Command) { }
}

/// <summary>Labels a type as a <see cref="ConceptKind.DomainEvent"/>.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class DomainEventAttribute : DomainConceptAttribute
{
    /// <summary>Labels the type as a domain event.</summary>
    public DomainEventAttribute() : base(ConceptKind.DomainEvent) { }
}

/// <summary>Labels a type as a <see cref="ConceptKind.Policy"/> (a reaction).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class PolicyAttribute : DomainConceptAttribute
{
    /// <summary>Labels the type as a policy.</summary>
    public PolicyAttribute() : base(ConceptKind.Policy) { }
}

/// <summary>Labels a type as an <see cref="ConceptKind.Invariant"/> (a command-time business rule).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class InvariantAttribute : DomainConceptAttribute
{
    /// <summary>Labels the type as an invariant.</summary>
    public InvariantAttribute() : base(ConceptKind.Invariant) { }
}

/// <summary>Labels a type as a <see cref="ConceptKind.ReadModel"/>.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class ReadModelAttribute : DomainConceptAttribute
{
    /// <summary>Labels the type as a read model.</summary>
    public ReadModelAttribute() : base(ConceptKind.ReadModel) { }
}

/// <summary>Labels a type as a <see cref="ConceptKind.ValueObject"/>.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class ValueObjectAttribute : DomainConceptAttribute
{
    /// <summary>Labels the type as a value object.</summary>
    public ValueObjectAttribute() : base(ConceptKind.ValueObject) { }
}

/// <summary>Labels a type as an <see cref="ConceptKind.ExternalSystem"/>.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class ExternalSystemAttribute : DomainConceptAttribute
{
    /// <summary>Labels the type as an external system.</summary>
    public ExternalSystemAttribute() : base(ConceptKind.ExternalSystem) { }
}

/// <summary>Labels a type as an <see cref="ConceptKind.Actor"/>.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class ActorAttribute : DomainConceptAttribute
{
    /// <summary>Labels the type as an actor.</summary>
    public ActorAttribute() : base(ConceptKind.Actor) { }
}
