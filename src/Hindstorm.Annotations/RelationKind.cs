namespace Hindstorm;

/// <summary>
/// The kind of directed relationship between two domain concepts. Together these form the grammar of an
/// event-storming flow: an actor or policy issues a command, an aggregate handles it and raises an event,
/// a policy reacts to that event and issues the next command, and read models are updated from events.
/// </summary>
public enum RelationKind
{
    /// <summary>The declaring type raises the target event. Edge: declaring -&gt; event.</summary>
    Raises,

    /// <summary>The declaring type handles the target command. Edge: command -&gt; declaring.</summary>
    Handles,

    /// <summary>The declaring type reacts to the target event. Edge: event -&gt; declaring.</summary>
    ReactsTo,

    /// <summary>The declaring type issues the target command. Edge: declaring -&gt; command.</summary>
    Issues,

    /// <summary>The declaring type enforces the target invariant. Edge: declaring -&gt; invariant.</summary>
    Enforces,

    /// <summary>The declaring type updates the target read model. Edge: declaring -&gt; read model.</summary>
    Updates,
}

/// <summary>Which end of a relation the annotated (declaring) type sits on.</summary>
public enum RelationDirection
{
    /// <summary>The declaring type is the source of the edge (declaring -&gt; target).</summary>
    FromDeclaring,

    /// <summary>The declaring type is the destination of the edge (target -&gt; declaring).</summary>
    ToDeclaring,
}
