using System.Reflection;
using Hindstorm.Tests.Sample;
using Xunit;

namespace Hindstorm.Tests;

public class DomainModelScannerTests
{
    private static DomainModel ScanSample(Action<ScannerOptions>? configure = null)
    {
        // Limit the scan to the sample namespace so test infrastructure types are ignored.
        var options = configure;
        return DomainModelScanner.Scan([Assembly.GetExecutingAssembly()], o =>
        {
            o.TypeFilter = t => t.Namespace?.StartsWith("Hindstorm.Tests.Sample", StringComparison.Ordinal) == true;
            configure?.Invoke(o);
        });
    }

    private static DomainNode Node<T>(DomainModel model)
        => model.Nodes.Single(n => n.Id == typeof(T).FullName);

    private static bool HasEdge(DomainModel model, Type from, Type to, RelationKind relation)
        => model.Edges.Any(e => e.FromId == from.FullName && e.ToId == to.FullName && e.Relation == relation);

    [Fact]
    public void Finds_tagged_concepts_with_their_kinds()
    {
        var model = ScanSample();

        Assert.Equal(ConceptKind.Aggregate, Node<Skill>(model).Kind);
        Assert.Equal(ConceptKind.Command, Node<PushSkillVersion>(model).Kind);
        Assert.Equal(ConceptKind.DomainEvent, Node<SkillVersionPushed>(model).Kind);
        Assert.Equal(ConceptKind.Policy, Node<RefinementPolicy>(model).Kind);
        Assert.Equal(ConceptKind.ReadModel, Node<SkillCatalogView>(model).Kind);
        Assert.Equal(ConceptKind.ValueObject, Node<Sha256Hash>(model).Kind);
        Assert.Equal(ConceptKind.Actor, Node<Creator>(model).Kind);
    }

    [Fact]
    public void Recovers_method_level_edges_in_flow_direction()
    {
        var model = ScanSample();

        // Command -> Aggregate (handles reverses direction onto the declaring type).
        Assert.True(HasEdge(model, typeof(PushSkillVersion), typeof(Skill), RelationKind.Handles));
        // Aggregate -> Event.
        Assert.True(HasEdge(model, typeof(Skill), typeof(SkillVersionPushed), RelationKind.Raises));
        // Aggregate -> Policy.
        Assert.True(HasEdge(model, typeof(Skill), typeof(SkillSizePolicy), RelationKind.Enforces));
    }

    [Fact]
    public void Recovers_reaction_chain_through_a_policy()
    {
        var model = ScanSample();

        Assert.True(HasEdge(model, typeof(SkillVersionPushed), typeof(RefinementPolicy), RelationKind.ReactsTo));
        Assert.True(HasEdge(model, typeof(RefinementPolicy), typeof(StartRefinement), RelationKind.Issues));
    }

    [Fact]
    public void Recovers_type_level_update_edge_to_read_model()
    {
        var model = ScanSample();

        Assert.True(HasEdge(model, typeof(SkillVersionPushed), typeof(SkillCatalogView), RelationKind.Updates));
    }

    [Fact]
    public void Records_the_declaring_member_on_method_edges()
    {
        var model = ScanSample();

        var raises = model.Edges.Single(e =>
            e.FromId == typeof(Skill).FullName &&
            e.ToId == typeof(SkillVersionPushed).FullName &&
            e.Relation == RelationKind.Raises);

        Assert.Equal(nameof(Skill.PushVersion), raises.Member);
    }

    [Fact]
    public void Infers_a_dashed_node_for_an_untagged_target()
    {
        var model = ScanSample();

        var archived = Node<SkillArchived>(model);
        Assert.True(archived.Inferred);
        Assert.Equal(ConceptKind.DomainEvent, archived.Kind);
    }

    [Fact]
    public void Suppresses_untagged_endpoints_when_inference_is_off()
    {
        var model = ScanSample(o => o.InferUntaggedEndpoints = false);

        Assert.DoesNotContain(model.Nodes, n => n.Id == typeof(SkillArchived).FullName);
        Assert.False(HasEdge(model, typeof(Skill), typeof(SkillArchived), RelationKind.Raises));
    }

    [Fact]
    public void Recovers_reaction_edges_from_the_handler_interface()
    {
        var model = ScanSample(o =>
        {
            o.HandlerInterface = typeof(IDomainEventHandler<>);
            o.DefaultHandlerKind = ConceptKind.ReadModel;
        });

        Assert.True(HasEdge(model, typeof(SkillVersionPushed), typeof(SkillCatalogProjection), RelationKind.ReactsTo));
        Assert.Equal(ConceptKind.ReadModel, Node<SkillCatalogProjection>(model).Kind);
    }

    [Fact]
    public void Is_deterministic_across_runs()
    {
        // Node and edge ordering is stable, so the serialized model is byte-for-byte identical.
        Assert.Equal(JsonExporter.Export(ScanSample()), JsonExporter.Export(ScanSample()));
    }
}
