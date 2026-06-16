namespace Hindstorm;

/// <summary>
/// The kind of event-storming sticky a domain type represents. These map onto the colored notes used
/// on a storming wall, so a model recovered from code reads the same way as one built in a workshop.
/// </summary>
public enum ConceptKind
{
    /// <summary>A consistency boundary that handles commands and raises events (storming: yellow).</summary>
    Aggregate,

    /// <summary>An intent to change the system, usually issued by an actor or a policy (storming: blue).</summary>
    Command,

    /// <summary>Something that happened in the domain, in the past tense (storming: orange).</summary>
    DomainEvent,

    /// <summary>A reaction: "whenever this event, then issue that command" (storming: lilac).</summary>
    Policy,

    /// <summary>A projection or view built for a query, fed by events (storming: green).</summary>
    ReadModel,

    /// <summary>An immutable, equality-by-value concept with no lifecycle of its own.</summary>
    ValueObject,

    /// <summary>A system outside this boundary that sends or receives events (storming: pink).</summary>
    ExternalSystem,

    /// <summary>A person or role that issues commands (storming: small yellow).</summary>
    Actor,

    /// <summary>
    /// A constraint an aggregate enforces while handling a command, before any event is raised (a DDD
    /// invariant; in Event Storming this is a "business rule"). Distinct from a <see cref="Policy"/>:
    /// an invariant governs whether a command is allowed to succeed, it is not triggered by an event and
    /// does not issue a command.
    /// </summary>
    Invariant,
}
