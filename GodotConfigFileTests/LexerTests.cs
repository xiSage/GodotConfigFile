namespace GodotConfigFileTests;

/// <summary>
/// The tokenizer, where most of Godot's quirks live.
/// </summary>
public class LexerTests
{
    [Fact]
    public void ReadsPunctuationAndScalars()
    {
        List<Token> tokens = Fixture.Lex("a = 1");

        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal("a", ((Variant.Str)tokens[0].Value).Value);
        Assert.Equal(TokenType.Equal, tokens[1].Type);
        Assert.Equal(TokenType.Number, tokens[2].Type);
        Assert.Equal(new Variant.Int(1), tokens[2].Value);
    }

    [Fact]
    public void BracesBracketsAndOperators()
    {
        Assert.Equal(
            "CurlyBracketOpen CurlyBracketClose BracketOpen BracketClose ParenthesisOpen ParenthesisClose Colon Comma Period Equal",
            Shape("{ } [ ] ( ) : , . ="));
    }

    [Fact]
    public void NewlinesAndSpacesAreNotTokens()
    {
        Assert.Empty(Fixture.Lex(" \t\r\n \v\f "));
    }

    [Fact]
    public void OnlySemicolonStartsAComment()
    {
        // A '#' is a colour literal, never a comment.
        List<Token> tokens = Fixture.Lex("; comment\nx");
        Assert.Single(tokens);
        Assert.Equal(TokenType.Identifier, tokens[0].Type);
        Assert.Equal(2, tokens[0].Span.Start.Line);
    }

    [Fact]
    public void CommentAtEndOfInputEndsTheStream()
    {
        Assert.Empty(Fixture.Lex("; trailing comment"));
    }

    [Theory]
    [InlineData("#ff0000 ", 1f, 0f, 0f, 1f)]
    [InlineData("#f00 ", 1f, 0f, 0f, 1f)]
    [InlineData("#ff000080 ", 1f, 0f, 0f, 128f / 255f)]
    [InlineData("#0f08 ", 0f, 1f, 0f, 8f / 15f)]
    public void ReadsColourLiterals(string text, float r, float g, float b, float a)
    {
        List<Token> tokens = Fixture.Lex(text, out IReadOnlyList<Diagnostic> diagnostics);

        Assert.Empty(diagnostics);
        Token token = Assert.Single(tokens);
        Assert.Equal(TokenType.Color, token.Type);
        Color color = ((Variant.Color)token.Value).Value;
        Assert.Equal(r, color.R);
        Assert.Equal(g, color.G);
        Assert.Equal(b, color.B);
        Assert.Equal(a, color.A);
    }

    [Fact]
    public void ColourThatEndsTheInputIsSwallowed()
    {
        // Godot's colour scan treats running out of input as end-of-tokens: the literal is lost entirely.
        // A file Godot wrote always ends with a newline, so this only bites hand-edited files.
        Assert.Empty(Fixture.Lex("#ff0000"));
    }

