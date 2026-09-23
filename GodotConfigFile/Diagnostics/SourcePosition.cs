namespace GodotConfigFile;

/// <summary>
/// A position inside the parsed text.
/// </summary>
/// <param name="Offset">Zero-based index of the character, counted in UTF-16 code units.</param>
/// <param name="Line">One-based line number, counted over the text as written (see deviation D9).</param>
/// <param name="Column">One-based column, counted in UTF-16 code units.</param>
/// <remarks>
/// For input read as UTF-8 bytes (<see cref="ConfigFile.ParseUtf8"/>), offsets and columns refer to the
/// decoded text, not to the original bytes.
/// </remarks>
public readonly record struct SourcePosition(int Offset, int Line, int Column)
{
    /// <summary>Returns the position as <c>line:column</c>.</summary>
    /// <returns>A string of the form <c>12:5</c>.</returns>
    public override string ToString() => $"{Line}:{Column}";
}
