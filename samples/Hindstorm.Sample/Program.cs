using Hindstorm;
using Hindstorm.Sample.Ordering;
using Hindstorm.Sample.Shared;
using Hindstorm.Sample.Streaming;

const string StreamingNamespace = "Hindstorm.Sample.Streaming";

// Recover the storming model for the whole order-to-cash flow straight from the compiled assembly.
var model = DomainModelScanner.Scan(typeof(Order).Assembly, options =>
{
    // Keep the streaming pipeline out of this transactional model; it is scanned on its own below.
    options.TypeFilter = t => t.Namespace != StreamingNamespace;

    // Harvest event -> handler reaction edges from the sample's own handler interface, so projections
    // like InventoryLevelsProjection show up without being annotated.
    options.HandlerInterface = typeof(IDomainEventHandler<>);
    options.DefaultHandlerKind = ConceptKind.ReadModel;

    // Derive each concept's bounded context from the last namespace segment (Ordering, Payments, ...).
    // None of the sample types set an explicit Context, so this rule supplies them all; an explicit
    // [Aggregate(Context = "...")] would win over it.
    options.ContextFromNamespace = ns => ns?.Split('.').Last();
});

Console.WriteLine($"Recovered {model.Nodes.Count} concepts and {model.Edges.Count} relationships.");
Console.WriteLine();

// Group by the declared bounded context.
Console.WriteLine("Bounded contexts:");
foreach (var context in model.Nodes.GroupBy(n => n.Context).OrderBy(g => g.Key, StringComparer.Ordinal))
    Console.WriteLine($"  {context.Key ?? "(none)"} — {context.Count()} concepts");
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

Console.WriteLine();
Console.WriteLine(new string('=', 80));
Console.WriteLine();

// A second, separate model: the real-time audio pipeline on the dataflow plane. Scanned on its own so
// the two planes are not mashed into one graph; the [Translates] seam is the only join into the domain.
var streaming = DomainModelScanner.Scan(typeof(Microphone).Assembly, options =>
    options.TypeFilter = t => t.Namespace == StreamingNamespace);

Console.WriteLine(
    $"Recovered {streaming.Nodes.Count} streaming concepts and {streaming.Edges.Count} relationships.");
Console.WriteLine();
Console.WriteLine("# Mermaid: the AudioIngest dataflow pipeline and its translation seam");
Console.WriteLine();
Console.WriteLine(MermaidExporter.Export(streaming));

var streamingWritten = new (string File, string Content)[]
{
    ("audio-ingest.mmd", MermaidExporter.Export(streaming)),
    ("audio-ingest.dot", DotExporter.Export(streaming)),
    ("audio-ingest.json", JsonExporter.Export(streaming)),
};

foreach (var (file, content) in streamingWritten)
{
    var path = Path.Combine(outDir, file);
    File.WriteAllText(path, content);
    Console.WriteLine($"Wrote {path}");
}
