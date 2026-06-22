namespace Hindstorm;

/// <summary>
/// Base for the attributes that declare a directed edge between two domain concepts. Reflection cannot
/// see the body of a method, so it cannot infer that an aggregate raises an event from the <c>new</c>
/// inside <c>PushVersion</c>. Instead the edge is stated as data: the target type is a constructor
/// argument baked into assembly metadata, which a scanner reads back with no IL parsing. Apply the
/// derived attributes (<see cref="RaisesAttribute"/> and friends) to a type or the specific member that
/// owns the relationship; scanners find them all through this one base.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public abstract class DomainRelationAttribute : Attribute
{
    /// <param name="kind">The kind of relationship.</param>
    /// <param name="direction">Which end of the edge the declaring type sits on.</param>
    /// <param name="target">The concept at the other end of the edge.</param>
    /// <exception cref="ArgumentNullException"><paramref name="target"/> is null.</exception>
    protected DomainRelationAttribute(RelationKind kind, RelationDirection direction, Type target)
    {
        Kind = kind;
        Direction = direction;
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    /// <summary>The kind of relationship.</summary>
    public RelationKind Kind { get; }

    /// <summary>Which end of the edge the declaring type sits on.</summary>
    public RelationDirection Direction { get; }

    /// <summary>The concept at the other end of the edge.</summary>
    public Type Target { get; }

    /// <summary>An optional label for the edge, shown on the diagram in place of the relation name.</summary>
    public string? Label { get; set; }
}

/// <summary>Declares that the annotated type or method raises a domain event.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class RaisesAttribute : DomainRelationAttribute
{
    /// <param name="eventType">The domain event raised.</param>
    public RaisesAttribute(Type eventType) : base(RelationKind.Raises, RelationDirection.FromDeclaring, eventType) { }
}

/// <summary>Declares that the annotated type or method handles a command.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class HandlesAttribute : DomainRelationAttribute
{
    /// <param name="commandType">The command handled.</param>
    public HandlesAttribute(Type commandType) : base(RelationKind.Handles, RelationDirection.ToDeclaring, commandType) { }
}

/// <summary>Declares that the annotated type or method reacts to a domain event.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ReactsToAttribute : DomainRelationAttribute
{
    /// <param name="eventType">The domain event reacted to.</param>
    public ReactsToAttribute(Type eventType) : base(RelationKind.ReactsTo, RelationDirection.ToDeclaring, eventType) { }
}

/// <summary>Declares that the annotated type or method issues a command.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class IssuesAttribute : DomainRelationAttribute
{
    /// <param name="commandType">The command issued.</param>
    public IssuesAttribute(Type commandType) : base(RelationKind.Issues, RelationDirection.FromDeclaring, commandType) { }
}

/// <summary>Declares that the annotated type or method enforces an invariant.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class EnforcesAttribute : DomainRelationAttribute
{
    /// <param name="invariantType">The invariant enforced.</param>
    public EnforcesAttribute(Type invariantType) : base(RelationKind.Enforces, RelationDirection.FromDeclaring, invariantType) { }
}

/// <summary>Declares that the annotated type or method updates a read model.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class UpdatesAttribute : DomainRelationAttribute
{
    /// <param name="readModelType">The read model updated.</param>
    public UpdatesAttribute(Type readModelType) : base(RelationKind.Updates, RelationDirection.FromDeclaring, readModelType) { }
}

/// <summary>Declares that the annotated processor or method transforms the stream into a data event.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class TransformsAttribute : DomainRelationAttribute
{
    /// <param name="dataEventType">The output representation the stream is transformed into.</param>
    public TransformsAttribute(Type dataEventType) : base(RelationKind.Transforms, RelationDirection.FromDeclaring, dataEventType) { }
}

/// <summary>Declares that the annotated processor or method feeds the next stage of the pipeline.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class FeedsAttribute : DomainRelationAttribute
{
    /// <param name="stageType">The downstream processor the stream is fed to.</param>
    public FeedsAttribute(Type stageType) : base(RelationKind.Feeds, RelationDirection.FromDeclaring, stageType) { }
}

/// <summary>Declares that the annotated processor or method translates the stream into a domain event at the seam.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class TranslatesAttribute : DomainRelationAttribute
{
    /// <param name="domainEventType">The domain event the stream is translated into at the anti-corruption seam.</param>
    public TranslatesAttribute(Type domainEventType) : base(RelationKind.Translates, RelationDirection.FromDeclaring, domainEventType) { }
}
