using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hindstorm;
using Xunit;

// Contract-derived tests for DomainModelScanner. Written from the CONTRACT PACKET only;
// implementation and pre-existing tests were intentionally NOT read.
//
// Fixtures live in namespace Hindstorm.Tests.ScannerContract.Fixtures and every scan is
// restricted to that namespace via TypeFilter so unrelated test types do not leak in.
//
// ASSUMES (low-impact, strongly implied by the contract):
//  - The fixture assembly under test is the assembly that declares the fixture types
//    (typeof(SomeFixture).Assembly), per the SUT construction recipe.
//  - DomainNode.Id == Type.FullName exactly (contract: "Id = the concept type's full name").
//  - DomainNode.Namespace == Type.Namespace (contract: "Namespace" with no transform stated).
//  - DomainNode.Name defaults to the simple type name when no Name override is given
//    (contract: "Name = display label (type name unless overridden on attr)").

namespace Hindstorm.Tests
{
    public class ScannerContractTests
    {
        // The namespace the fixtures live in; the scan is restricted to it.
        private const string FixtureNamespace = "Hindstorm.Tests.ScannerContract.Fixtures";

        private static readonly Assembly FixtureAssembly =
            typeof(ScannerContract.Fixtures.SampleAggregate).Assembly;

        // Scan helper that always restricts to the fixture namespace, then lets the caller
        // layer on additional options. We re-apply the TypeFilter inside the configure callback
        // so callers who set other options still keep the namespace restriction.
        private static DomainModel ScanFixtures(Action<ScannerOptions>? configure = null)
        {
            return DomainModelScanner.Scan(
                new[] { FixtureAssembly },
                o =>
                {
                    o.TypeFilter = t => t.Namespace == FixtureNamespace;
                    configure?.Invoke(o);
                    // Guard: if the caller replaced TypeFilter, wrap to keep the namespace fence.
                    var inner = o.TypeFilter;
                    if (inner != null)
                    {
                        o.TypeFilter = t => t.Namespace == FixtureNamespace && inner(t);
                    }
                    else
                    {
                        o.TypeFilter = t => t.Namespace == FixtureNamespace;
                    }
                });
        }

        // Convenience: scan but restrict to a single fixture type (and optional extra types),
        // so a test only sees the nodes/edges it cares about.
        private static DomainModel ScanOnly(params Type[] types)
        {
            var set = new HashSet<Type>(types);
            return DomainModelScanner.Scan(
                new[] { FixtureAssembly },
                o => o.TypeFilter = t => t.Namespace == FixtureNamespace && set.Contains(t));
        }

        private static DomainModel ScanOnly(Action<ScannerOptions> extra, params Type[] types)
        {
            var set = new HashSet<Type>(types);
            return DomainModelScanner.Scan(
                new[] { FixtureAssembly },
                o =>
                {
                    o.TypeFilter = t => t.Namespace == FixtureNamespace && set.Contains(t);
                    extra(o);
                    var inner = o.TypeFilter;
                    o.TypeFilter = t => t.Namespace == FixtureNamespace && set.Contains(t) && (inner == null || inner(t));
                });
        }

        private static DomainNode NodeFor(DomainModel model, Type t)
            => model.Nodes.Single(n => n.Id == t.FullName);

        // ---------------------------------------------------------------------
        // POSITIVE: every concept attribute produces a node with correct kind/id/namespace
        // ---------------------------------------------------------------------

        public static IEnumerable<object[]> ConceptCases()
        {
            yield return new object[] { typeof(ScannerContract.Fixtures.SampleAggregate), ConceptKind.Aggregate };
            yield return new object[] { typeof(ScannerContract.Fixtures.SampleCommand), ConceptKind.Command };
            yield return new object[] { typeof(ScannerContract.Fixtures.SampleEvent), ConceptKind.DomainEvent };
            yield return new object[] { typeof(ScannerContract.Fixtures.SamplePolicy), ConceptKind.Policy };
            yield return new object[] { typeof(ScannerContract.Fixtures.SampleInvariant), ConceptKind.Invariant };
            yield return new object[] { typeof(ScannerContract.Fixtures.SampleReadModel), ConceptKind.ReadModel };
            yield return new object[] { typeof(ScannerContract.Fixtures.SampleValueObject), ConceptKind.ValueObject };
            yield return new object[] { typeof(ScannerContract.Fixtures.SampleExternalSystem), ConceptKind.ExternalSystem };
            yield return new object[] { typeof(ScannerContract.Fixtures.SampleActor), ConceptKind.Actor };
        }

