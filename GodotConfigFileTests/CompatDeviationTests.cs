namespace GodotConfigFileTests;

/// <summary>
/// One test per deliberate deviation, so that every entry in <c>docs/compat-deviations.md</c> is pinned down.
/// </summary>
public class CompatDeviationTests
{
    [Fact]
    public void D1_ByteOrderMarkIsStripped()
    {
        ParseResult result = ConfigFile.ParseWithResult("\uFEFF[a]\nk=1\n");

        Assert.True(result.Success);
        Assert.Equal(["a"], result.Document.SectionNames);
        Assert.Equal(DiagnosticCode.BomStripped, Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void D2_UnquotedNonAsciiKeyIsDecoded()
    {
        // Godot's load() path keeps the raw code points, so this key comes back as mojibake there.
        ConfigFileDocument document = ConfigFile.Parse("静音=1\n");

        Assert.Equal(["静音"], document.GetSection("")!.KeyNames);
        Assert.Equal(new Variant.Int(1), Fixture.Require(document, "", "静音"));
    }

    [Fact]
    public void D3_UnicodeEscapesAreNotDoubleDecoded()
    {
        Assert.Equal(new Variant.Str("é"), Fixture.Require(ConfigFile.Parse("k=\"\\u00e9\"\n"), "", "k"));
        Assert.Equal(new Variant.Str("中"), Fixture.Require(ConfigFile.Parse("k=\"\\u4e2d\"\n"), "", "k"));
    }

    [Fact]
    public void D4_InputIsDecodedOnce()
    {
        // The byte entry point produces exactly what the string entry point produces.
        ConfigFileDocument fromBytes = ConfigFile.ParseUtf8("[a]\nk=\"é中\"\n"u8);
        ConfigFileDocument fromText = ConfigFile.Parse("[a]\nk=\"é中\"\n");

        Assert.Equal(Fixture.Require(fromText, "a", "k"), Fixture.Require(fromBytes, "a", "k"));
        Assert.Empty(ConfigFile.ParseWithResult("[a]\nk=\"é中\"\n").Diagnostics);
    }

    [Fact]
    public void D5_NestingBeyondTheDepthLimitIsAnError()
    {
        string tooDeep = "k=" + new string('[', 101) + new string(']', 101) + "\n";

        ConfigFileParseException error = Assert.Throws<ConfigFileParseException>(() => ConfigFile.Parse(tooDeep));
        Assert.Equal(DiagnosticCode.DepthLimitExceeded, error.Diagnostic.Code);
    }

    [Fact]
    public void D6_IntegerOverflowSaturatesCorrectly()
    {
        // Godot's own check misses the 19-digit case and yields long.MinValue.
        ParseResult result = ConfigFile.ParseWithResult("k=9223372036854775808\n");

        Assert.Equal(new Variant.Int(long.MaxValue), Fixture.Require(result.Document, "", "k"));
        Assert.Equal(DiagnosticCode.IntegerOverflow, Assert.Single(result.Diagnostics).Code);
        Assert.Equal(DiagnosticSeverity.Warning, result.Diagnostics[0].Severity);
    }

    [Fact]
    public void D7_ObjectsAndResourcesAreOpaqueAndNeverFail()
    {
        // Godot instantiates through ClassDB and loads through ResourceLoader; both can fail the whole parse.
        ParseResult result = ConfigFile.ParseWithResult(
            "a=Object(NoSuchClass, \"p\": 1)\nb=ExtResource(\"1_missing\")\n");

        Assert.True(result.Success);
        Assert.Equal("NoSuchClass", ((Variant.Object)Fixture.Require(result.Document, "", "a")).ClassName);
        Assert.Equal(new Variant.Resource("ExtResource", null, "1_missing"), Fixture.Require(result.Document, "", "b"));
    }

    [Fact]
    public void D8_TypedContainerAsymmetryIsNotCopied()
    {
        // Godot leaves the unknown side as NIL but still marks the dictionary typed; here both sides are just null.
        Variant.Dictionary dictionary = (Variant.Dictionary)Fixture.Require(
            ConfigFile.Parse("k=Dictionary[Whatever, int]({})\n"),
            "",
            "k");

        Assert.Null(dictionary.KeyType);
        Assert.Equal(VariantType.Int, dictionary.ValueType);
        Assert.Contains(
            ConfigFile.ParseWithResult("k=Dictionary[Whatever, int]({})\n").Diagnostics,
            diagnostic => diagnostic.Code == DiagnosticCode.UnknownTypeName);
    }

    [Fact]
    public void D9_LinesFollowTheRealText()
    {
        // The newline inside the section header counts, which Godot's own line counter misses.
        ConfigFileParseException error = Assert.Throws<ConfigFileParseException>(() => ConfigFile.Parse("[a\nb]\nk=foo\n"));

        Assert.Equal(3, error.Span.Start.Line);
        Assert.Equal(3, error.Diagnostic.Span.Start.Line);
    }

    [Fact]
    public void D10_MalformedColourIsBlackWithAWarning()
    {
        ParseResult result = ConfigFile.ParseWithResult("k=#12345\n");

        Assert.True(result.Success);
        Assert.Equal(new Variant.Color(new Color(0, 0, 0, 1)), Fixture.Require(result.Document, "", "k"));
        Assert.Equal(DiagnosticCode.MalformedHexColor, Assert.Single(result.Diagnostics).Code);
        Assert.Equal(DiagnosticSeverity.Warning, result.Diagnostics[0].Severity);
    }

    [Fact]
    public void D11_CompositeKeysHashAndCompareConsistently()
    {
        // Godot hashes dictionaries in insertion order but compares them without order, so these two keys do not
        // collide there. Here hash and equality agree, so the second assignment overwrites the first.
        Variant.Dictionary outer = (Variant.Dictionary)Fixture.Require(
            ConfigFile.Parse("k={{1: 2, 3: 4}: \"a\", {3: 4, 1: 2}: \"b\"}\n"),
            "",
            "k");

        Assert.Single(outer.Items);
        Assert.Equal(new Variant.Str("b"), outer.Items.Values[0]);
    }

    [Fact]
    public void D12_NaNEqualsNaN()
    {
        ConfigFileDocument document = ConfigFile.Parse("a=[nan]\nb=[nan]\n");

        Assert.Equal(Fixture.Require(document, "", "a"), Fixture.Require(document, "", "b"));
        Assert.Equal(Fixture.Require(document, "", "a").GetHashCode(), Fixture.Require(document, "", "b").GetHashCode());
    }

    [Fact]
    public void StringIsNeverImplicitlyConvertedToNumber()
    {
        ConfigFileDocument document = ConfigFile.Parse("s=\"5\"\nn=5\n");

        Assert.False(document.TryGetValue<int>("", "s", out int _));
        Assert.Equal(0, document.GetValue("", "s", 0));
        Assert.Equal(5, document.GetValue("", "n", 0));
    }
}
