namespace GodotConfigFileTests;

/// <summary>
/// The top-level reader: section headers, key names and how they glue across whitespace, newlines and comments.
/// </summary>
public class TagAndKeyTests
{
    [Fact]
    public void ReadsSectionsAndRootKeys()
    {
        ConfigFileDocument document = ConfigFile.Parse("config_version=5\n\n[application]\nname=\"x\"\n");

        // Godot inserts the section-less map at the front, so top-level keys always list first.
        Assert.Equal(["", "application"], document.SectionNames);
        Assert.Equal(new Variant.Int(5), Fixture.Require(document, "", "config_version"));
        Assert.Equal(new Variant.Str("x"), Fixture.Require(document, "application", "name"));
    }

    [Fact]
    public void RepeatedSectionsMerge()
    {
        ConfigFileDocument document = ConfigFile.Parse("[a]\nk=1\n\n[b]\nz=0\n\n[a]\nj=2\n");

        Assert.Equal(["a", "b"], document.SectionNames);
        ConfigSection section = document.GetSection("a")!;
        Assert.Equal(["k", "j"], section.KeyNames);
    }

    [Fact]
    public void SectionNamesAreCaseSensitiveAndVerbatim()
    {
        ConfigFileDocument document = ConfigFile.Parse("[A]\nk=1\n[a]\nj=2\n[res://x/y.gd]\nm=3\n[preset.0]\nn=4\n[a=b]\no=5\n");

        Assert.Equal(["A", "a", "res://x/y.gd", "preset.0", "a=b"], document.SectionNames);
    }

    [Fact]
    public void EscapedBracketInSectionNameIsRestored()
    {
        Assert.Equal(["a]b"], ConfigFile.Parse("[a\\]b]\nk=1\n").SectionNames);
    }

    [Fact]
    public void BackslashInSectionNameIsKept()
    {
        Assert.Equal(["a\\b"], ConfigFile.Parse("[a\\b]\nk=1\n").SectionNames);
    }

    [Fact]
    public void EmptySectionHeaderKeepsTheCurrentSection()
    {
        ConfigFileDocument document = ConfigFile.Parse("[a]\nk=1\n[]\nm=2\n[   ]\nn=3\n");

        Assert.Equal(["a"], document.SectionNames);
        Assert.Equal(["k", "m", "n"], document.GetSection("a")!.KeyNames);
    }

    [Fact]
    public void KeyNamesSwallowWhitespace()
    {
        Assert.Equal(["mykey"], ConfigFile.Parse("my key=1\n").GetSection("")!.KeyNames);
        Assert.Equal(["key"], ConfigFile.Parse("\t key = 1\n").GetSection("")!.KeyNames);
    }

    [Fact]
    public void KeyNamesRunAcrossNewlines()
    {
        // A newline is not a statement terminator: "foo" and "bar" glue into one key.
        Assert.Equal(["foobar"], ConfigFile.Parse("foo\nbar=1\n").GetSection("")!.KeyNames);
    }

    [Fact]
    public void CommentsDoNotEndAKeyName()
    {
        Assert.Equal(["ab"], ConfigFile.Parse("a; comment\nb=1\n").GetSection("")!.KeyNames);
    }

    [Fact]
    public void ResidualCharactersGlueOntoTheNextKey()
    {
        ConfigFileDocument document = ConfigFile.Parse("a=1 remainder\nb=2\n");

        Assert.Equal(["a", "remainderb"], document.GetSection("")!.KeyNames);
        Assert.Equal(new Variant.Int(1), Fixture.Require(document, "", "a"));
        Assert.Equal(new Variant.Int(2), Fixture.Require(document, "", "remainderb"));
    }

    [Fact]
    public void ResidualCharactersAfterASectionHeaderGlueOntoTheNextKey()
    {
        ConfigFileDocument document = ConfigFile.Parse("[section] extra\nkey=1\n");

        Assert.Equal(["section"], document.SectionNames);
        Assert.Equal(new Variant.Int(1), Fixture.Require(document, "section", "extrakey"));
    }

    [Fact]
    public void TwoAssignmentsMayShareALine()
    {
        ConfigFileDocument document = ConfigFile.Parse("a=1 b=2\nc=\"x\" d=true\n");

        Assert.Equal(["a", "b", "c", "d"], document.GetSection("")!.KeyNames);
    }