        [Theory]
        [MemberData(nameof(ConceptCases))]
        public void Concept_attribute_produces_node_with_kind_id_and_namespace(Type conceptType, ConceptKind expectedKind)
        {
            var model = ScanOnly(conceptType);

            var node = Assert.Single(model.Nodes);
            Assert.Equal(expectedKind, node.Kind);
            Assert.Equal(conceptType.FullName, node.Id);
            Assert.Equal(conceptType.Namespace, node.Namespace);
            // Discovered by its own concept attribute, so not inferred.
            Assert.False(node.Inferred);
        }

        [Fact]
        public void Concept_name_defaults_to_simple_type_name()
        {
            var model = ScanOnly(typeof(ScannerContract.Fixtures.SampleAggregate));
            var node = Assert.Single(model.Nodes);
            // ASSUMES: default Name is the simple type name (not FullName).
            Assert.Equal(nameof(ScannerContract.Fixtures.SampleAggregate), node.Name);
        }

        // ---------------------------------------------------------------------
        // POSITIVE: relation attributes on a METHOD produce a correctly-oriented edge,
        // one test per RelationKind. Handles/ReactsTo reverse onto the declaring type.
        // ---------------------------------------------------------------------

        [Fact]
        public void Raises_on_method_orients_declaring_to_event()
        {
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.RaiserAggregate),
                typeof(ScannerContract.Fixtures.SampleEvent));