    [Fact]
    public void MalformedColourIsOpaqueBlackWithAWarning()
    {
        // Deviation D10: the same colour Godot produces, plus a diagnostic.
        List<Token> tokens = Fixture.Lex("#12345 ", out IReadOnlyList<Diagnostic> diagnostics);

        Assert.Equal(TokenType.Color, tokens[0].Type);
        Assert.Equal(new Color(0, 0, 0, 1), ((Variant.Color)tokens[0].Value).Value);
        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticCode.MalformedHexColor, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public void ColourTerminatorIsPushedBack()
    {
        Assert.Equal("Color Comma Identifier", Shape("#fff,x"));
    }

    [Fact]
    public void HashAtEndOfInputIsEndOfInput()
    {
        Assert.Empty(Fixture.Lex("#"));
    }

    [Fact]
    public void ReadsStringNames()
    {
        List<Token> tokens = Fixture.Lex("&\"name\"");

        Assert.Equal(TokenType.StringName, Assert.Single(tokens).Type);
        Assert.Equal(new Variant.StringName("name"), tokens[0].Value);
    }

    [Fact]
    public void DeprecatedAtStringNameIsAcceptedWithAnInfo()
    {
        List<Token> tokens = Fixture.Lex("@\"name\"", out IReadOnlyList<Diagnostic> diagnostics);

        Assert.Equal(TokenType.StringName, Assert.Single(tokens).Type);
        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticCode.DeprecatedAtStringName, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
    }

    [Fact]
    public void AmpersandMustBeFollowedByAQuote()
    {
        Assert.Throws<ParseAbortException>(() => Fixture.Lex("&x"));
    }

    [Theory]
    [InlineData("1x", "Number Identifier")]
    [InlineData("5-3", "Number Number")]
    [InlineData("1.2.3", "Number Period Number")]
    [InlineData("1..2", "Number Period Number")]
    public void NumberScanningSplitsTheWayGodotDoes(string text, string expected)
    {
        Assert.Equal(expected, Shape(text));
    }

    [Fact]
    public void NumberScanningProducesTheExpectedValues()
    {
        Assert.Equal(new Variant.Int(5), Fixture.Lex("5-3")[0].Value);
        Assert.Equal(new Variant.Int(-3), Fixture.Lex("5-3")[1].Value);
        Assert.Equal(new Variant.Float(1.2), Fixture.Lex("1.2.3")[0].Value);
        Assert.Equal(new Variant.Int(3), Fixture.Lex("1.2.3")[2].Value);
    }

    [Theory]
    [InlineData("1e", 1.0)]
    [InlineData("1e+", 1.0)]
    [InlineData("1E-", 1.0)]
    [InlineData("1.", 1.0)]
    [InlineData("1e3", 1000.0)]
    [InlineData("1e+3", 1000.0)]
    [InlineData("1.5e-2", 0.015)]
    [InlineData("-0.5", -0.5)]
    public void ReadsFloatForms(string text, double expected)
    {
        Token token = Assert.Single(Fixture.Lex(text));
        Assert.Equal(TokenType.Number, token.Type);
        Assert.Equal(new Variant.Float(expected), token.Value);
    }

    [Fact]
    public void HugeExponentsBecomeInfinityAndZero()
    {
        Assert.Equal(new Variant.Float(double.PositiveInfinity), Assert.Single(Fixture.Lex("1e999")).Value);
        Assert.Equal(new Variant.Float(0.0), Assert.Single(Fixture.Lex("1e-999")).Value);
    }

    [Theory]
    [InlineData("+1")]
    [InlineData("-.5")]
    [InlineData("--5")]
    [InlineData("- 5")]
    public void RejectsTheFormsGodotRejects(string text)
    {
        ParseAbortException error = Assert.Throws<ParseAbortException>(() => Fixture.Lex(text));
        Assert.Equal(DiagnosticCode.UnexpectedCharacter, error.Diagnostic.Code);
    }

    [Fact]
    public void LeadingPeriodIsNotANumber()
    {
        // ".5" is a period followed by 5; the grammar then rejects it where a value was expected.
        Assert.Equal("Period Number", Shape(".5"));
    }

    [Fact]
    public void MinusInfinityIsAnIdentifier()
    {
        Token token = Assert.Single(Fixture.Lex("-inf"));
        Assert.Equal(TokenType.Identifier, token.Type);
        Assert.Equal("-inf", ((Variant.Str)token.Value).Value);
    }

    [Fact]
    public void IdentifiersMayNotStartWithADigit()
    {
        Assert.Equal("Number Identifier", Shape("1abc"));
    }

    [Fact]
    public void NoHexOrBinaryOrDigitSeparators()
    {
        // 0x10 is the number 0 followed by the identifier x10, which then glues onto the next key name.
        Assert.Equal("Number Identifier", Shape("0x10"));
    }

    [Fact]
    public void IntegerOverflowSaturatesAndWarns()
    {
        List<Token> tokens = Fixture.Lex("99999999999999999999", out IReadOnlyList<Diagnostic> diagnostics);

        Assert.Equal(new Variant.Int(long.MaxValue), Assert.Single(tokens).Value);
        Assert.Equal(DiagnosticCode.IntegerOverflow, Assert.Single(diagnostics).Code);
    }

    [Fact]
    public void NineteenDigitOverflowIsFixedToTheRightBound()
    {
        // Deviation D6: Godot silently yields long.MinValue here.
        List<Token> tokens = Fixture.Lex("9223372036854775808", out IReadOnlyList<Diagnostic> diagnostics);

        Assert.Equal(new Variant.Int(long.MaxValue), Assert.Single(tokens).Value);
        Assert.Equal(DiagnosticCode.IntegerOverflow, Assert.Single(diagnostics).Code);
    }

    [Fact]
    public void MinimumIntegerStillWorks()
    {
        List<Token> tokens = Fixture.Lex("-9223372036854775808", out IReadOnlyList<Diagnostic> diagnostics);

        Assert.Equal(new Variant.Int(long.MinValue), Assert.Single(tokens).Value);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void StringEscapes()
    {
        Assert.Equal(new Variant.Str("a\tb"), Assert.Single(Fixture.Lex("\"a\\tb\"")).Value);
        Assert.Equal(new Variant.Str("a\bb\fc\rd"), Assert.Single(Fixture.Lex("\"a\\bb\\fc\\rd\"")).Value);
        Assert.Equal(new Variant.Str("quote\"and\\slash"), Assert.Single(Fixture.Lex("\"quote\\\"and\\\\slash\"")).Value);
        Assert.Equal(new Variant.Str("p"), Assert.Single(Fixture.Lex("\"\\p\"")).Value);
        Assert.Equal(new Variant.Str("'"), Assert.Single(Fixture.Lex("\"\\'\"")).Value);
    }

    [Fact]
    public void StringsMaySpanLinesAndKeepTheNewline()
    {
        Token token = Assert.Single(Fixture.Lex("\"Waiting\nfor\nGodot\""));

        Assert.Equal(new Variant.Str("Waiting\nfor\nGodot"), token.Value);
        Assert.Equal(1, token.Span.Start.Line);
        Assert.Equal(3, token.Span.End.Line);
    }

    [Fact]
    public void StringsKeepCarriageReturnsVerbatim()
    {
        Assert.Equal(new Variant.Str("a\r\nb"), Assert.Single(Fixture.Lex("\"a\r\nb\"")).Value);
    }

    [Fact]
    public void UnicodeEscapesAreDecodedOnce()
    {
        // Deviation D3: only one UTF-8 decode happens, so these are not mangled.
        Assert.Equal(new Variant.Str("A"), Assert.Single(Fixture.Lex("\"\\u0041\"")).Value);
        Assert.Equal(new Variant.Str("é"), Assert.Single(Fixture.Lex("\"\\u00e9\"")).Value);
        Assert.Equal(new Variant.Str("中"), Assert.Single(Fixture.Lex("\"\\u4e2d\"")).Value);
        Assert.Equal(new Variant.Str("😀"), Assert.Single(Fixture.Lex("\"\\U01F600\"")).Value);
    }

    [Fact]
    public void SurrogatePairsAreCombined()
    {
        Assert.Equal(new Variant.Str("😀"), Assert.Single(Fixture.Lex("\"\\ud83d\\ude00\"")).Value);
    }

    [Fact]
    public void UnpairedSurrogateIsAnError()
    {
        ParseAbortException error = Assert.Throws<ParseAbortException>(() => Fixture.Lex("\"\\ud83dx\""));
        Assert.Equal(DiagnosticCode.UnpairedSurrogate, error.Diagnostic.Code);
    }

    [Fact]
    public void UnterminatedStringIsAnError()
    {
        ParseAbortException error = Assert.Throws<ParseAbortException>(() => Fixture.Lex("\"abc"));
        Assert.Equal(DiagnosticCode.UnterminatedString, error.Diagnostic.Code);
        Assert.Equal("Unterminated string", error.Diagnostic.Message);
    }

    [Fact]
    public void MalformedHexEscapeIsAnError()
    {
        ParseAbortException error = Assert.Throws<ParseAbortException>(() => Fixture.Lex("\"\\u00zz\""));
        Assert.Equal(DiagnosticCode.MalformedHexEscape, error.Diagnostic.Code);
    }

    [Fact]
    public void NulTerminatingATokenIsSwallowed()
    {
        // The pushback slot uses 0 to mean "empty", so a NUL that ends a token disappears.
        Assert.Equal("Identifier Identifier", Shape("a\0b"));
    }

    [Fact]
    public void NulInTheTokenLoopEndsTheStream()
    {
        Assert.Equal("Identifier", Shape("a \0b"));
    }

    [Fact]
    public void LinesAreCountedOnEveryNewline()
    {
        List<Token> tokens = Fixture.Lex("a\n\nb");
        Assert.Equal(1, tokens[0].Span.Start.Line);
        Assert.Equal(3, tokens[1].Span.Start.Line);
        Assert.Equal(1, tokens[1].Span.Start.Column);
    }

    [Fact]
    public void CarriageReturnsDoNotCountAsLines()
    {
        List<Token> tokens = Fixture.Lex("a\r\nb");
        Assert.Equal(2, tokens[1].Span.Start.Line);
        Assert.Equal(1, tokens[1].Span.Start.Column);
    }

    [Fact]
    public void SpansCoverTheToken()
    {
        Token token = Assert.Single(Fixture.Lex("abc"));
        Assert.Equal(0, token.Span.Start.Offset);
        Assert.Equal(3, token.Span.End.Offset);

        List<Token> tokens = Fixture.Lex("x = 42");
        Assert.Equal(2, tokens[1].Span.Start.Offset);
        Assert.Equal(3, tokens[1].Span.End.Offset);
        Assert.Equal(4, tokens[2].Span.Start.Offset);
        Assert.Equal(6, tokens[2].Span.End.Offset);
        Assert.Equal(5, tokens[2].Span.Start.Column);
    }

    private static string Shape(string text) =>
        string.Join(" ", Fixture.Lex(text).Select(token => token.Type.ToString()));
}
