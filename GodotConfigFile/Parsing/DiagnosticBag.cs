namespace GodotConfigFile;

/// <summary>
/// Collects diagnostics while parsing, and aborts on errors.
/// </summary>
/// <remarks>
/// Errors are reported by throwing <see cref="ParseAbortException"/>; the diagnostic is recorded in the bag
/// first, so a caller that catches the exception still sees every message in order. The document built so far is
/// kept by the parser, which is how a partial document survives a failed parse — the same thing Godot does.
/// </remarks>
internal sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _items = [];

    /// <summary>Gets the diagnostics reported so far, in order.</summary>
    public IReadOnlyList<Diagnostic> Items => _items;

    /// <summary>Gets a value indicating whether any diagnostic has <see cref="DiagnosticSeverity.Error"/>.</summary>
    public bool HasErrors { get; private set; }

    /// <summary>Records an informational diagnostic.</summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The message.</param>
    /// <param name="span">Where it happened.</param>
    public void Info(DiagnosticCode code, string message, SourceSpan span) =>
        Add(new Diagnostic(DiagnosticSeverity.Info, code, message, span));

    /// <summary>Records a warning.</summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The message.</param>
    /// <param name="span">Where it happened.</param>
    public void Warning(DiagnosticCode code, string message, SourceSpan span) =>
        Add(new Diagnostic(DiagnosticSeverity.Warning, code, message, span));

    /// <summary>Records an error and aborts the parse.</summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The message, mirroring Godot's wording.</param>
    /// <param name="span">Where it happened.</param>
    /// <exception cref="ParseAbortException">Always thrown.</exception>
    public void Fail(DiagnosticCode code, string message, SourceSpan span)
    {
        Diagnostic diagnostic = new(DiagnosticSeverity.Error, code, message, span);
        Add(diagnostic);
        throw new ParseAbortException(diagnostic);
    }

    private void Add(Diagnostic diagnostic)
    {
        _items.Add(diagnostic);
        if (diagnostic.Severity == DiagnosticSeverity.Error)
        {
            HasErrors = true;
        }
    }
}
