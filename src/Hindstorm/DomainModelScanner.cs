using System.Reflection;

namespace Hindstorm;

/// <summary>
/// Recovers a <see cref="DomainModel"/> from one or more assemblies by reflecting over Hindstorm's
/// attributes. Nodes come from <see cref="DomainConceptAttribute"/>; edges come from
/// <see cref="DomainRelationAttribute"/> declared on a concept or its members, plus the reaction edges
/// implied by an optional event-handler interface. Method bodies are never read, so every edge is one
/// that was stated as metadata.
/// </summary>
/// <remarks>
/// Guarantees a caller can rely on:
/// <list type="bullet">
/// <item>A relation attribute is only read when it is declared on a tagged concept or one of that
/// concept's members. A relation on an untagged type contributes nothing.</item>
/// <item>When nothing in the assemblies is tagged, the result is an empty model (no nodes, no edges).</item>
/// <item>Identical relations (same endpoints, relation, and member) collapse to a single edge.</item>
/// <item>Output is deterministic: nodes are ordered by namespace then name, edges by endpoints then
/// relation, so two scans of the same input produce identical models.</item>
/// </list>
/// </remarks>
public static class DomainModelScanner
{
    /// <summary>Scans a single assembly with default options.</summary>
    public static DomainModel Scan(Assembly assembly) => Scan([assembly], null);

    /// <summary>Scans a single assembly with the given options.</summary>
    public static DomainModel Scan(Assembly assembly, Action<ScannerOptions>? configure)
        => Scan([assembly], configure);

    /// <summary>Scans a set of assemblies with the given options.</summary>
    public static DomainModel Scan(IEnumerable<Assembly> assemblies, Action<ScannerOptions>? configure)
    {
        var options = new ScannerOptions();
        configure?.Invoke(options);

        var candidates = assemblies
            .SelectMany(GetLoadableTypes)
            .Where(t => options.TypeFilter is null || options.TypeFilter(t))
            .ToList();

        var nodes = new Dictionary<string, DomainNode>(StringComparer.Ordinal);
        var edges = new HashSet<DomainEdge>();

        // Pass 1: a node for every tagged concept.
        foreach (var type in candidates)
        {
            var concept = type.GetCustomAttribute<DomainConceptAttribute>(inherit: false);
            if (concept is not null)
                nodes[IdOf(type)] = NodeFor(type, concept, options);
        }

        // Pass 2: edges declared on a tagged concept or one of its methods.
        foreach (var type in candidates)
        {
            if (type.GetCustomAttribute<DomainConceptAttribute>(inherit: false) is null)
                continue;

            var declaringId = IdOf(type);

            foreach (var relation in type.GetCustomAttributes<DomainRelationAttribute>(inherit: false))
                AddRelationEdge(nodes, edges, declaringId, relation, member: null, options);

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                foreach (var relation in method.GetCustomAttributes<DomainRelationAttribute>(inherit: false))
                    AddRelationEdge(nodes, edges, declaringId, relation, method.Name, options);
        }

        // Pass 3: reaction edges from a generic event-handler interface.
        if (options.HandlerInterface is { IsGenericTypeDefinition: true } handlerInterface)
            AddHandlerEdges(candidates, nodes, edges, handlerInterface, options);

        var orderedNodes = nodes.Values
            .OrderBy(n => n.Namespace, StringComparer.Ordinal)
            .ThenBy(n => n.Name, StringComparer.Ordinal)
            .ToList();

        var orderedEdges = edges
            .OrderBy(e => e.FromId, StringComparer.Ordinal)
            .ThenBy(e => e.ToId, StringComparer.Ordinal)
            .ThenBy(e => e.Relation)
            .ToList();

        return new DomainModel(orderedNodes, orderedEdges);
    }

    private static void AddRelationEdge(
        Dictionary<string, DomainNode> nodes,
        HashSet<DomainEdge> edges,
        string declaringId,
        DomainRelationAttribute relation,
        string? member,
        ScannerOptions options)
    {
        var targetId = IdOf(relation.Target);
        EnsureNode(nodes, relation.Target, InferredKindFor(relation.Kind), options);

        var (fromId, toId) = relation.Direction == RelationDirection.FromDeclaring
            ? (declaringId, targetId)
            : (targetId, declaringId);

        // Drop the edge when an untagged endpoint was suppressed.
        if (!nodes.ContainsKey(fromId) || !nodes.ContainsKey(toId))
            return;

        edges.Add(new DomainEdge(fromId, toId, relation.Kind, member, relation.Label));
    }

    private static void AddHandlerEdges(
        IEnumerable<Type> candidates,
        Dictionary<string, DomainNode> nodes,
        HashSet<DomainEdge> edges,
        Type handlerInterface,
        ScannerOptions options)
    {
        foreach (var type in candidates)
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            foreach (var implemented in type.GetInterfaces())
            {
                if (!implemented.IsGenericType || implemented.GetGenericTypeDefinition() != handlerInterface)
                    continue;

                var eventType = implemented.GetGenericArguments()[0];
                EnsureNode(nodes, eventType, ConceptKind.DomainEvent, options);
                EnsureNode(nodes, type, options.DefaultHandlerKind, options);

                var eventId = IdOf(eventType);
                var handlerId = IdOf(type);
                if (nodes.ContainsKey(eventId) && nodes.ContainsKey(handlerId))
                    edges.Add(new DomainEdge(eventId, handlerId, RelationKind.ReactsTo));
            }
        }
    }

    private static void EnsureNode(Dictionary<string, DomainNode> nodes, Type type, ConceptKind inferredKind, ScannerOptions options)
    {
        var id = IdOf(type);
        if (nodes.ContainsKey(id))
            return;

        if (!options.InferUntaggedEndpoints)
            return;

        nodes[id] = new DomainNode(
            id, type.Name, inferredKind, type.Namespace, Description: null, Inferred: true,
            Context: options.ContextFromNamespace?.Invoke(type.Namespace),
            Pipeline: options.PipelineFromNamespace?.Invoke(type.Namespace));
    }

    private static DomainNode NodeFor(Type type, DomainConceptAttribute concept, ScannerOptions options)
        => new(
            IdOf(type), concept.Name ?? type.Name, concept.Kind, type.Namespace, concept.Description,
            Context: concept.Context ?? options.ContextFromNamespace?.Invoke(type.Namespace),
            Pipeline: concept.Pipeline ?? options.PipelineFromNamespace?.Invoke(type.Namespace),
            AbstractionLevel: concept.AbstractionLevel);

    private static ConceptKind InferredKindFor(RelationKind relation) => relation switch
    {
        RelationKind.Raises => ConceptKind.DomainEvent,
        RelationKind.Handles => ConceptKind.Command,
        RelationKind.ReactsTo => ConceptKind.DomainEvent,
        RelationKind.Issues => ConceptKind.Command,
        RelationKind.Enforces => ConceptKind.Invariant,
        RelationKind.Updates => ConceptKind.ReadModel,
        RelationKind.Transforms => ConceptKind.DataEvent,
        RelationKind.Feeds => ConceptKind.Processor,
        RelationKind.Translates => ConceptKind.DomainEvent,
        _ => ConceptKind.ValueObject,
    };

    private static string IdOf(Type type) => type.FullName ?? type.Name;

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
