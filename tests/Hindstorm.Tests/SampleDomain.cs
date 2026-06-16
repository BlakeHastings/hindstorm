namespace Hindstorm.Tests.Sample;

// A small slice of a skill registry, annotated as an event-storming flow:
//   Creator -> PushSkillVersion -> Skill -> SkillVersionPushed -> RefinementPolicy -> StartRefinement
//                                            SkillVersionPushed -> SkillCatalogView (read model)
//                                            SkillVersionPushed -> SkillCatalogProjection (handler interface)

/// <summary>A generic event-handler interface, standing in for a consumer's own building block.</summary>
public interface IDomainEventHandler<in TEvent>
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}

[Actor]
public sealed class Creator;

[Command]
public sealed record PushSkillVersion(Guid SkillId, Guid CreatorId);

[Command]
public sealed record StartRefinement(Guid SkillId, Guid VersionId);

[DomainEvent]
[Updates(typeof(SkillCatalogView))]
public sealed record SkillVersionPushed(Guid SkillId, Guid VersionId);

// Intentionally left untagged so the scanner infers a dashed DomainEvent node.
public sealed record SkillArchived(Guid SkillId);

[Policy]
public static class SkillSizePolicy
{
    public static bool Exceeded(long bytes) => bytes > 20 * 1024 * 1024;
}

[Policy]
public sealed class RefinementPolicy
{
    [ReactsTo(typeof(SkillVersionPushed))]
    [Issues(typeof(StartRefinement))]
    public StartRefinement OnPushed(SkillVersionPushed pushed) => new(pushed.SkillId, pushed.VersionId);
}

[ReadModel]
public sealed class SkillCatalogView
{
    public Guid SkillId { get; init; }
}

[ValueObject]
public sealed record Sha256Hash(string Value);

[Aggregate]
public sealed class Skill
{
    [Handles(typeof(PushSkillVersion))]
    [Raises(typeof(SkillVersionPushed))]
    [Raises(typeof(SkillArchived))]
    [Enforces(typeof(SkillSizePolicy))]
    public SkillVersionPushed PushVersion(PushSkillVersion command)
        => new(command.SkillId, Guid.NewGuid());
}

// Untagged handler, discovered through the handler interface rather than a concept attribute.
public sealed class SkillCatalogProjection : IDomainEventHandler<SkillVersionPushed>
{
    public Task HandleAsync(SkillVersionPushed domainEvent, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
