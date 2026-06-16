using System.Reflection;

namespace Hindstorm.AspNetCore;

/// <summary>Configures the Hindstorm domain-model endpoint.</summary>
public sealed class HindstormEndpointOptions
{
    /// <summary>
    /// The assemblies to scan. When left empty the entry assembly is used, which is usually the API host.
    /// Add the assemblies that hold your annotated domain types.
    /// </summary>
    public IList<Assembly> Assemblies { get; } = [];

    /// <summary>Optional scanner tuning, for example wiring up a handler interface or a namespace filter.</summary>
    public Action<ScannerOptions>? ConfigureScanner { get; set; }

    /// <summary>
    /// The default response format when the request specifies none. Defaults to <c>json</c>; also accepts
    /// <c>mermaid</c> and <c>dot</c>.
    /// </summary>
    public string DefaultFormat { get; set; } = "json";

    internal IReadOnlyList<Assembly> ResolveAssemblies()
        => Assemblies.Count > 0
            ? [.. Assemblies]
            : [Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()];
}
