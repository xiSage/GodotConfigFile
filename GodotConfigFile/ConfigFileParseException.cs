namespace GodotConfigFile;

/// <summary>
/// Thrown by <see cref="ConfigFile.Parse(string, ParseOptions?)"/> and friends when the input cannot be parsed.
/// </summary>
/// <remarks>
/// A failed parse is not transactional, matching Godot: <see cref="PartialDocument"/> holds everything that had
/// been read before the error.
/// </remarks>
public sealed class ConfigFileParseException : Exception
{
    internal ConfigFileParseException(
        Diagnostic diagnostic,
        IReadOnlyList<Diagnostic> diagnostics,
        ConfigFileDocument partialDocument)
        : base($"{diagnostic.Message} (at line {diagnostic.Span.Start.Line}, column {diagnostic.Span.Start.Column})")
    {
        Diagnostic = diagnostic;
        Diagnostics = diagnostics;
        PartialDocument = partialDocument;
    }

    /// <summary>Gets the error that stopped the parse.</summary>
    public Diagnostic Diagnostic { get; }

    /// <summary>Gets where the error is.</summary>
    public SourceSpan Span => Diagnostic.Span;

    /// <summary>Gets every diagnostic produced before the parse stopped, in order.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>Gets what had been parsed when the error occurred.</summary>
    public ConfigFileDocument PartialDocument { get; }
}
