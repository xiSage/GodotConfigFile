using System.Text;

namespace GodotConfigFile;

/// <summary>
/// The top-level reader, a port of <c>ConfigFile::_parse</c> over
/// <c>VariantParser::parse_tag_assign_eof(..., p_simple_tag: true)</c>.
/// </summary>
/// <remarks>
/// The structure is the point: newlines are <em>not</em> statement terminators. A key name accumulates every
/// character above 32 until the first <c>=</c>, so whitespace inside a key disappears
/// (<c>my key=1</c> is the key <c>mykey</c>) and a key runs across newlines and <c>;</c> comments. Only when
/// nothing has been accumulated yet does a <c>[</c> start a section header. Whatever follows a value is the
/// beginning of the next key name.
/// </remarks>
internal sealed partial class Parser
{
    private readonly Lexer _lexer;
    private readonly DiagnosticBag _diagnostics;
    private readonly ParseOptions _options;
    private readonly ConfigFileDocument _document = new();
    private string _currentSection = ConfigFile.RootSection;
    private int _depth;

    /// <summary>Initializes a parser.</summary>
    /// <param name="text">The decoded text to parse.</param>
    /// <param name="diagnostics">Where diagnostics are collected.</param>
    /// <param name="options">The parsing options.</param>
    public Parser(string text, DiagnosticBag diagnostics, ParseOptions options)
    {
        _lexer = new Lexer(text, diagnostics);
        _diagnostics = diagnostics;
        _options = options;
    }

    /// <summary>Gets the document built so far. Valid even after a failed parse.</summary>
    public ConfigFileDocument Document => _document;

    /// <summary>Parses the whole input.</summary>
    /// <returns>The parsed document.</returns>
    public ConfigFileDocument ParseDocument()
    {
        while (true)
        {
            StepKind kind = ParseTagAssignEof(out string tagName, out string key, out Variant value);

            if (kind == StepKind.Eof)
            {
                return _document;
            }

            if (key.Length != 0)
            {
                ApplyAssign(key, value);
            }
            else if (tagName.Length != 0)
            {
                // A section header keeps its literal text; the '\]' escape is undone here, as in ConfigFile.
                _currentSection = tagName.Replace("\\]", "]", StringComparison.Ordinal);
            }
        }
    }

    private enum StepKind
    {
        Eof,
        Assign,
        Tag,
    }

    /// <summary>
    /// Reads one step: either an assignment, a section header, or end of input.
    /// </summary>
    /// <param name="tagName">The section name, when a header was read. Godot calls this <c>r_tag.name</c>.</param>
    /// <param name="key">The key name, when an assignment was read. Godot calls this <c>r_assign</c>.</param>
    /// <param name="value">The assigned value.</param>
    /// <returns>Which of the three happened.</returns>
    private StepKind ParseTagAssignEof(out string tagName, out string key, out Variant value)
    {
        tagName = string.Empty;
        key = string.Empty;
        value = Variant.Nil.Instance;
        StringBuilder keyName = new();

        while (true)
        {
            char c;
            if (_lexer.Saved != '\0')
            {
                c = _lexer.Saved;
                _lexer.Saved = '\0';
            }
            else
            {
                c = _lexer.ReadChar();
            }

            if (_lexer.IsEof)
            {
                // A dangling key name at end of input is not an error; Godot returns ERR_FILE_EOF here.
                return StepKind.Eof;
            }

            if (c == ';')
            {
                while (true)
                {
                    char ch = _lexer.ReadChar();
                    if (_lexer.IsEof)
                    {
                        return StepKind.Eof;
                    }

                    if (ch == '\n')
                    {
                        _lexer.CountNewline();
                        break;
                    }
                }

                // A comment does not end the key name: `a;comment\nb=1` defines the key `ab`.
                continue;
            }

            if (c == '[' && keyName.Length == 0)
            {
                _lexer.Saved = '[';
                tagName = ParseSectionHeader();
                return StepKind.Tag;
            }

            if (c > 32)
            {
                if (c == '"')
                {
                    // A quoted key name replaces what has been accumulated so far rather than appending to it.
                    _lexer.Saved = '"';
                    Token token = _lexer.GetToken();
                    if (token.Type != TokenType.String)
                    {
                        Fail(DiagnosticCode.ExpectedToken, "Error reading quoted string", token.Span);
                    }

                    keyName.Clear();
                    keyName.Append(IdentifierText(token));
                }
                else if (c != '=')
                {
                    keyName.Append(c);
                }
                else
                {
                    key = keyName.ToString();
                    Token token = _lexer.GetToken();
                    value = ParseValue(token);
                    return StepKind.Assign;
                }
            }
            else if (c == '\n')
            {
                _lexer.CountNewline();
            }
        }
    }

    /// <summary>Reads a <c>[section]</c> header. The <c>[</c> is pushed back and re-read as a token.</summary>
    private string ParseSectionHeader()
    {
        Token token = _lexer.GetToken();
        if (token.Type != TokenType.BracketOpen)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected '['", token.Span);
        }

        SourcePosition start = token.Span.Start;
        StringBuilder name = new();
        bool escaping = false;

        while (true)
        {
            char c = _lexer.ReadChar();
            if (_lexer.IsEof)
            {
                Fail(DiagnosticCode.UnexpectedEof, "Unexpected EOF while parsing simple tag", new SourceSpan(start, _lexer.Position));
            }

            if (c == ']')
            {
                if (escaping)
                {
                    escaping = false;
                }
                else
                {
                    break;
                }
            }
            else if (c == '\\')
            {
                escaping = true;
            }
            else
            {
                escaping = false;
            }

            if (c == '\n')
            {
                // Godot does not count newlines inside a section header, which makes its error lines wrong;
                // this library counts every newline (deviation D9).
                _lexer.CountNewline();
            }

            name.Append(c);
        }

        return name.ToString().Trim();
    }

    private void ApplyAssign(string key, Variant value)
    {
        if (value.Kind == VariantType.Nil && !_options.PreserveNilKeys)
        {
            // Godot: assigning NIL erases the key, and a section that becomes empty is erased with it.
            ConfigSection? existing = _document.GetSection(_currentSection);
            if (existing is null)
            {
                return;
            }

            if (existing.Remove(key) && existing.Count == 0)
            {
                _document.RemoveSection(_currentSection);
            }

            return;
        }

        ConfigSection section = _document.GetOrAddSection(_currentSection);
        if (section.Set(key, value))
        {
            _diagnostics.Info(
                DiagnosticCode.DuplicateKey,
                $"Key '{key}' was assigned more than once in section '{_currentSection}'; the last assignment wins",
                SourceSpan.At(_lexer.Position));
        }
    }

    private void Fail(DiagnosticCode code, string message, SourceSpan span) =>
        _diagnostics.Fail(code, message, span);

    private SourceSpan CurrentSpan() => SourceSpan.At(_lexer.Position);

    private static string IdentifierText(Token token) => ((Variant.Str)token.Value).Value;

    private static double NumberValue(Token token) => token.Value switch
    {
        Variant.Int i => i.Value,
        Variant.Float f => f.Value,
        _ => 0,
    };
}