    [Fact]
    public void BracketInsideAKeyNameIsNotASection()
    {
        // Only a '[' with nothing accumulated before it starts a section header.
        ConfigFileDocument document = ConfigFile.Parse("x[y]=1\n");

        Assert.Equal([""], document.SectionNames);
        Assert.Equal(new Variant.Int(1), Fixture.Require(document, "", "x[y]"));
    }

    [Fact]
    public void HashIsNotAComment()
    {
        // '# comment' becomes part of the next key name; only ';' comments.
        ConfigFileDocument document = ConfigFile.Parse("# comment\nk=1\n");

        Assert.Equal(["#commentk"], document.GetSection("")!.KeyNames);
        Assert.Equal(new Variant.Int(1), Fixture.Require(document, "", "#commentk"));
    }

    [Fact]
    public void QuotedKeyNamesReplaceWhatWasAccumulated()
    {
        Assert.Equal(["my key"], ConfigFile.Parse("\"my key\"=1\n").GetSection("")!.KeyNames);
        Assert.Equal(["ab"], ConfigFile.Parse("x\"ab\"=1\n").GetSection("")!.KeyNames);
        Assert.Equal(["ab"], ConfigFile.Parse("\"a\"b=1\n").GetSection("")!.KeyNames);
        Assert.Equal(["静音"], ConfigFile.Parse("\"静音\"=1\n").GetSection("")!.KeyNames);
    }

    [Fact]
    public void KeyWithNoNameIsDropped()
    {
        ConfigFileDocument document = ConfigFile.Parse("=5\n");

        Assert.Empty(document.SectionNames);
    }

    [Fact]
    public void DanglingKeyAtEndOfInputIsIgnored()
    {
        ConfigFileDocument document = ConfigFile.Parse("[a]\nk=1\ndangling");

        Assert.Equal(["a"], document.SectionNames);
        Assert.Equal(new Variant.Int(1), Fixture.Require(document, "a", "k"));
    }

    [Fact]
    public void KeyWithoutAValueIsAnError()
    {
        ConfigFileParseException error = Assert.Throws<ConfigFileParseException>(() => ConfigFile.Parse("k="));

        Assert.Equal(DiagnosticCode.ExpectedValue, error.Diagnostic.Code);
        Assert.Equal("Expected value, got 'EOF'", error.Diagnostic.Message);
    }

    [Fact]
    public void EmptyAndCommentOnlyFilesAreValid()
    {
        Assert.Empty(ConfigFile.Parse("").SectionNames);
        Assert.Empty(ConfigFile.Parse("\n\n\n").SectionNames);
        Assert.Empty(ConfigFile.Parse("; only a comment").SectionNames);
        Assert.Empty(ConfigFile.Parse("\r\n").SectionNames);
    }

    [Fact]
    public void CarriageReturnsAreWhitespace()
    {
        ConfigFileDocument document = ConfigFile.Parse("[a]\r\nk=1\r\n");

        Assert.Equal(new Variant.Int(1), Fixture.Require(document, "a", "k"));
    }

    [Fact]
    public void UnclosedSectionHeaderIsAnError()
    {
        ConfigFileParseException error = Assert.Throws<ConfigFileParseException>(() => ConfigFile.Parse("[a\nk=1\n"));

        Assert.Equal(DiagnosticCode.UnexpectedEof, error.Diagnostic.Code);
        Assert.Equal("Unexpected EOF while parsing simple tag", error.Diagnostic.Message);
    }

    [Fact]
    public void SectionHeaderTokensAreNotParsed()
    {
        // The simple-tag reader takes characters literally: field syntax from .tscn does not apply here.
        Assert.Equal(["node name=\"X\" type=\"Y\""], ConfigFile.Parse("[node name=\"X\" type=\"Y\"]\nk=1\n").SectionNames);
    }

    [Fact]
    public void NulIsDroppedInsideAKeyName()
    {
        ConfigFileDocument document = ConfigFile.Parse("a\0b=1\n");

        Assert.Equal(["ab"], document.GetSection("")!.KeyNames);
    }

    [Fact]
    public void DuplicateKeysKeepTheLastValueAndReportAnInfo()
    {
        ParseResult result = ConfigFile.ParseWithResult("[a]\nk=1\nk=2\n");

        Assert.True(result.Success);
        Assert.Equal(new Variant.Int(2), Fixture.Require(result.Document, "a", "k"));
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.DuplicateKey, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Equal(3, diagnostic.Span.Start.Line);
    }
}
