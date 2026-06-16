using Hindstorm;
using Hindstorm.Sample;

// Recover the storming model for the Orders domain straight from the compiled assembly, then export it.
var model = DomainModelScanner.Scan(typeof(Order).Assembly, options =>
{
    // Harvest the event -> handler reaction edges from the sample's own handler interface, so they show
    // up without annotating OrderSummaryProjection.
    options.HandlerInterface = typeof(IDomainEventHandler<>);
    options.DefaultHandlerKind = ConceptKind.ReadModel;
});

Console.WriteLine($"Recovered {model.Nodes.Count} concepts and {model.Edges.Count} relationships.");
Console.WriteLine();
Console.WriteLine("# Mermaid (paste into https://mermaid.live)");
Console.WriteLine();
Console.WriteLine(MermaidExporter.Export(model));

// Write all three formats next to the run so you can open them.
var outDir = Path.Combine(Directory.GetCurrentDirectory(), "out");
Directory.CreateDirectory(outDir);

var written = new (string File, string Content)[]
{
    ("orders.mmd", MermaidExporter.Export(model)),
    ("orders.dot", DotExporter.Export(model)),
    ("orders.json", JsonExporter.Export(model)),
};

foreach (var (file, content) in written)
{
    var path = Path.Combine(outDir, file);
    File.WriteAllText(path, content);
    Console.WriteLine($"Wrote {path}");
}
