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

    /// <summary>
    /// A dataflow transform stage on the streaming plane (a filter, a processor, a Complex Event Processing
    /// agent). It transforms one representation of a stream into another and holds no invariant, makes no
    /// decision, and cannot reject. Distinct from an <see cref="Aggregate"/>: "do the next step" is a
    /// pipeline edge, not a command, so a processor is not a consistency boundary. A stateful processor is
    /// a domain service, never an aggregate.
    /// </summary>
    Processor,

    /// <summary>
    /// A measurement or low-abstraction data point on the streaming plane (a frame, a probability, a score),
    /// as opposed to a meaningful <see cref="DomainEvent"/>. A data event is the raw material a
    /// <see cref="Processor"/> consumes and produces; only a domain-significant fact lifted out of the
    /// stream crosses the translation seam to become a domain event. Use <see cref="DomainConceptAttribute.AbstractionLevel"/>
    /// to place it in a Complex Event Processing hierarchy (frame -&gt; segment -&gt; utterance -&gt; intent).
    /// </summary>
    DataEvent,
}
