namespace GodotConfigFile;

/// <summary>
/// The token kinds produced by <see cref="Lexer"/>, mirroring Godot's <c>VariantParser::TokenType</c>.
/// </summary>
internal enum TokenType
{
    /// <summary><c>{</c></summary>
    CurlyBracketOpen,

    /// <summary><c>}</c></summary>
    CurlyBracketClose,

    /// <summary><c>[</c></summary>
    BracketOpen,

    /// <summary><c>]</c></summary>
    BracketClose,

    /// <summary><c>(</c></summary>
    ParenthesisOpen,

    /// <summary><c>)</c></summary>
    ParenthesisClose,

    /// <summary>An identifier: <c>[A-Za-z_][A-Za-z0-9_]*</c>, optionally prefixed with <c>-</c>.</summary>
    Identifier,

    /// <summary>A string literal.</summary>
    String,

    /// <summary>A StringName literal: <c>&amp;"name"</c> or the deprecated <c>@"name"</c>.</summary>
    StringName,

    /// <summary>An integer or float literal.</summary>
    Number,

    /// <summary>A <c>#rrggbb</c> style colour literal.</summary>
    Color,

    /// <summary><c>:</c></summary>
    Colon,

    /// <summary><c>,</c></summary>
    Comma,

    /// <summary><c>.</c></summary>
    Period,

    /// <summary><c>=</c></summary>
    Equal,

    /// <summary>End of input.</summary>
    Eof,
}

/// <summary>
/// A token plus the value it carries and where it came from.
/// </summary>
internal readonly struct Token
{
    /// <summary>Initializes a token.</summary>
    /// <param name="type">The token kind.</param>
    /// <param name="value">The value carried by the token, or <see cref="Variant.Nil.Instance"/> for punctuation.</param>
    /// <param name="span">Where the token came from.</param>
    public Token(TokenType type, Variant value, SourceSpan span)
    {
        Type = type;
        Value = value;
        Span = span;
    }

    /// <summary>Gets the token kind.</summary>
    public TokenType Type { get; }

    /// <summary>Gets the carried value.</summary>
    public Variant Value { get; }

    /// <summary>Gets the source range of the token.</summary>
    public SourceSpan Span { get; }

    /// <summary>
    /// Gets the name Godot uses for this token kind in <c>Expected value, got '...'</c> messages.
    /// </summary>
    public string Name => Type switch
    {
        TokenType.CurlyBracketOpen => "'{'",
        TokenType.CurlyBracketClose => "'}'",
        TokenType.BracketOpen => "'['",
        TokenType.BracketClose => "']'",
        TokenType.ParenthesisOpen => "'('",
        TokenType.ParenthesisClose => "')'",
        TokenType.Identifier => "identifier",
        TokenType.String => "string",
        TokenType.StringName => "string_name",
        TokenType.Number => "number",
        TokenType.Color => "color",
        TokenType.Colon => "':'",
        TokenType.Comma => "','",
        TokenType.Period => "'.'",
        TokenType.Equal => "'='",
        _ => "EOF",
    };
}
