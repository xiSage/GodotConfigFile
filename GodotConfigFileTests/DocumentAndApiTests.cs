namespace GodotConfigFileTests;

/// <summary>
/// The document model, null handling, merging, entry points and the conversion whitelist.
/// </summary>
public class DocumentAndApiTests
{
    private const string Sample =
        "s=\"x\"\n" +
        "num=\"5\"\n" +
        "i=5\n" +
        "big=9223372036854775807\n" +
        "f=1.5\n" +
        "b=true\n" +
        "a=[1, 2]\n" +
        "pa=PackedStringArray(\"a\", \"b\")\n" +
        "pb=PackedByteArray(\"AQID\")\n" +
        "pi=PackedInt32Array(1, 2)\n" +
        "nt=NodePath(\"a/b\")\n" +
        "sn=&\"n\"\n" +
        "c=Color(1, 0, 0, 1)\n" +
        "v2=Vector2(1, 2)\n" +
        "d={\"x\": 1}\n";

    [Fact]
    public void ConvertsScalars()
    {
        ConfigFileDocument document = ConfigFile.Parse(Sample);

        Assert.Equal("x", document.GetValue<string>("", "s"));
        Assert.Equal(5, document.GetValue<int>("", "i"));
        Assert.Equal(5L, document.GetValue<long>("", "i"));
        Assert.Equal(5.0, document.GetValue<double>("", "i"));
        Assert.Equal(5f, document.GetValue<float>("", "i"));
        Assert.Equal(1.5, document.GetValue<double>("", "f"));
        Assert.Equal(1.5f, document.GetValue<float>("", "f"));
        Assert.True(document.GetValue<bool>("", "b"));
        Assert.Equal("a/b", document.GetValue<string>("", "nt"));
        Assert.Equal("n", document.GetValue<string>("", "sn"));
        Assert.Equal(new Color(1, 0, 0, 1), document.GetValue<Color>("", "c"));
        Assert.Equal(new Vector2(1, 2), document.GetValue<Vector2>("", "v2"));
        Assert.Equal(new Variant.Int(5), document.GetValue<Variant>("", "i"));
    }

    [Fact]
    public void ConvertsCollections()
    {
        ConfigFileDocument document = ConfigFile.Parse(Sample);

        Assert.Equal<string[]>(["a", "b"], document.GetValue<string[]>("", "pa"));
        Assert.Equal<byte[]>([1, 2, 3], document.GetValue<byte[]>("", "pb"));
        Assert.Equal<int[]>([1, 2], document.GetValue<int[]>("", "pi"));
        Assert.Equal<int[]>([1, 2], document.GetValue<int[]>("", "a"));
        Assert.Equal<long[]>([1L, 2L], document.GetValue<long[]>("", "a"));
        Assert.Equal(2, document.GetValue<Variant[]>("", "a").Length);
        Assert.Equal(new Variant.Int(1), document.GetValue<VariantDictionary>("", "d")[new Variant.Str("x")]);
    }

    [Fact]
    public void UnsuitableConversionsReportFailureInsteadOfGuessing()
    {
        ConfigFileDocument document = ConfigFile.Parse(Sample);

        // A quoted number is a string, not a number: no implicit string-to-number conversion.
        Assert.False(document.TryGetValue<int>("", "num", out int _));
        Assert.Equal(0, document.GetValue("", "num", 0));

        // The wrong value kind, an out-of-range integer and a missing key all fall back to the default.
        Assert.Equal("fallback", document.GetValue("", "i", "fallback"));
        Assert.Equal(0, document.GetValue("", "big", 0));
        Assert.Equal("fallback", document.GetValue("", "missing", "fallback"));
        Assert.Equal(7, document.GetValue("", "missing", 7));

        Assert.False(document.TryGetValue<string[]>("", "a", out string[]? _));
        Assert.False(document.TryGetValue<Vector2>("", "c", out Vector2 _));
    }

