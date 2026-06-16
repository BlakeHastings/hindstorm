namespace Hindstorm;

/// <summary>The layout engine a Mermaid flowchart requests.</summary>
public enum MermaidLayout
{
    /// <summary>
    /// The ELK layout engine (the default). Produces cleaner left-to-right layered diagrams, especially
    /// for cyclic flows. Emitted as a <c>%%{init: {"layout": "elk"}}%%</c> directive, which the renderer
    /// (for example mermaid.live) must support.
    /// </summary>
    Elk,

    /// <summary>Mermaid's built-in dagre renderer, with no init directive. Use to opt out of ELK.</summary>
    Dagre,
}
