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
}
