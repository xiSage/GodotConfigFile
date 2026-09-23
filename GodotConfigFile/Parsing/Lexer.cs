using System.Text;

namespace GodotConfigFile;

/// <summary>
/// The tokenizer, a faithful port of <c>VariantParser::get_token</c>.
/// </summary>
/// <remarks>
/// <para>
/// Everything here is deliberate. In particular: only <c>;</c> starts a comment (<c>#</c> is a colour literal);
/// a NUL character is end-of-input; <c>#</c> immediately followed by end-of-input yields an EOF token rather than
/// a colour; numbers are scanned by a three-state machine whose terminator is always pushed back, so <c>1x</c>
/// becomes the number <c>1</c> followed by the identifier <c>x</c>; and there is a single pushback slot, which
/// cannot hold a NUL — exactly as in Godot.
/// </para>
/// </remarks>
internal sealed class Lexer
{
    private const int ReadingInt = 0;
    private const int ReadingDec = 1;
    private const int ReadingExp = 2;
    private const int ReadingDone = 3;

    private readonly string _text;
    private readonly DiagnosticBag _diagnostics;
    private int _position;
    private int _line = 1;
    private int _lineStart;
    private bool _eof;
    private char _saved;

    /// <summary>Initializes a lexer over <paramref name="text"/>.</summary>
    /// <param name="text">The decoded text to tokenize.</param>
    /// <param name="diagnostics">Where non-fatal problems are reported.</param>
    public Lexer(string text, DiagnosticBag diagnostics)
    {
        _text = text;
        _diagnostics = diagnostics;
    }

    /// <summary>Gets the current line, counting every newline in the text (deviation D9).</summary>
    public int Line => _line;

    /// <summary>Gets the position just past the last character consumed.</summary>
    public SourcePosition Position => new(_position, _line, _position - _lineStart + 1);

    /// <summary>Gets a value indicating whether the input is exhausted.</summary>
    public bool IsEof => _eof;

    /// <summary>
    /// Gets or sets Godot's single-character pushback slot. <c>'\0'</c> means "empty", which is why a NUL cannot
    /// be pushed back.
    /// </summary>
    public char Saved
    {
        get => _saved;
        set => _saved = value;
    }

    /// <summary>Reads the next character, or <c>'\0'</c> at end of input.</summary>
    /// <returns>The character.</returns>
    public char ReadChar()
    {
        if (_position < _text.Length)
        {
            return _text[_position++];
        }

        _eof = true;
        return '\0';
    }

    /// <summary>
    /// Records that a newline was consumed outside the tokenizer, which happens because the top-level reader and
    /// the section-header reader pull raw characters rather than tokens.
    /// </summary>
    public void CountNewline()
    {
        _line++;
        _lineStart = _position;
    }

