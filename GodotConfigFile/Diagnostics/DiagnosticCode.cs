namespace GodotConfigFile;

/// <summary>
/// Machine-readable classification of a <see cref="Diagnostic"/>.
/// </summary>
/// <remarks>
/// <para>
/// The code is the stable part of the API; <see cref="Diagnostic.Message"/> mirrors Godot's own wording so a
/// report can be compared against the reference implementation, but is not meant to be matched on.
/// </para>
/// <para>
/// Godot emits a number of distinct <c>Expected '...'</c> style messages. Those all share
/// <see cref="ExpectedToken"/>; the specific expectation is carried by the message.
/// </para>
/// </remarks>
public enum DiagnosticCode
{
    // ── Errors. ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>A character that cannot start any token was found (Godot: <c>Unexpected character</c>).</summary>
    UnexpectedCharacter,

    /// <summary>An identifier was found where a value cannot be formed from it (Godot: <c>Unexpected identifier '...'</c>).</summary>
    UnexpectedIdentifier,

    /// <summary>A token that cannot start a value was found (Godot: <c>Expected value, got '...'</c>).</summary>
    ExpectedValue,

    /// <summary>A literal reached the end of input before its closing delimiter (Godot: <c>Unterminated string</c>).</summary>
    UnterminatedString,

    /// <summary>A <c>\u</c> or <c>\U</c> escape contained a non-hexadecimal character.</summary>
    MalformedHexEscape,

    /// <summary>A UTF-16 surrogate appeared without its pair.</summary>
    UnpairedSurrogate,

    /// <summary>The input did not match the grammar where a specific token was required. The message says which.</summary>
    ExpectedToken,

    /// <summary>A separator was missing between two elements (Godot: <c>Expected ','</c>).</summary>
    ExpectedComma,

    /// <summary>A key/value separator was missing (Godot: <c>Expected ':'</c>).</summary>
    ExpectedColon,

    /// <summary>The input ended in the middle of a construct (arrays, dictionaries, tags, <c>Object()</c>).</summary>
    UnexpectedEof,

    /// <summary>A <c>&amp;</c> or <c>@</c> was not followed by a double quote (Godot: <c>Expected '"' after '&amp;'</c>).</summary>
    MissingQuoteAfterAmpersand,

    /// <summary>A constructor received the wrong number of arguments.</summary>
    ConstructorArgumentCount,

    /// <summary>A constructor argument was not a number (Godot: <c>Expected float/number in constructor</c>).</summary>
    ConstructorArgumentType,

    /// <summary>A <c>PackedByteArray("...")</c> literal was not valid base64. Godot treats this as fatal.</summary>
    InvalidBase64,

    /// <summary>A resource reference carried two UID paths or two non-UID paths.</summary>
    InvalidResourceReference,

    /// <summary>An element of a typed container did not match the declared type and could not be converted.</summary>
    IncompatibleElement,

    /// <summary>The nesting limit in <see cref="ParseOptions.MaxDepth"/> was reached.</summary>
    DepthLimitExceeded,

    // ── Non-fatal: deliberate deviations, documented in docs/compat-deviations.md. ───────────────

    /// <summary>A UTF-8 byte order mark was stripped (deviation D1).</summary>
    BomStripped,

    /// <summary>A byte sequence was not valid UTF-8 and became U+FFFD (deviation D4).</summary>
    InvalidUtf8Byte,

    /// <summary>An integer literal did not fit in a signed 64-bit integer and was saturated (deviation D6).</summary>
    IntegerOverflow,

    /// <summary>A type name inside <c>Array[...]</c> or <c>Dictionary[...]</c> was not recognised (deviation D8).</summary>
    UnknownTypeName,

    /// <summary>The deprecated <c>@"name"</c> spelling of a StringName was accepted (Godot still accepts it).</summary>
    DeprecatedAtStringName,

    /// <summary>A key was assigned more than once in the same section; the last assignment won, as in Godot.</summary>
    DuplicateKey,

    /// <summary>A <c>#</c> colour literal had a length Godot does not accept and became opaque black (deviation D10).</summary>
    MalformedHexColor,
}
