namespace GodotConfigFile;

/// <summary>
/// Knobs for <see cref="ConfigFile.Parse(string, ParseOptions?)"/> and friends.
/// </summary>
/// <remarks>
/// The defaults reproduce Godot's observable behaviour except where a deliberate deviation is documented in
/// <c>docs/compat-deviations.md</c>.
/// </remarks>
public sealed record ParseOptions
{
    /// <summary>The default options.</summary>
    public static ParseOptions Default { get; } = new();

    /// <summary>
    /// Maximum nesting depth of arrays and dictionaries. Exceeding it is an error (deviation D5).
    /// Godot's parser has no such limit and can overflow the stack. Defaults to 100, matching Godot's writer.
    /// </summary>
    public int MaxDepth { get; init; } = 100;

    /// <summary>
    /// Strip a leading UTF-8 byte order mark and report <see cref="DiagnosticCode.BomStripped"/> (deviation D1).
    /// Godot does not strip it and folds the three BOM bytes into the first key name.
    /// </summary>
    public bool StripBom { get; init; } = true;

    /// <summary>
    /// Keep top-level <c>key=null</c> assignments as <see cref="Variant.Nil"/> instead of applying Godot's
    /// erase-on-null rule. Leave this <see langword="false"/> for a single file; turn it on when the document is
    /// going to be merged into another one and nulls must delete the base keys.
    /// </summary>
    public bool PreserveNilKeys { get; init; }

    /// <summary>
    /// Whether <see cref="ConfigFile.Parse(string, ParseOptions?)"/> throws <see cref="ConfigFileParseException"/>
    /// when an error diagnostic is produced. <see cref="ConfigFile.ParseWithResult"/> never throws.
    /// </summary>
    public bool ThrowOnError { get; init; } = true;
}