    /// <summary>Reads the next token.</summary>
    /// <returns>The token. Errors are reported by throwing <see cref="ParseAbortException"/>.</returns>
    public Token GetToken()
    {
        // Assigned at the top of every round of the loop below, because a token can be preceded by skipped
        // whitespace, newlines or a comment.
        int startOffset;
        int startLine;
        int startColumn;

        SourcePosition Start() => new(startOffset, startLine, startColumn);

        SourcePosition Here() => new(_position, _line, _position - _lineStart + 1);

        SourcePosition At(int offset) => new(offset, _line, offset - _lineStart + 1);

        Token Make(TokenType type, Variant? value = null, int? end = null) =>
            new(type, value ?? Variant.Nil.Instance, new SourceSpan(Start(), At(end ?? _position)));

        void Fail(DiagnosticCode code, string message) =>
            _diagnostics.Fail(code, message, new SourceSpan(Start(), Here()));

        int ReadHexEscape(int length)
        {
            int code = 0;
            for (int i = 0; i < length; i++)
            {
                char c = ReadChar();
                if (c == '\0')
                {
                    Fail(DiagnosticCode.UnterminatedString, "Unterminated string");
                }

                if (!IsHexDigit(c))
                {
                    Fail(DiagnosticCode.MalformedHexEscape, "Malformed hex constant in string");
                }

                code = (code << 4) | HexValue(c);
            }

            return code;
        }

        bool stringName = false;

        while (true)
        {
            // The span starts at the first character that is not skipped, so it is recomputed every round: a
            // token can be preceded by newlines, comments or whitespace.
            bool fromSaved = _saved != '\0';
            startOffset = fromSaved ? _position - 1 : _position;
            startLine = _line;
            startColumn = startOffset - _lineStart + 1;

            char cchar;
            if (fromSaved)
            {
                cchar = _saved;
                _saved = '\0';
            }
            else
            {
                cchar = ReadChar();
                if (_eof)
                {
                    return Make(TokenType.Eof);
                }
            }

            switch (cchar)
            {
                case '\n':
                    _line++;
                    _lineStart = _position;
                    break;

                // A NUL ends the token stream, even in the middle of the text.
                case '\0':
                    return Make(TokenType.Eof);

                case '{':
                    return Make(TokenType.CurlyBracketOpen);
                case '}':
                    return Make(TokenType.CurlyBracketClose);
                case '[':
                    return Make(TokenType.BracketOpen);
                case ']':
                    return Make(TokenType.BracketClose);
                case '(':
                    return Make(TokenType.ParenthesisOpen);
                case ')':
                    return Make(TokenType.ParenthesisClose);
                case ':':
                    return Make(TokenType.Colon);
                case ',':
                    return Make(TokenType.Comma);
                case '.':
                    return Make(TokenType.Period);
                case '=':
                    return Make(TokenType.Equal);

                case ';':
                    while (true)
                    {
                        char ch = ReadChar();
                        if (_eof)
                        {
                            return Make(TokenType.Eof);
                        }

                        if (ch == '\n')
                        {
                            _line++;
                            _lineStart = _position;
                            break;
                        }
                    }

                    break;

                case '#':
                    {
                        // A colour literal, or end of input when the hash is the last character.
                        StringBuilder colorText = new("#");
                        char ch;
                        while (true)
                        {
                            ch = ReadChar();
                            if (_eof)
                            {
                                return Make(TokenType.Eof);
                            }

                            if (IsHexDigit(ch))
                            {
                                colorText.Append(ch);
                            }
                            else
                            {
                                _saved = ch;
                                break;
                            }
                        }

                        int end = ch == '\0' ? _position : _position - 1;
                        string literal = colorText.ToString();
                        if (ColorHtml.TryParse(literal, out Color color))
                        {
                            return Make(TokenType.Color, new Variant.Color(color), end);
                        }

                        _diagnostics.Warning(
                            DiagnosticCode.MalformedHexColor,
                            $"Invalid color code: {literal}",
                            new SourceSpan(Start(), At(end)));
                        return Make(TokenType.Color, new Variant.Color(ColorHtml.OpaqueBlack), end);
                    }

                case '@': // Deprecated 3.x spelling of a StringName; Godot still accepts it.
                case '&':
                    {
                        bool deprecated = cchar == '@';
                        cchar = ReadChar();
                        if (cchar != '"')
                        {
                            Fail(DiagnosticCode.MissingQuoteAfterAmpersand, "Expected '\"' after '&'");
                            return Make(TokenType.Eof);
                        }

                        if (deprecated)
                        {
                            _diagnostics.Info(
                                DiagnosticCode.DeprecatedAtStringName,
                                "The '@\"...\"' spelling of StringName is deprecated; use '&\"...\"'",
                                new SourceSpan(Start(), Here()));
                        }

                        stringName = true;
                        goto case '"';
                    }

                case '"':
                    {
                        StringBuilder text = new();
                        int prev = 0;

                        while (true)
                        {
                            char ch = ReadChar();

                            if (ch == '\0')
                            {
                                Fail(DiagnosticCode.UnterminatedString, "Unterminated string");
                                return Make(TokenType.Eof);
                            }

                            if (ch == '"')
                            {
                                break;
                            }

                            if (ch == '\\')
                            {
                                int next = ReadChar();
                                if (next == '\0')
                                {
                                    Fail(DiagnosticCode.UnterminatedString, "Unterminated string");
                                    return Make(TokenType.Eof);
                                }

                                int code = next switch
                                {
                                    'b' => 8,
                                    't' => 9,
                                    'n' => 10,
                                    'f' => 12,
                                    'r' => 13,
                                    'U' => ReadHexEscape(6),
                                    'u' => ReadHexEscape(4),
                                    _ => next,
                                };

                                if ((code & 0xFFFFFC00) == 0xD800)
                                {
                                    if (prev == 0)
                                    {
                                        prev = code;
                                        continue;
                                    }

                                    Fail(DiagnosticCode.UnpairedSurrogate, "Invalid UTF-16 sequence in string, unpaired lead surrogate");
                                }
                                else if ((code & 0xFFFFFC00) == 0xDC00)
                                {
                                    if (prev == 0)
                                    {
                                        Fail(DiagnosticCode.UnpairedSurrogate, "Invalid UTF-16 sequence in string, unpaired trail surrogate");
                                    }
                                    else
                                    {
                                        code = (prev << 10) + code - ((0xD800 << 10) + 0xDC00 - 0x10000);
                                        prev = 0;
                                    }
                                }

                                if (prev != 0)
                                {
                                    Fail(DiagnosticCode.UnpairedSurrogate, "Invalid UTF-16 sequence in string, unpaired lead surrogate");
                                }

                                AppendCodePoint(text, code);
                            }
                            else
                            {
                                if (prev != 0)
                                {
                                    Fail(DiagnosticCode.UnpairedSurrogate, "Invalid UTF-16 sequence in string, unpaired lead surrogate");
                                }

                                if (ch == '\n')
                                {
                                    _line++;
                                    _lineStart = _position;
                                }

                                // The string is not re-decoded afterwards: this library decodes UTF-8 once, up
                                // front (deviations D3 and D4), which matches Godot's parse(String) entry point.
                                text.Append(ch);
                            }
                        }

                        if (prev != 0)
                        {
                            Fail(DiagnosticCode.UnpairedSurrogate, "Invalid UTF-16 sequence in string, unpaired lead surrogate");
                        }

                        string value = text.ToString();
                        return stringName
                            ? Make(TokenType.StringName, new Variant.StringName(value))
                            : Make(TokenType.String, new Variant.Str(value));
                    }

                default:
                    {
                        if (cchar <= 32)
                        {
                            break;
                        }

                        StringBuilder tokenText = new();
                        if (cchar == '-')
                        {
                            tokenText.Append('-');
                            cchar = ReadChar();
                        }

                        if (IsDigit(cchar))
                        {
                            return Make(TokenType.Number, ReadNumber(tokenText, cchar, Start()));
                        }

                        if (IsAsciiAlphabet(cchar) || cchar == '_')
                        {
                            bool first = true;
                            while (IsAsciiAlphabet(cchar) || cchar == '_' || (!first && IsDigit(cchar)))
                            {
                                tokenText.Append(cchar);
                                cchar = ReadChar();
                                first = false;
                            }

                            _saved = cchar;
                            return Make(
                                TokenType.Identifier,
                                new Variant.Str(tokenText.ToString()),
                                cchar == '\0' ? _position : _position - 1);
                        }

                        Fail(DiagnosticCode.UnexpectedCharacter, "Unexpected character");
                        break;
                    }
            }
        }
    }