            var edge = Assert.Single(model.Edges, e => e.Relation == RelationKind.Raises);
            Assert.Equal(typeof(ScannerContract.Fixtures.RaiserAggregate).FullName, edge.FromId);
            Assert.Equal(typeof(ScannerContract.Fixtures.SampleEvent).FullName, edge.ToId);
        }

        [Fact]
        public void Handles_on_method_orients_command_to_declaring()
        {
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.HandlerAggregate),
                typeof(ScannerContract.Fixtures.SampleCommand));

            var edge = Assert.Single(model.Edges, e => e.Relation == RelationKind.Handles);
            // ToDeclaring => command -> declaring
            Assert.Equal(typeof(ScannerContract.Fixtures.SampleCommand).FullName, edge.FromId);
            Assert.Equal(typeof(ScannerContract.Fixtures.HandlerAggregate).FullName, edge.ToId);
        }

        [Fact]
        public void ReactsTo_on_method_orients_event_to_declaring()
        {
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.ReactorPolicy),
                typeof(ScannerContract.Fixtures.SampleEvent));

            var edge = Assert.Single(model.Edges, e => e.Relation == RelationKind.ReactsTo);
            // ToDeclaring => event -> declaring
            Assert.Equal(typeof(ScannerContract.Fixtures.SampleEvent).FullName, edge.FromId);
            Assert.Equal(typeof(ScannerContract.Fixtures.ReactorPolicy).FullName, edge.ToId);
        }

        [Fact]
        public void Issues_on_method_orients_declaring_to_command()
        {
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.IssuerPolicy),
                typeof(ScannerContract.Fixtures.SampleCommand));

            var edge = Assert.Single(model.Edges, e => e.Relation == RelationKind.Issues);
            Assert.Equal(typeof(ScannerContract.Fixtures.IssuerPolicy).FullName, edge.FromId);
            Assert.Equal(typeof(ScannerContract.Fixtures.SampleCommand).FullName, edge.ToId);
        }

        [Fact]
        public void Enforces_on_method_orients_declaring_to_invariant()
        {
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.EnforcerAggregate),
                typeof(ScannerContract.Fixtures.SampleInvariant));

            var edge = Assert.Single(model.Edges, e => e.Relation == RelationKind.Enforces);
            Assert.Equal(typeof(ScannerContract.Fixtures.EnforcerAggregate).FullName, edge.FromId);
            Assert.Equal(typeof(ScannerContract.Fixtures.SampleInvariant).FullName, edge.ToId);
        }

        [Fact]
        public void Enforces_target_that_is_untagged_is_inferred_as_an_invariant()
        {
            // EnforcerOfUntagged enforces UntaggedInvariant, which carries no concept attribute.
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.EnforcerOfUntagged),
                typeof(ScannerContract.Fixtures.UntaggedInvariant));

            var node = model.Nodes.Single(n => n.Id == typeof(ScannerContract.Fixtures.UntaggedInvariant).FullName);
            Assert.True(node.Inferred);
            Assert.Equal(ConceptKind.Invariant, node.Kind);
        }

        [Fact]
        public void Updates_on_method_orients_declaring_to_read_model()
        {
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.UpdaterPolicy),
                typeof(ScannerContract.Fixtures.SampleReadModel));

            var edge = Assert.Single(model.Edges, e => e.Relation == RelationKind.Updates);
            Assert.Equal(typeof(ScannerContract.Fixtures.UpdaterPolicy).FullName, edge.FromId);
            Assert.Equal(typeof(ScannerContract.Fixtures.SampleReadModel).FullName, edge.ToId);
        }

        // ---------------------------------------------------------------------
        // POSITIVE: relation on the TYPE (not a method) produces an edge.
        // ---------------------------------------------------------------------

        [Fact]
        public void Relation_on_type_produces_edge()
        {
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.TypeLevelRaiser),
                typeof(ScannerContract.Fixtures.SampleEvent));

            var edge = Assert.Single(model.Edges, e => e.Relation == RelationKind.Raises);
            Assert.Equal(typeof(ScannerContract.Fixtures.TypeLevelRaiser).FullName, edge.FromId);
            Assert.Equal(typeof(ScannerContract.Fixtures.SampleEvent).FullName, edge.ToId);
        }

        // ---------------------------------------------------------------------
        // POSITIVE: edge declared on a method records the method name in Member;
        // edge declared on the type has no Member.
        // ---------------------------------------------------------------------

        [Fact]
        public void Edge_on_method_records_member_name()
        {
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.RaiserAggregate),
                typeof(ScannerContract.Fixtures.SampleEvent));

            var edge = Assert.Single(model.Edges, e => e.Relation == RelationKind.Raises);
            Assert.Equal(nameof(ScannerContract.Fixtures.RaiserAggregate.DoTheThing), edge.Member);
        }

        [Fact]
        public void Edge_on_type_has_no_member()
        {
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.TypeLevelRaiser),
                typeof(ScannerContract.Fixtures.SampleEvent));

            var edge = Assert.Single(model.Edges, e => e.Relation == RelationKind.Raises);
            Assert.Null(edge.Member);
        }

        // ---------------------------------------------------------------------
        // POSITIVE: Name override, Description carry-through, Label carry-through.
        // ---------------------------------------------------------------------

        [Fact]
        public void Concept_name_override_replaces_display_name()
        {
            var model = ScanOnly(typeof(ScannerContract.Fixtures.NamedAggregate));
            var node = Assert.Single(model.Nodes);
            Assert.Equal("Custom Display Name", node.Name);
            // Id still keyed off the type's FullName, not the override.
            Assert.Equal(typeof(ScannerContract.Fixtures.NamedAggregate).FullName, node.Id);
        }

        [Fact]
        public void Concept_description_is_carried_through()
        {
            var model = ScanOnly(typeof(ScannerContract.Fixtures.DescribedAggregate));
            var node = Assert.Single(model.Nodes);
            Assert.Equal("A described aggregate.", node.Description);
        }

        [Fact]
        public void Relation_label_is_carried_onto_edge()
        {
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.LabelledRaiser),
                typeof(ScannerContract.Fixtures.SampleEvent));

            var edge = Assert.Single(model.Edges, e => e.Relation == RelationKind.Raises);
            Assert.Equal("on save", edge.Label);
        }

        // ---------------------------------------------------------------------
        // POSITIVE: HandlerInterface (open generic) => ReactsTo edge event -> handler.
        // Untagged handler gets DefaultHandlerKind (default Policy, and overridable).
        // ---------------------------------------------------------------------

        [Fact]
        public void HandlerInterface_open_generic_produces_reactsto_edge_from_event_to_handler()
        {
            var model = ScanOnly(
                o => o.HandlerInterface = typeof(ScannerContract.Fixtures.IEventHandler<>),
                typeof(ScannerContract.Fixtures.UntaggedHandler),
                typeof(ScannerContract.Fixtures.SampleEvent));

            var edge = Assert.Single(model.Edges, e => e.Relation == RelationKind.ReactsTo);
            // event (generic arg) -> handler
            Assert.Equal(typeof(ScannerContract.Fixtures.SampleEvent).FullName, edge.FromId);
            Assert.Equal(typeof(ScannerContract.Fixtures.UntaggedHandler).FullName, edge.ToId);
        }

        [Fact]
        public void Untagged_handler_gets_default_handler_kind_Policy()
        {
            var model = ScanOnly(
                o => o.HandlerInterface = typeof(ScannerContract.Fixtures.IEventHandler<>),
                typeof(ScannerContract.Fixtures.UntaggedHandler),
                typeof(ScannerContract.Fixtures.SampleEvent));

            var handlerNode = NodeFor(model, typeof(ScannerContract.Fixtures.UntaggedHandler));
            Assert.Equal(ConceptKind.Policy, handlerNode.Kind);
        }

        [Fact]
        public void Untagged_handler_kind_is_overridable_via_DefaultHandlerKind()
        {
            var model = ScanOnly(
                o =>
                {
                    o.HandlerInterface = typeof(ScannerContract.Fixtures.IEventHandler<>);
                    o.DefaultHandlerKind = ConceptKind.ReadModel;
                },
                typeof(ScannerContract.Fixtures.UntaggedHandler),
                typeof(ScannerContract.Fixtures.SampleEvent));

            var handlerNode = NodeFor(model, typeof(ScannerContract.Fixtures.UntaggedHandler));
            Assert.Equal(ConceptKind.ReadModel, handlerNode.Kind);
        }

        // ---------------------------------------------------------------------
        // POSITIVE: untagged relation target with InferUntaggedEndpoints=true (default)
        // => an Inferred node with the kind inferred from the relation.
        // ---------------------------------------------------------------------

        [Fact]
        public void Untagged_raises_target_is_inferred_as_domain_event_by_default()
        {
            // RaiserToUntaggedEvent raises UntaggedEventTarget, which has no concept attribute.
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.RaiserToUntaggedEvent),
                typeof(ScannerContract.Fixtures.UntaggedEventTarget));

            var targetNode = NodeFor(model, typeof(ScannerContract.Fixtures.UntaggedEventTarget));
            Assert.True(targetNode.Inferred);
            // A Raises target becomes a DomainEvent (per contract example).
            Assert.Equal(ConceptKind.DomainEvent, targetNode.Kind);

            // And the edge is present.
            Assert.Single(model.Edges, e =>
                e.Relation == RelationKind.Raises &&
                e.FromId == typeof(ScannerContract.Fixtures.RaiserToUntaggedEvent).FullName &&
                e.ToId == typeof(ScannerContract.Fixtures.UntaggedEventTarget).FullName);
        }

        // ---------------------------------------------------------------------
        // POSITIVE: empty / no-match scans.
        // ---------------------------------------------------------------------

        [Fact]
        public void TypeFilter_excluding_everything_yields_empty_model()
        {
            var model = DomainModelScanner.Scan(
                new[] { FixtureAssembly },
                o => o.TypeFilter = _ => false);

            Assert.Empty(model.Nodes);
            Assert.Empty(model.Edges);
        }

        [Fact]
        public void Scanning_only_untagged_types_yields_empty_model()
        {
            // Restrict to a single untagged type with no relations.
            var model = ScanOnly(typeof(ScannerContract.Fixtures.PlainUntagged));
            Assert.Empty(model.Nodes);
            Assert.Empty(model.Edges);
        }

        // ---------------------------------------------------------------------
        // POSITIVE: determinism. Two scans produce identical node and edge ordering.
        // ---------------------------------------------------------------------

        [Fact]
        public void Two_scans_produce_identical_node_and_edge_ordering()
        {
            var a = ScanFixtures();
            var b = ScanFixtures();

            Assert.Equal(a.Nodes.Select(n => n.Id), b.Nodes.Select(n => n.Id));
            Assert.Equal(
                a.Edges.Select(e => (e.FromId, e.ToId, e.Relation, e.Member)),
                b.Edges.Select(e => (e.FromId, e.ToId, e.Relation, e.Member)));
        }

        [Fact]
        public void Nodes_are_ordered_by_namespace_then_name()
        {
            var model = ScanFixtures();
            // All fixtures share a namespace, so ordering reduces to Name.
            // We verify monotonicity by (Namespace, Name) rather than an exact tiebreak so the test
            // does not over-constrain how the scanner breaks ties between equal names.
            // ASSUMES: "ordered by namespace then name" is ordinal-ish ascending. If the scanner
            // uses a culture-sensitive comparison the relative order of our ASCII fixture names is
            // unaffected, so this monotonicity check is robust to that.
            var actual = model.Nodes.ToList();
            for (int i = 1; i < actual.Count; i++)
            {
                var prev = actual[i - 1];
                var cur = actual[i];
                var nsCmp = string.CompareOrdinal(prev.Namespace, cur.Namespace);
                Assert.True(nsCmp <= 0, "Nodes must be ordered by namespace ascending.");
                if (nsCmp == 0)
                {
                    Assert.True(
                        string.CompareOrdinal(prev.Name, cur.Name) <= 0,
                        "Within a namespace, nodes must be ordered by name ascending.");
                }
            }
        }

        // ---------------------------------------------------------------------
        // POSITIVE: identical duplicated relation collapses to ONE edge.
        // ---------------------------------------------------------------------

        [Fact]
        public void Identical_duplicated_relation_collapses_to_single_edge()
        {
            // DoubleRaiser declares [Raises(typeof(SampleEvent))] twice on the same member.
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.DoubleRaiser),
                typeof(ScannerContract.Fixtures.SampleEvent));

            var raisesEdges = model.Edges
                .Where(e => e.Relation == RelationKind.Raises &&
                            e.FromId == typeof(ScannerContract.Fixtures.DoubleRaiser).FullName &&
                            e.ToId == typeof(ScannerContract.Fixtures.SampleEvent).FullName)
                .ToList();

            Assert.Single(raisesEdges);
        }

        // ---------------------------------------------------------------------
        // NEGATIVE: relation attribute on an UNTAGGED type contributes no node and no edge.
        // ---------------------------------------------------------------------

        [Fact]
        public void Relation_on_untagged_type_contributes_no_node_and_no_edge()
        {
            // UntaggedRelationHolder has a [Raises] but no concept attribute.
            // SampleEvent is tagged. The relation on the untagged holder must contribute nothing.
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.UntaggedRelationHolder),
                typeof(ScannerContract.Fixtures.SampleEvent));

            // Only the tagged SampleEvent node should exist.
            var node = Assert.Single(model.Nodes);
            Assert.Equal(typeof(ScannerContract.Fixtures.SampleEvent).FullName, node.Id);

            // No edge from the untagged holder.
            Assert.DoesNotContain(model.Edges, e =>
                e.FromId == typeof(ScannerContract.Fixtures.UntaggedRelationHolder).FullName ||
                e.ToId == typeof(ScannerContract.Fixtures.UntaggedRelationHolder).FullName);
            Assert.Empty(model.Edges);
        }

        // ---------------------------------------------------------------------
        // NEGATIVE: InferUntaggedEndpoints=false => untagged target produces neither node nor edge.
        // ---------------------------------------------------------------------

        [Fact]
        public void InferUntaggedEndpoints_false_drops_untagged_target_node_and_edge()
        {
            var model = ScanOnly(
                o => o.InferUntaggedEndpoints = false,
                typeof(ScannerContract.Fixtures.RaiserToUntaggedEvent),
                typeof(ScannerContract.Fixtures.UntaggedEventTarget));

            // No node for the untagged target.
            Assert.DoesNotContain(model.Nodes, n => n.Id == typeof(ScannerContract.Fixtures.UntaggedEventTarget).FullName);
            // No edge to the untagged target.
            Assert.DoesNotContain(model.Edges, e => e.ToId == typeof(ScannerContract.Fixtures.UntaggedEventTarget).FullName);
        }

        // ---------------------------------------------------------------------
        // NEGATIVE: HandlerInterface set to a NON-open-generic type => no handler edges.
        // ---------------------------------------------------------------------

        [Fact]
        public void HandlerInterface_closed_generic_is_ignored_no_throw_no_edges()
        {
            var model = ScanOnly(
                o => o.HandlerInterface = typeof(ScannerContract.Fixtures.IEventHandler<ScannerContract.Fixtures.SampleEvent>),
                typeof(ScannerContract.Fixtures.UntaggedHandler),
                typeof(ScannerContract.Fixtures.SampleEvent));

            // No reaction edges synthesized from handler discovery.
            Assert.DoesNotContain(model.Edges, e =>
                e.Relation == RelationKind.ReactsTo &&
                e.ToId == typeof(ScannerContract.Fixtures.UntaggedHandler).FullName);
        }

        [Fact]
        public void HandlerInterface_plain_class_is_ignored_no_throw_no_edges()
        {
            var model = ScanOnly(
                o => o.HandlerInterface = typeof(ScannerContract.Fixtures.PlainUntagged),
                typeof(ScannerContract.Fixtures.UntaggedHandler),
                typeof(ScannerContract.Fixtures.SampleEvent));

            Assert.DoesNotContain(model.Edges, e =>
                e.Relation == RelationKind.ReactsTo &&
                e.ToId == typeof(ScannerContract.Fixtures.UntaggedHandler).FullName);
        }

        // ---------------------------------------------------------------------
        // NEGATIVE: HandlerInterface left null => no reaction edges from handler discovery.
        // ---------------------------------------------------------------------

        [Fact]
        public void HandlerInterface_null_produces_no_handler_reaction_edges()
        {
            // No HandlerInterface set (null by default). The handler implements IEventHandler<SampleEvent>
            // but is untagged, so without handler discovery it yields no node and no reaction edge.
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.UntaggedHandler),
                typeof(ScannerContract.Fixtures.SampleEvent));

            Assert.DoesNotContain(model.Edges, e => e.Relation == RelationKind.ReactsTo);
            Assert.DoesNotContain(model.Nodes, n => n.Id == typeof(ScannerContract.Fixtures.UntaggedHandler).FullName);
        }

        // ---------------------------------------------------------------------
        // NEGATIVE: abstract class / interface implementing the handler interface is NOT a handler.
        // ---------------------------------------------------------------------

        [Fact]
        public void Abstract_handler_implementation_is_not_treated_as_handler()
        {
            var model = ScanOnly(
                o => o.HandlerInterface = typeof(ScannerContract.Fixtures.IEventHandler<>),
                typeof(ScannerContract.Fixtures.AbstractHandler),
                typeof(ScannerContract.Fixtures.SampleEvent));

            Assert.DoesNotContain(model.Edges, e =>
                e.Relation == RelationKind.ReactsTo &&
                e.ToId == typeof(ScannerContract.Fixtures.AbstractHandler).FullName);
        }

        [Fact]
        public void Interface_extending_the_handler_interface_is_not_treated_as_handler()
        {
            var model = ScanOnly(
                o => o.HandlerInterface = typeof(ScannerContract.Fixtures.IEventHandler<>),
                typeof(ScannerContract.Fixtures.IDerivedHandler),
                typeof(ScannerContract.Fixtures.SampleEvent));

            Assert.DoesNotContain(model.Edges, e =>
                e.Relation == RelationKind.ReactsTo &&
                e.ToId == typeof(ScannerContract.Fixtures.IDerivedHandler).FullName);
        }

        // ---------------------------------------------------------------------
        // NEGATIVE/robustness: a concept type with NO relations => a node with zero edges, no throw.
        // ---------------------------------------------------------------------

        [Fact]
        public void Concept_with_no_relations_yields_node_and_no_edges()
        {
            var model = ScanOnly(typeof(ScannerContract.Fixtures.LonelyAggregate));

            var node = Assert.Single(model.Nodes);
            Assert.Equal(typeof(ScannerContract.Fixtures.LonelyAggregate).FullName, node.Id);
            Assert.Empty(model.Edges);
        }

        // ---------------------------------------------------------------------
        // POSITIVE: DomainModel.Empty has no nodes/edges (referenced by the contract).
        // ---------------------------------------------------------------------

        [Fact]
        public void DomainModel_Empty_has_no_nodes_or_edges()
        {
            Assert.Empty(DomainModel.Empty.Nodes);
            Assert.Empty(DomainModel.Empty.Edges);
        }

        // ---------------------------------------------------------------------
        // POSITIVE: dataflow-plane concepts and edges.
        // ---------------------------------------------------------------------

        [Fact]
        public void Processor_and_data_event_concepts_produce_nodes_with_their_kinds()
        {
            var processor = ScanOnly(typeof(ScannerContract.Fixtures.SampleProcessor));
            Assert.Equal(ConceptKind.Processor, Assert.Single(processor.Nodes).Kind);

            var dataEvent = ScanOnly(typeof(ScannerContract.Fixtures.SampleDataEvent));
            Assert.Equal(ConceptKind.DataEvent, Assert.Single(dataEvent.Nodes).Kind);
        }

        [Fact]
        public void Transforms_on_method_orients_processor_to_data_event()
        {
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.TransformingProcessor),
                typeof(ScannerContract.Fixtures.SampleDataEvent));

            var edge = Assert.Single(model.Edges, e => e.Relation == RelationKind.Transforms);
            Assert.Equal(typeof(ScannerContract.Fixtures.TransformingProcessor).FullName, edge.FromId);
            Assert.Equal(typeof(ScannerContract.Fixtures.SampleDataEvent).FullName, edge.ToId);
        }

        [Fact]
        public void Feeds_on_method_orients_processor_to_downstream_processor()
        {
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.FeedingProcessor),
                typeof(ScannerContract.Fixtures.SampleProcessor));

            var edge = Assert.Single(model.Edges, e => e.Relation == RelationKind.Feeds);
            Assert.Equal(typeof(ScannerContract.Fixtures.FeedingProcessor).FullName, edge.FromId);
            Assert.Equal(typeof(ScannerContract.Fixtures.SampleProcessor).FullName, edge.ToId);
        }

        [Fact]
        public void Translates_on_method_orients_processor_to_domain_event_at_the_seam()
        {
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.TranslatingProcessor),
                typeof(ScannerContract.Fixtures.SampleEvent));

            var edge = Assert.Single(model.Edges, e => e.Relation == RelationKind.Translates);
            Assert.Equal(typeof(ScannerContract.Fixtures.TranslatingProcessor).FullName, edge.FromId);
            Assert.Equal(typeof(ScannerContract.Fixtures.SampleEvent).FullName, edge.ToId);
        }

        [Fact]
        public void Untagged_transforms_target_is_inferred_as_a_data_event()
        {
            var model = ScanOnly(
                typeof(ScannerContract.Fixtures.TransformerToUntagged),
                typeof(ScannerContract.Fixtures.UntaggedDataTarget));

            var node = NodeFor(model, typeof(ScannerContract.Fixtures.UntaggedDataTarget));
            Assert.True(node.Inferred);
            Assert.Equal(ConceptKind.DataEvent, node.Kind);
        }

        [Fact]
        public void Pipeline_and_abstraction_level_are_carried_onto_the_node()
        {
            var model = ScanOnly(typeof(ScannerContract.Fixtures.PipelinedDataEvent));

            var node = Assert.Single(model.Nodes);
            Assert.Equal("AudioIngest", node.Pipeline);
            Assert.Equal(2, node.AbstractionLevel);
        }

        [Fact]
        public void PipelineFromNamespace_fills_pipeline_only_when_not_declared_explicitly()
        {
            var model = ScanOnly(
                o => o.PipelineFromNamespace = _ => "FromNamespace",
                typeof(ScannerContract.Fixtures.SampleProcessor),
                typeof(ScannerContract.Fixtures.PipelinedDataEvent));

            // The explicit attribute wins; the rule fills in the processor that declared none.
            Assert.Equal("FromNamespace", NodeFor(model, typeof(ScannerContract.Fixtures.SampleProcessor)).Pipeline);
            Assert.Equal("AudioIngest", NodeFor(model, typeof(ScannerContract.Fixtures.PipelinedDataEvent)).Pipeline);
        }
    }
}
