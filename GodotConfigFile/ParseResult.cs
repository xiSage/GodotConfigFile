namespace GodotConfigFile;

/// <summary>
/// The outcome of a parse that does not throw: the document, plus everything the parser had to say about the input.
/// </summary>
public sealed class ParseResult
{
    internal ParseResult(ConfigFileDocument document, IReadOnlyList<Diagnostic> diagnostics, bool success)
    {
        Document = document;
        Diagnostics = diagnostics;
        Success = success;
    }

    /// <summary>
    /// Gets the parsed document. Never <see langword="null"/>: when parsing failed this is the partial document
    /// built before the error, just as Godot keeps what it had already read.
    /// </summary>
    public ConfigFileDocument Document { get; }

    /// <summary>Gets every diagnostic produced, in order.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>Gets a value indicating whether the parse completed without an error diagnostic.</summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the first diagnostic that is an error, or <see langword="null"/> when the parse completed.
    /// </summary>
    public Diagnostic? Error
    {
        get
        {
            foreach (Diagnostic diagnostic in Diagnostics)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                {
                    return diagnostic;
                }
            }

            return null;
        }
    }

    /// <summary>Gets the diagnostics that are warnings or worse.</summary>
    /// <returns>The diagnostics that are not informational.</returns>
    public IEnumerable<Diagnostic> Problems
    {
        get
        {
            foreach (Diagnostic diagnostic in Diagnostics)
            {
                if (diagnostic.Severity != DiagnosticSeverity.Info)
                {
                    yield return diagnostic;
                }
            }
        }
    }
}