    /// <summary>
    /// Scans a number literal with Godot's three-state machine. The character that ends the number is always
    /// pushed back, which is what makes <c>1x</c> two tokens and <c>5-3</c> the numbers 5 and -3.
    /// </summary>
    private Variant ReadNumber(StringBuilder tokenText, char first, SourcePosition start)
    {
        int reading = ReadingInt;
        char c = first;
        bool expSign = false;
        bool expBeg = false;
        bool isFloat = false;

        while (true)
        {
            switch (reading)
            {
                case ReadingInt:
                    if (IsDigit(c))
                    {
                        // Keep reading digits.
                    }
                    else if (c == '.')
                    {
                        reading = ReadingDec;
                        isFloat = true;
                    }
                    else if (c is 'e' or 'E')
                    {
                        reading = ReadingExp;
                        isFloat = true;
                    }
                    else
                    {
                        reading = ReadingDone;
                    }

                    break;

                case ReadingDec:
                    if (IsDigit(c))
                    {
                        // Keep reading digits.
                    }
                    else if (c is 'e' or 'E')
                    {
                        reading = ReadingExp;
                    }
                    else
                    {
                        reading = ReadingDone;
                    }

                    break;

                case ReadingExp:
                    if (IsDigit(c))
                    {
                        expBeg = true;
                    }
                    else if ((c is '-' or '+') && !expSign && !expBeg)
                    {
                        expSign = true;
                    }
                    else
                    {
                        reading = ReadingDone;
                    }

                    break;

                default:
                    reading = ReadingDone;
                    break;
            }

            if (reading == ReadingDone)
            {
                break;
            }

            tokenText.Append(c);
            c = ReadChar();
        }

        _saved = c;

        int end = c == '\0' ? _position : _position - 1;
        SourceSpan span = new(start, new SourcePosition(end, _line, end - _lineStart + 1));
        string text = tokenText.ToString();

        return isFloat
            ? new Variant.Float(NumberParser.ParseFloat(text))
            : new Variant.Int(NumberParser.ParseInt(text, _diagnostics, span));
    }

    private static void AppendCodePoint(StringBuilder builder, int codePoint)
    {
        if (codePoint <= 0xFFFF)
        {
            builder.Append((char)codePoint);
        }
        else
        {
            builder.Append(char.ConvertFromUtf32(codePoint));
        }
    }

    private static bool IsDigit(char c) => c is >= '0' and <= '9';

    private static bool IsHexDigit(char c) =>
        c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');

    private static bool IsAsciiAlphabet(char c) => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z');

    private static int HexValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        _ => c - 'A' + 10,
    };
}
