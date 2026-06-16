using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hindstorm.AspNetCore;

/// <summary>
/// Maps a development endpoint that serves the recovered domain model. The model is scanned once at
/// registration and cached, then served as JSON, Mermaid, or DOT based on a <c>?format=</c> query value.
/// </summary>
public static class HindstormEndpointExtensions
{
    /// <summary>
    /// Maps the domain-model endpoint at <paramref name="pattern"/>. Guard the call behind a development
    /// check if you do not want the model exposed in production.
    /// </summary>
    public static IEndpointConventionBuilder MapHindstorm(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/domain-model",
        Action<HindstormEndpointOptions>? configure = null)
    {
        var options = new HindstormEndpointOptions();
        configure?.Invoke(options);

        var model = new Lazy<DomainModel>(() =>
            DomainModelScanner.Scan(options.ResolveAssemblies(), options.ConfigureScanner));

        return endpoints.MapGet(pattern, (HttpRequest request) =>
        {
            var format = request.Query.TryGetValue("format", out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.ToString()
                : options.DefaultFormat;

            // Mermaid defaults to the ELK renderer; ?layout=dagre opts out.
            var layout = string.Equals(request.Query["layout"], "dagre", StringComparison.OrdinalIgnoreCase)
                ? MermaidLayout.Dagre
                : MermaidLayout.Elk;

            return Render(model.Value, format, layout);
        });
    }

    private static IResult Render(DomainModel model, string format, MermaidLayout layout) => format.ToLowerInvariant() switch
    {
        "mermaid" => Results.Text(MermaidExporter.Export(model, layout), "text/plain"),
        "dot" => Results.Text(DotExporter.Export(model), "text/vnd.graphviz"),
        "json" => Results.Text(JsonExporter.Export(model), "application/json"),
        _ => Results.BadRequest($"Unknown format '{format}'. Use json, mermaid, or dot."),
    };
}
