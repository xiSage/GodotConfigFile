namespace GodotConfigFile;

/// <summary>
/// A half-open range of the parsed text: <see cref="Start"/> is included, <see cref="End"/> is excluded.
/// </summary>
/// <param name="Start">Position of the first character of the range.</param>
/// <param name="End">Position just past the last character of the range.</param>
public readonly record struct SourceSpan(SourcePosition Start, SourcePosition End)
{
    /// <summary>Creates an empty span that sits at <paramref name="position"/>.</summary>
    /// <param name="position">The position to point at.</param>
    /// <returns>A span whose start and end are both <paramref name="position"/>.</returns>
    public static SourceSpan At(SourcePosition position) => new(position, position);

    /// <summary>Returns the span as <c>line:column</c> or <c>start-end</c>.</summary>
    /// <returns>A human-readable representation of the range.</returns>
    public override string ToString() => Start == End ? Start.ToString() : $"{Start}-{End}";
}
