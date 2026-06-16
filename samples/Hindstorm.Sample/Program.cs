using Hindstorm;
using Hindstorm.Sample.Ordering;
using Hindstorm.Sample.Shared;

// Recover the storming model for the whole order-to-cash flow straight from the compiled assembly.
var model = DomainModelScanner.Scan(typeof(Order).Assembly, options =>
{
    // Harvest event -> handler reaction edges from the sample's own handler interface, so projections
    // like InventoryLevelsProjection show up without being annotated.
    options.HandlerInterface = typeof(IDomainEventHandler<>);
    options.DefaultHandlerKind = ConceptKind.ReadModel;
});

Console.WriteLine($"Recovered {model.Nodes.Count} concepts and {model.Edges.Count} relationships.");
Console.WriteLine();

// Group by namespace to show the bounded contexts the concepts fall into.
Console.WriteLine("Bounded contexts (by namespace):");
foreach (var context in model.Nodes.GroupBy(n => n.Namespace).OrderBy(g => g.Key, StringComparer.Ordinal))
    Console.WriteLine($"  {context.Key} — {context.Count()} concepts");
Console.WriteLine();

Console.WriteLine("# Mermaid (paste into https://mermaid.live)");
Console.WriteLine();
Console.WriteLine(MermaidExporter.Export(model));

// Write all three formats next to the run so you can open them.
var outDir = Path.Combine(Directory.GetCurrentDirectory(), "out");
Directory.CreateDirectory(outDir);

var written = new (string File, string Content)[]
{
    ("order-to-cash.mmd", MermaidExporter.Export(model)),
    ("order-to-cash.dot", DotExporter.Export(model)),
    ("order-to-cash.json", JsonExporter.Export(model)),
};

foreach (var (file, content) in written)
{
    var path = Path.Combine(outDir, file);
    File.WriteAllText(path, content);
    Console.WriteLine($"Wrote {path}");
}
