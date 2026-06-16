using Hindstorm;
using Xunit;
using System;
using System.Linq;
using System.Reflection;

namespace Hindstorm.Tests;

// Contract-only tests for the Hindstorm annotation attributes. These assert the public metadata
// surface that the reflection-based scanner depends on: every concept attribute reports its Kind
// and is reachable via the single DomainConceptAttribute base; every relation attribute reports
// its fixed Kind + Direction, stores its Target, and rejects a null target.
public class AnnotationContractTests
{
    // ----------------------------------------------------------------------------------------
    // POSITIVE: concept attributes report the matching Kind.
    // ----------------------------------------------------------------------------------------

    public static TheoryData<Type, ConceptKind> ConceptKindMap => new()
    {
        { typeof(AggregateAttribute), ConceptKind.Aggregate },
        { typeof(CommandAttribute), ConceptKind.Command },
        { typeof(DomainEventAttribute), ConceptKind.DomainEvent },
        { typeof(PolicyAttribute), ConceptKind.Policy },
        { typeof(ReadModelAttribute), ConceptKind.ReadModel },
        { typeof(ValueObjectAttribute), ConceptKind.ValueObject },
        { typeof(ExternalSystemAttribute), ConceptKind.ExternalSystem },
        { typeof(ActorAttribute), ConceptKind.Actor },
    };

    [Theory]
    [MemberData(nameof(ConceptKindMap))]
    public void ConceptAttribute_ReportsMatchingKind(Type attributeType, ConceptKind expected)
    {
        var instance = (DomainConceptAttribute)Activator.CreateInstance(attributeType)!;

        Assert.Equal(expected, instance.Kind);
    }

    [Theory]
    [MemberData(nameof(ConceptKindMap))]
    public void ConceptAttribute_IsAssignableToDomainConceptBase(Type attributeType, ConceptKind expected)
    {
        // expected is unused here; the base-type contract is what matters for the scanner.
        _ = expected;

        // The scanner finds every concept by looking for this one base type, so each sealed
        // concept attribute must derive from DomainConceptAttribute (and thus from Attribute).
        Assert.True(typeof(DomainConceptAttribute).IsAssignableFrom(attributeType));
        Assert.True(typeof(Attribute).IsAssignableFrom(attributeType));
    }

    [Fact]
    public void ConceptAttribute_NameAndDescription_DefaultToNull()
    {
        var attr = new AggregateAttribute();

        Assert.Null(attr.Name);
        Assert.Null(attr.Description);
    }

    [Fact]
    public void ConceptAttribute_NameAndDescription_RoundTripViaInitializer()
    {
        var attr = new AggregateAttribute { Name = "Order", Description = "An order aggregate." };

        Assert.Equal("Order", attr.Name);
        Assert.Equal("An order aggregate.", attr.Description);
    }

    // ----------------------------------------------------------------------------------------
    // POSITIVE: relation attributes report Kind + Direction and store the Target.
    // ----------------------------------------------------------------------------------------

    public static TheoryData<Type, RelationKind, RelationDirection> RelationMap => new()
    {
        { typeof(RaisesAttribute), RelationKind.Raises, RelationDirection.FromDeclaring },
        { typeof(HandlesAttribute), RelationKind.Handles, RelationDirection.ToDeclaring },
        { typeof(ReactsToAttribute), RelationKind.ReactsTo, RelationDirection.ToDeclaring },
        { typeof(IssuesAttribute), RelationKind.Issues, RelationDirection.FromDeclaring },
        { typeof(EnforcesAttribute), RelationKind.Enforces, RelationDirection.FromDeclaring },
        { typeof(UpdatesAttribute), RelationKind.Updates, RelationDirection.FromDeclaring },
    };

    [Theory]
    [MemberData(nameof(RelationMap))]
    public void RelationAttribute_ReportsKindDirectionAndTarget(
        Type attributeType, RelationKind expectedKind, RelationDirection expectedDirection)
    {
        var target = typeof(SomeTargetType);

        // Each sealed relation attribute has a single (Type) constructor.
        var instance = (DomainRelationAttribute)Activator.CreateInstance(attributeType, target)!;

        Assert.Equal(expectedKind, instance.Kind);
        Assert.Equal(expectedDirection, instance.Direction);
        Assert.Same(target, instance.Target);
    }

