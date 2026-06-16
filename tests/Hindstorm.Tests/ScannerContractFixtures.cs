using Hindstorm;

// Fixture types for ScannerContractTests. All live in namespace
// Hindstorm.Tests.ScannerContract.Fixtures so the scan can be fenced to just these.
//
// Concept attributes label a type with a ConceptKind; relation attributes declare edges.
// These are intentionally simple, body-less shapes: the scanner never reads method bodies.

namespace Hindstorm.Tests.ScannerContract.Fixtures
{
    // ------------------------------------------------------------------
    // One fixture per ConceptKind (for the parameterized node test).
    // ------------------------------------------------------------------
    [Aggregate]
    public class SampleAggregate { }

    [Command]
    public class SampleCommand { }

    [DomainEvent]
    public class SampleEvent { }

    [Policy]
    public class SamplePolicy { }

    [ReadModel]
    public class SampleReadModel { }

    [ValueObject]
    public class SampleValueObject { }

    [ExternalSystem]
    public class SampleExternalSystem { }

    [Actor]
    public class SampleActor { }

    // ------------------------------------------------------------------
    // Concept with a method-level relation per RelationKind.
    // ------------------------------------------------------------------

    // Raises: declaring -> event (FromDeclaring).
    [Aggregate]
    public class RaiserAggregate
    {
        [Raises(typeof(SampleEvent))]
        public void DoTheThing() { }
    }

    // Handles: command -> declaring (ToDeclaring).
    [Aggregate]
    public class HandlerAggregate
    {
        [Handles(typeof(SampleCommand))]
        public void Apply() { }
    }

    // ReactsTo: event -> declaring (ToDeclaring).
    [Policy]
    public class ReactorPolicy
    {
        [ReactsTo(typeof(SampleEvent))]
        public void When() { }
    }

    // Issues: declaring -> command (FromDeclaring).
    [Policy]
    public class IssuerPolicy
    {
        [Issues(typeof(SampleCommand))]
        public void Decide() { }
    }

    // Enforces: declaring -> policy (FromDeclaring).
    [Aggregate]
    public class EnforcerAggregate
    {
        [Enforces(typeof(SamplePolicy))]
        public void Check() { }
    }

    // Updates: declaring -> read model (FromDeclaring).
    [Policy]
    public class UpdaterPolicy
    {
        [Updates(typeof(SampleReadModel))]
        public void Project() { }
    }

    // ------------------------------------------------------------------
    // Type-level relation (not on a method).
    // ------------------------------------------------------------------
    [Aggregate]
    [Raises(typeof(SampleEvent))]
    public class TypeLevelRaiser { }

    // ------------------------------------------------------------------
    // Name override / Description / Label fixtures.
    // ------------------------------------------------------------------
    [Aggregate(Name = "Custom Display Name")]
    public class NamedAggregate { }

    [Aggregate(Description = "A described aggregate.")]
    public class DescribedAggregate { }

    [Aggregate]
    public class LabelledRaiser
    {
        [Raises(typeof(SampleEvent), Label = "on save")]
        public void Save() { }
    }

    // ------------------------------------------------------------------
    // HandlerInterface discovery fixtures.
    // ------------------------------------------------------------------
    public interface IEventHandler<TEvent> { }

    // A concrete, non-abstract, untagged handler: gets DefaultHandlerKind when discovered.
    public class UntaggedHandler : IEventHandler<SampleEvent> { }

    // Abstract implementer: must NOT be treated as a handler.
    public abstract class AbstractHandler : IEventHandler<SampleEvent> { }

    // An interface that extends the handler interface: must NOT be treated as a handler.
    public interface IDerivedHandler : IEventHandler<SampleEvent> { }

    // ------------------------------------------------------------------
    // Inference fixtures: tagged raiser pointing at an UNTAGGED target.
    // ------------------------------------------------------------------
    [Aggregate]
    public class RaiserToUntaggedEvent
    {
        [Raises(typeof(UntaggedEventTarget))]
        public void Emit() { }
    }

    // No concept attribute: synthesized as an Inferred DomainEvent when InferUntaggedEndpoints is on.
    public class UntaggedEventTarget { }

    // ------------------------------------------------------------------
    // Negative fixtures.
    // ------------------------------------------------------------------

    // A relation on an UNTAGGED type: must contribute nothing.
    public class UntaggedRelationHolder
    {
        [Raises(typeof(SampleEvent))]
        public void Whatever() { }
    }

    // Plain untagged type with no relations and no concept attribute.
    public class PlainUntagged { }

    // Tagged concept with no relations at all.
    [Aggregate]
    public class LonelyAggregate { }

    // Duplicate identical relation on a single member: must collapse to one edge.
    [Aggregate]
    public class DoubleRaiser
    {
        [Raises(typeof(SampleEvent))]
        [Raises(typeof(SampleEvent))]
        public void Fire() { }
    }
}
