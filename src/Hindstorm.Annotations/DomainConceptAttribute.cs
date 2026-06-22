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

    /// <summary>
    /// The dataflow pipeline this concept belongs to, named explicitly (for example <c>"AudioIngest"</c>),
    /// for streaming-plane concepts such as a <see cref="ConceptKind.Processor"/> or a
    /// <see cref="ConceptKind.DataEvent"/>. Exporters draw the pipeline as its own lane, visually distinct
    /// from a bounded context. Parallel to <see cref="Context"/> and, like it, a declared grouping never
    /// inferred from project structure on its own. Left null, a scanner can derive one from the namespace
    /// via <c>ScannerOptions.PipelineFromNamespace</c>; an explicit value set here wins over that.
    /// </summary>
    public string? Pipeline { get; set; }

    /// <summary>
    /// The level of this concept in a Complex Event Processing abstraction hierarchy, where 0 (the default)
    /// is the rawest stream and a higher number is more meaningful (frame 0 -&gt; segment 1 -&gt; utterance 2
    /// -&gt; intent 3). Meaningful mainly on a <see cref="ConceptKind.DataEvent"/> or a
    /// <see cref="ConceptKind.DomainEvent"/> lifted out of a stream; it lets an exporter order the dataflow
    /// plane by abstraction. Left at 0 it has no effect, so transactional concepts can ignore it.
    /// </summary>
    public int AbstractionLevel { get; set; }
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

/// <summary>Labels a type as a <see cref="ConceptKind.Processor"/> (a dataflow transform stage).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class ProcessorAttribute : DomainConceptAttribute
{
    /// <summary>Labels the type as a dataflow processor.</summary>
    public ProcessorAttribute() : base(ConceptKind.Processor) { }
}

/// <summary>Labels a type as a <see cref="ConceptKind.DataEvent"/> (a measurement or data point).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class DataEventAttribute : DomainConceptAttribute
{
    /// <summary>Labels the type as a data event.</summary>
    public DataEventAttribute() : base(ConceptKind.DataEvent) { }
}
