namespace GodotConfigFile;

/// <summary>
/// A single message produced while parsing: a deviation that was applied, or a reason parsing failed.
/// </summary>
/// <param name="Severity">How serious the message is.</param>
/// <param name="Code">Machine-readable classification.</param>
/// <param name="Message">Human-readable text. Error messages mirror Godot's own wording.</param>
/// <param name="Span">The range of input the message is about.</param>
public sealed record Diagnostic(DiagnosticSeverity Severity, DiagnosticCode Code, string Message, SourceSpan Span)
{
    /// <summary>Returns <c>span: severity code: message</c>.</summary>
    /// <returns>A single-line rendering of the diagnostic.</returns>
    public override string ToString() => $"{Span}: {Severity} {Code}: {Message}";
}
