namespace GodotConfigFile;

/// <summary>
/// Severity of a <see cref="Diagnostic"/> produced while parsing.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>The parser adjusted something harmless, or reported a deprecated-but-accepted construct.</summary>
    Info,

    /// <summary>The input was accepted, but the result may differ from what Godot would produce.</summary>
    Warning,

    /// <summary>The input is malformed. Parsing either stopped here or the value could not be produced.</summary>
    Error,
}
