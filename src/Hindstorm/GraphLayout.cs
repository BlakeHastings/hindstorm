namespace Hindstorm;

/// <summary>
/// Orders a model's nodes and edges for a left-to-right layout. Entry points (nodes with no incoming
/// edge, such as actors and initiating external systems) come first, then a breadth-first walk along the
/// flow, so a renderer lays the graph out from its entry points rather than from an arbitrary node. Ties
/// are broken by the model's own (already deterministic) order, so the result is deterministic too.
/// </summary>
internal static class GraphLayout
{
    internal readonly record struct Ordered(
        IReadOnlyList<DomainNode> Nodes,
        IReadOnlyList<DomainEdge> Edges,
        IReadOnlyCollection<string> EntryIds);

    public static Ordered Order(DomainModel model)
    {
        var byId = new Dictionary<string, DomainNode>(StringComparer.Ordinal);
        var incoming = new Dictionary<string, int>(StringComparer.Ordinal);
        var outgoing = new Dictionary<string, List<DomainEdge>>(StringComparer.Ordinal);
        foreach (var node in model.Nodes)
        {
            byId[node.Id] = node;
            incoming[node.Id] = 0;
            outgoing[node.Id] = [];
        }

        foreach (var edge in model.Edges)
        {
            if (outgoing.TryGetValue(edge.FromId, out var list))
                list.Add(edge);
            if (incoming.ContainsKey(edge.ToId))
                incoming[edge.ToId]++;
        }

        // Order each node's out-edges so the ones pointing at a terminal (a node with no outgoing edges
        // of its own, like an invariant or a read model) come first. Renderers float the first-declared
        // out-edge's target to the top, so dead-ends sit above the node that continues the flow and the
        // graph expands down and to the right. OrderBy is stable, so model order is kept within a group.
        var outDegree = outgoing.ToDictionary(kv => kv.Key, kv => kv.Value.Count, StringComparer.Ordinal);
        bool IsTerminal(string id) => !outDegree.TryGetValue(id, out var degree) || degree == 0;
        foreach (var id in outgoing.Keys.ToList())
            outgoing[id] = [.. outgoing[id].OrderBy(e => IsTerminal(e.ToId) ? 0 : 1)];

        // Entry points: real nodes nothing points at that do start a flow (an isolated node with no
        // edges at all is not an entry point). Kept in the model's order.
        var entryIds = model.Nodes
            .Where(n => incoming[n.Id] == 0 && outgoing[n.Id].Count > 0)
            .Select(n => n.Id)
            .ToList();

        var orderedNodes = new List<DomainNode>(model.Nodes.Count);
        var orderedEdges = new List<DomainEdge>(model.Edges.Count);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();

        foreach (var id in entryIds)
            if (visited.Add(id))
                queue.Enqueue(id);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            orderedNodes.Add(byId[id]);
            foreach (var edge in outgoing[id])
            {
                orderedEdges.Add(edge);
                if (byId.ContainsKey(edge.ToId) && visited.Add(edge.ToId))
                    queue.Enqueue(edge.ToId);
            }
        }

        // Nodes only reachable through a cycle (no entry point leads to them) and any isolated nodes,
        // appended in model order so nothing is dropped.
        foreach (var node in model.Nodes)
            if (visited.Add(node.Id))
            {
                orderedNodes.Add(node);
                foreach (var edge in outgoing[node.Id])
                    orderedEdges.Add(edge);
            }

        // Any edges still not emitted (their source was appended above, or duplicates), in model order.
        if (orderedEdges.Count != model.Edges.Count)
        {
            var emitted = new HashSet<DomainEdge>(orderedEdges);
            foreach (var edge in model.Edges)
                if (emitted.Add(edge))
                    orderedEdges.Add(edge);
        }

        return new Ordered(orderedNodes, orderedEdges, entryIds);
    }

    /// <summary>Which plane a group of nodes sits on: the transactional domain or the streaming dataflow.</summary>
    internal enum Plane
    {
        /// <summary>A bounded context on the transactional domain plane.</summary>
        Context,

        /// <summary>A dataflow pipeline on the streaming plane.</summary>
        Pipeline,
    }

    internal readonly record struct Group(string Label, Plane Plane, IReadOnlyList<DomainNode> Nodes);

    internal readonly record struct Grouping(
        IReadOnlyList<Group> Groups,
        IReadOnlyList<DomainNode> Ungrouped,
        bool AnyGroup)
    {
        /// <summary>True when at least one group is drawn (a context or a pipeline boundary).</summary>
        public bool AnyContext => AnyGroup;
    }

    /// <summary>
    /// Buckets nodes into boundary groups: a dataflow <see cref="Plane.Pipeline"/> when the node declares a
    /// pipeline, otherwise a bounded <see cref="Plane.Context"/> when it declares one. A node's pipeline
    /// wins over its context, so a streaming concept lands in its pipeline lane rather than a domain box.
    /// Order within a bucket is preserved and buckets are ordered by first appearance, with pipelines drawn
    /// after contexts so the two planes read as separate bands. Nodes in neither are returned ungrouped.
    /// When nothing declares a context or a pipeline, <see cref="Grouping.AnyGroup"/> is false and an
    /// exporter can skip drawing boundaries entirely.
    /// </summary>
    public static Grouping GroupByContext(IReadOnlyList<DomainNode> nodes)
    {
        if (!nodes.Any(n => n.Context is not null || n.Pipeline is not null))
            return new Grouping([], nodes, false);

        var order = new List<(string Label, Plane Plane)>();
        var buckets = new Dictionary<(string, Plane), List<DomainNode>>();
        var ungrouped = new List<DomainNode>();

        foreach (var node in nodes)
        {
            // A declared pipeline places the node on the streaming plane; otherwise its context, if any,
            // places it on the domain plane. A node with neither is ungrouped.
            var key = node.Pipeline is not null
                ? (node.Pipeline, Plane.Pipeline)
                : node.Context is not null
                    ? (node.Context, Plane.Context)
                    : ((string, Plane)?)null;

            if (key is null)
            {
                ungrouped.Add(node);
                continue;
            }

            if (!buckets.TryGetValue(key.Value, out var bucket))
            {
                bucket = [];
                buckets[key.Value] = bucket;
                order.Add(key.Value);
            }

            bucket.Add(node);
        }

        // Contexts first, then pipelines, each kept in first-appearance order, so the domain plane and the
        // dataflow plane render as two distinct bands rather than interleaved.
        var groups = order
            .OrderBy(k => k.Plane)
            .Select(k => new Group(k.Label, k.Plane, buckets[k]))
            .ToList();

        return new Grouping(groups, ungrouped, true);
    }
}
