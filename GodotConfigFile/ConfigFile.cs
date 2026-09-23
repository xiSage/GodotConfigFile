using System.Text;

namespace GodotConfigFile;

/// <summary>
/// Entry points for reading Godot's configuration text format.
/// </summary>
/// <remarks>
/// <para>
/// This library parses; it does not write. There is no <c>Save</c> and no <c>EncodeToText</c>: the data model is
/// read-only, and the debug rendering on <see cref="Variant.ToString"/> is not a serialization format.
/// </para>
/// <para>
/// The grammar is the one Godot's <c>ConfigFile</c> accepts, reproduced faithfully, including the parts that
/// surprise people — see <c>docs/godot-configfile-spec.md</c>. Deliberate differences are listed in
/// <c>docs/compat-deviations.md</c>.
/// </para>
/// </remarks>
public static class ConfigFile
{
    /// <summary>
    /// The name of the section that holds keys written before any <c>[section]</c> header. Godot uses the empty
    /// string for the same purpose.
    /// </summary>
    public const string RootSection = "";

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly UTF8Encoding LenientUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    /// <summary>Parses configuration text.</summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="options">Options, or <see langword="null"/> for the defaults.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ConfigFileParseException">The input is malformed and <see cref="ParseOptions.ThrowOnError"/> is set.</exception>
    public static ConfigFileDocument Parse(string text, ParseOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        ParseOptions effective = options ?? ParseOptions.Default;
        return Finish(ParsePrepared(text, effective, new DiagnosticBag()), effective);
    }

    /// <summary>Parses configuration text.</summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="options">Options, or <see langword="null"/> for the defaults.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ConfigFileParseException">The input is malformed and <see cref="ParseOptions.ThrowOnError"/> is set.</exception>
    public static ConfigFileDocument Parse(ReadOnlySpan<char> text, ParseOptions? options = null) =>
        Parse(text.ToString(), options);

    /// <summary>Parses UTF-8 encoded configuration data.</summary>
    /// <param name="bytes">The bytes to decode and parse.</param>
    /// <param name="options">Options, or <see langword="null"/> for the defaults.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ConfigFileParseException">The input is malformed and <see cref="ParseOptions.ThrowOnError"/> is set.</exception>
    /// <remarks>
    /// The whole input is decoded once (deviation D4); bytes that are not valid UTF-8 become U+FFFD and produce a
    /// warning rather than failing the parse.
    /// </remarks>
    public static ConfigFileDocument ParseUtf8(ReadOnlySpan<byte> bytes, ParseOptions? options = null)
    {
        ParseOptions effective = options ?? ParseOptions.Default;
        DiagnosticBag diagnostics = new();
        return Finish(ParsePrepared(DecodeUtf8(bytes, diagnostics), effective, diagnostics), effective);
    }

    /// <summary>Reads and parses a file as UTF-8.</summary>
    /// <param name="path">The file to read.</param>
    /// <param name="options">Options, or <see langword="null"/> for the defaults.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ConfigFileParseException">The input is malformed and <see cref="ParseOptions.ThrowOnError"/> is set.</exception>
    /// <remarks>
    /// The file is read as bytes and decoded here, so a byte order mark is handled by this library rather than by
    /// the reader (deviation D1).
    /// </remarks>
    public static ConfigFileDocument Load(string path, ParseOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        return ParseUtf8(File.ReadAllBytes(path), options);
    }

    /// <summary>Parses configuration text without throwing on malformed input.</summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="options">Options, or <see langword="null"/> for the defaults. <see cref="ParseOptions.ThrowOnError"/> is ignored.</param>
    /// <returns>The document, the diagnostics, and whether the parse completed.</returns>
    public static ParseResult ParseWithResult(ReadOnlySpan<char> text, ParseOptions? options = null) =>
        ParsePrepared(text.ToString(), options ?? ParseOptions.Default, new DiagnosticBag());

    /// <summary>Parses UTF-8 encoded configuration data without throwing on malformed input.</summary>
    /// <param name="bytes">The bytes to decode and parse.</param>
    /// <param name="options">Options, or <see langword="null"/> for the defaults. <see cref="ParseOptions.ThrowOnError"/> is ignored.</param>
    /// <returns>The document, the diagnostics, and whether the parse completed.</returns>
    public static ParseResult ParseUtf8WithResult(ReadOnlySpan<byte> bytes, ParseOptions? options = null)
    {
        ParseOptions effective = options ?? ParseOptions.Default;
        DiagnosticBag diagnostics = new();
        return ParsePrepared(DecodeUtf8(bytes, diagnostics), effective, diagnostics);
    }

    private static ConfigFileDocument Finish(ParseResult result, ParseOptions options)
    {
        if (!result.Success && options.ThrowOnError)
        {
            throw new ConfigFileParseException(result.Error!, result.Diagnostics, result.Document);
        }

        return result.Document;
    }

    private static ParseResult ParsePrepared(string text, ParseOptions options, DiagnosticBag diagnostics)
    {
        string prepared = StripBom(text, options, diagnostics);
        Parser parser = new(prepared, diagnostics, options);

        try
        {
            return new ParseResult(parser.ParseDocument(), diagnostics.Items, true);
        }
        catch (ParseAbortException)
        {
            // A failed parse is not transactional: what was read before the error is still returned.
            return new ParseResult(parser.Document, diagnostics.Items, false);
        }
    }

    private static string StripBom(string text, ParseOptions options, DiagnosticBag diagnostics)
    {
        if (!options.StripBom || text.Length == 0 || text[0] != '\uFEFF')
        {
            return text;
        }

        diagnostics.Info(
            DiagnosticCode.BomStripped,
            "Stripped a UTF-8 byte order mark",
            new SourceSpan(new SourcePosition(0, 1, 1), new SourcePosition(1, 1, 2)));
        return text[1..];
    }

    private static string DecodeUtf8(ReadOnlySpan<byte> bytes, DiagnosticBag diagnostics)
    {
        if (bytes.IsEmpty)
        {
            return string.Empty;
        }

        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            // Report where the damage starts, in decoded coordinates, then decode leniently.
            int position = FindFirstInvalidByte(bytes);
            string prefix = LenientUtf8.GetString(bytes[..position]);
            int line = 1;
            int lineStart = 0;
            for (int i = 0; i < prefix.Length; i++)
            {
                if (prefix[i] == '\n')
                {
                    line++;
                    lineStart = i + 1;
                }
            }

            diagnostics.Warning(
                DiagnosticCode.InvalidUtf8Byte,
                "The input is not valid UTF-8; the offending bytes were replaced with U+FFFD",
                SourceSpan.At(new SourcePosition(prefix.Length, line, (prefix.Length - lineStart) + 1)));
            return LenientUtf8.GetString(bytes);
        }
    }

    private static int FindFirstInvalidByte(ReadOnlySpan<byte> bytes)
    {
        for (int length = 1; length <= bytes.Length; length++)
        {
            try
            {
                _ = StrictUtf8.GetString(bytes[..length]);
            }
            catch (DecoderFallbackException)
            {
                return length - 1;
            }
        }

        return bytes.Length;
    }
}