    [Fact]
    public void MissingSectionsAndKeysAreReported()
    {
        ConfigFileDocument document = ConfigFile.Parse("a=1\n");

        Assert.False(document.TryGetVariant("", "b", out Variant missing));
        Assert.Same(Variant.Nil.Instance, missing);
        Assert.False(document.TryGetVariant("nope", "b", out _));
        Assert.Null(document.GetSection("nope"));
        Assert.False(document.HasSection("nope"));
        Assert.Null(document.Root!["nope"]);
        Assert.False(document.Root!.ContainsKey("nope"));
        Assert.True(document.Root!.ContainsKey("a"));
    }

    [Fact]
    public void EmptyDocumentHasNoSections()
    {
        Assert.Empty(ConfigFileDocument.Empty.SectionNames);
        Assert.Null(ConfigFileDocument.Empty.Root);
    }

    [Fact]
    public void NullErasesTheKeyAndTheSection()
    {
        Assert.Empty(ConfigFile.Parse("k=null\n").SectionNames);
        Assert.Empty(ConfigFile.Parse("k=nil\n").SectionNames);
        Assert.Empty(ConfigFile.Parse("k=1\nk=null\n").SectionNames);
        Assert.Empty(ConfigFile.Parse("[a]\nk=1\nk=null\n").SectionNames);

        // Erasing an absent key changes nothing.
        ConfigFileDocument document = ConfigFile.Parse("k=1\nabsent=null\n");
        Assert.Equal(["k"], document.GetSection("")!.KeyNames);
    }

    [Fact]
    public void NullInsideContainersIsKept()
    {
        ConfigFileDocument document = ConfigFile.Parse("a=[null]\nd={\"x\": null}\n");

        Assert.Equal(new Variant.Array([Variant.Nil.Instance]), Fixture.Require(document, "", "a"));
        Variant.Dictionary dictionary = (Variant.Dictionary)Fixture.Require(document, "", "d");
        Assert.Same(Variant.Nil.Instance, dictionary.Items[new Variant.Str("x")]);
    }

    [Fact]
    public void PreserveNilKeysKeepsNullsForMerging()
    {
        ParseOptions options = new() { PreserveNilKeys = true };
        ConfigFileDocument document = ConfigFile.Parse("[a]\nk=null\n", options);

        Assert.Equal(new Variant.Nil(), Fixture.Require(document, "a", "k"));
    }

    [Fact]
    public void MergeLaysOneDocumentOverAnother()
    {
        ConfigFileDocument baseDocument = ConfigFile.Parse("a=1\nb=2\n[s]\nk=1\n");
        ConfigFileDocument overlay = ConfigFile.Parse("b=3\nc=4\n[s]\nj=2\n");

        ConfigFileDocument merged = baseDocument.Merge(overlay);

        Assert.Equal(["", "s"], merged.SectionNames);
        Assert.Equal(["a", "b", "c"], merged.GetSection("")!.KeyNames);
        Assert.Equal(new Variant.Int(3), Fixture.Require(merged, "", "b"));
        Assert.Equal(["k", "j"], merged.GetSection("s")!.KeyNames);

        // Neither input is modified.
        Assert.Equal(new Variant.Int(2), Fixture.Require(baseDocument, "", "b"));
        Assert.False(baseDocument.Root!.ContainsKey("c"));
    }

    [Fact]
    public void MergeDeletesKeysCarriedAsNull()
    {
        ConfigFileDocument baseDocument = ConfigFile.Parse("a=1\nb=2\n[s]\nk=1\n");
        ConfigFileDocument overlay = ConfigFile.Parse("b=null\n[s]\nk=null\n", new ParseOptions { PreserveNilKeys = true });

        ConfigFileDocument merged = baseDocument.Merge(overlay);

        Assert.Equal([""], merged.SectionNames);
        Assert.Equal(["a"], merged.GetSection("")!.KeyNames);
        Assert.False(merged.HasSection("s"));
    }

    [Fact]
    public void MergeCannotDeleteKeysThatWereAlreadyErased()
    {
        // Without PreserveNilKeys the overlay has no record of the removal, so the base key survives.
        ConfigFileDocument baseDocument = ConfigFile.Parse("a=1\n");
        ConfigFileDocument overlay = ConfigFile.Parse("a=null\n");

        Assert.Equal(new Variant.Int(1), Fixture.Require(baseDocument.Merge(overlay), "", "a"));
    }