    [Theory]
    [MemberData(nameof(RelationMap))]
    public void RelationAttribute_IsAssignableToDomainRelationBase(
        Type attributeType, RelationKind expectedKind, RelationDirection expectedDirection)
    {
        _ = expectedKind;
        _ = expectedDirection;

        Assert.True(typeof(DomainRelationAttribute).IsAssignableFrom(attributeType));
        Assert.True(typeof(Attribute).IsAssignableFrom(attributeType));
    }

    [Fact]
    public void RelationAttribute_Label_DefaultsToNull()
    {
        var attr = new RaisesAttribute(typeof(SomeTargetType));

        Assert.Null(attr.Label);
    }

    [Fact]
    public void RelationAttribute_Label_RoundTripsViaInitializer()
    {
        var attr = new RaisesAttribute(typeof(SomeTargetType)) { Label = "on success" };

        Assert.Equal("on success", attr.Label);
    }

    // ----------------------------------------------------------------------------------------
    // NEGATIVE: a null target is the documented failure mode -> ArgumentNullException.
    // Covered for more than the required two relation attributes to exercise the shared base ctor.
    // ----------------------------------------------------------------------------------------

    [Fact]
    public void RaisesAttribute_NullTarget_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new RaisesAttribute(null!));
    }

    [Fact]
    public void HandlesAttribute_NullTarget_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new HandlesAttribute(null!));
    }

    [Fact]
    public void IssuesAttribute_NullTarget_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new IssuesAttribute(null!));
    }

    // ----------------------------------------------------------------------------------------
    // POSITIVE: applied-and-read-back via reflection. This is the actual scanner path: discover
    // concepts on types and relations on methods through the base types.
    // ----------------------------------------------------------------------------------------

    [Fact]
    public void ConceptAttribute_IsReadableFromTypeViaBaseType()
    {
        var concept = typeof(AnnotationContract.Fixtures.OrderAggregate)
            .GetCustomAttribute<DomainConceptAttribute>();

        Assert.NotNull(concept);
        Assert.Equal(ConceptKind.Aggregate, concept!.Kind);
        Assert.Equal("Order", concept.Name);
    }

    [Fact]
    public void RelationAttribute_IsReadableFromMethodViaBaseType()
    {
        var method = typeof(AnnotationContract.Fixtures.OrderAggregate)
            .GetMethod(nameof(AnnotationContract.Fixtures.OrderAggregate.Place))!;

        // Place carries multiple relation attributes, so read them all through the single base type
        // (the same path the scanner uses) rather than the singular GetCustomAttribute overload.
        var relations = method.GetCustomAttributes<DomainRelationAttribute>().ToList();

        Assert.NotEmpty(relations);
        Assert.All(relations, r => Assert.Equal(RelationKind.Raises, r.Kind));
        Assert.All(relations, r => Assert.Equal(RelationDirection.FromDeclaring, r.Direction));
        Assert.Contains(relations, r => r.Target == typeof(AnnotationContract.Fixtures.OrderPlaced));
    }

    // ASSUMES: AllowMultiple is part of the relation-attribute contract (the prompt lists two
    // [Raises] on one method reading back as a should-work case). If AllowMultiple were false,
    // declaring two [Raises] on Place would be a compile error and this whole file would fail to
    // build, so this is a build-time guard as much as a runtime assertion. See BLOCK question 1.
    [Fact]
    public void RelationAttribute_AllowsMultipleOnSameMethod()
    {
        var method = typeof(AnnotationContract.Fixtures.OrderAggregate)
            .GetMethod(nameof(AnnotationContract.Fixtures.OrderAggregate.Place))!;

        var raised = method.GetCustomAttributes<RaisesAttribute>().ToArray();

        Assert.Equal(2, raised.Length);
        Assert.Contains(raised, r => r.Target == typeof(AnnotationContract.Fixtures.OrderPlaced));
        Assert.Contains(raised, r => r.Target == typeof(AnnotationContract.Fixtures.OrderRejected));
    }

    // A plain target type used by the constructor-level relation tests.
    private sealed class SomeTargetType
    {
    }
}