    [Fact]
    public void ByteOrderMarksAreStripped()
    {
        ParseResult result = ConfigFile.ParseWithResult("\uFEFF[a]\nk=1\n");

        Assert.True(result.Success);
        Assert.Equal(["a"], result.Document.SectionNames);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.BomStripped, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
    }

    [Fact]
    public void ByteOrderMarksCanBeKeptAndThenGlueOntoTheKey()
    {
        // Godot does not strip a BOM: it becomes part of the first key name, so the first section is lost.
        ConfigFileDocument document = ConfigFile.Parse("\uFEFF[a]\nk=1\n", new ParseOptions { StripBom = false });

        Assert.Equal([""], document.SectionNames);
        Assert.Equal("\uFEFF[a]k", document.GetSection("")!.KeyNames[0]);
    }

    [Fact]
    public void InvalidUtf8BytesBecomeReplacementCharactersWithAWarning()
    {
        byte[] bytes = [.. "k=\"a"u8.ToArray(), 0xFF, .. "b\"\n"u8.ToArray()];
        ParseResult result = ConfigFile.ParseUtf8WithResult(bytes);

        Assert.True(result.Success);
        Variant.Str value = (Variant.Str)Fixture.Require(result.Document, "", "k");
        Assert.Equal("a\uFFFDb", value.Value);
        Assert.Equal(DiagnosticCode.InvalidUtf8Byte, Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void DecodesUtf8ByteInput()
    {
        ParseResult result = ConfigFile.ParseUtf8WithResult("[a]\nk=\"纯文本\"\n"u8);

        Assert.True(result.Success);
        Assert.Equal(new Variant.Str("纯文本"), Fixture.Require(result.Document, "a", "k"));
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void FailedParseStillReturnsWhatWasRead()
    {
        const string text = "[a]\nk=1\nbroken=";

        ConfigFileParseException error = Assert.Throws<ConfigFileParseException>(() => ConfigFile.Parse(text));
        Assert.Equal(new Variant.Int(1), Fixture.Require(error.PartialDocument, "a", "k"));
        Assert.Equal(DiagnosticCode.ExpectedValue, error.Diagnostic.Code);
        Assert.Equal(3, error.Span.Start.Line);
        Assert.NotEmpty(error.Diagnostics);

        ParseResult result = ConfigFile.ParseWithResult(text);
        Assert.False(result.Success);
        Assert.Equal(new Variant.Int(1), Fixture.Require(result.Document, "a", "k"));
        Assert.Single(result.Problems);
    }

    [Fact]
    public void ThrowOnErrorCanBeTurnedOff()
    {
        ConfigFileDocument document = ConfigFile.Parse("broken=", new ParseOptions { ThrowOnError = false });

        Assert.Empty(document.SectionNames);
    }

    [Fact]
    public void LoadsFromAFileAndStripsItsByteOrderMark()
    {
        string path = Path.Combine(Path.GetTempPath(), $"godotcfg-{Guid.NewGuid():N}.godot");
        try
        {
            File.WriteAllBytes(path, [.. "\uFEFF"u8.ToArray(), .. "[a]\nk=1\n"u8.ToArray()]);

            ParseResult result = ConfigFile.ParseUtf8WithResult(File.ReadAllBytes(path));
            Assert.True(result.Success);
            Assert.Equal(["a"], result.Document.SectionNames);
            Assert.Equal(DiagnosticCode.BomStripped, Assert.Single(result.Diagnostics).Code);

            ConfigFileDocument document = ConfigFile.Load(path);
            Assert.Equal(new Variant.Int(1), Fixture.Require(document, "a", "k"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SpansPointAtTheProblem()
    {
        ConfigFileParseException error = Assert.Throws<ConfigFileParseException>(
            () => ConfigFile.Parse("[a]\nk = @bad\n"));

        Assert.Equal(2, error.Span.Start.Line);
        Assert.Equal(5, error.Span.Start.Column);
        Assert.Equal(8, error.Span.Start.Offset);
        Assert.Equal(10, error.Span.End.Offset);
    }
}
