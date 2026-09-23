namespace GodotConfigFileTests;

/// <summary>
/// The cases from Godot's own <c>tests/core/io/test_config_file.cpp</c>, ported so that the reference behaviour is
/// checked directly rather than re-derived.
/// </summary>
public class GodotOfficialTests
{
    /// <summary>"Parsing well-formatted files": hand-edited formatting that people actually write.</summary>
    private const string WellFormatted = """

        [player]

        name = "Unnamed Player"
        tagline="Waiting
        for
        Godot"

        color =Color(   0, 0.5,1, 1) ; Inline comment
        position= Vector2(
          3,
          4
        )

        [graphics]

        antialiasing = true

        ; Testing comments and case-sensitivity...
        antiAliasing = false

        """;

    /// <summary>"Parsing malformatted file": every line after the first section is wrong in some way.</summary>
    private const string Malformatted = """

        [player]

        name = "Unnamed Player"" ; Extraneous closing quote.
        tagline = "Waiting\nfor\nGodot"

        color = Color(0, 0.5, 1) ; Missing 4th parameter.
        position = Vector2(
          3,,
          4
        ) ; Extraneous comma.

        [graphics]

        antialiasing = true
        antialiasing = false ; Duplicate key.

        """;

    /// <summary>The exact text Godot's own writer produces in the "Saving file" case.</summary>
    private const string SavedByGodot = """
        [player]

        name="Unnamed Player"
        tagline="Waiting
        for
        Godot"
        color=Color(0, 0.5, 1, 1)
        position=Vector2(3, 4)

        [graphics]

        antialiasing=true
        antiAliasing=false

        [quoted]

        "静音"=42
        "a=b"=7

        """;

    /// <summary>The exact text Godot's writer produces for a dictionary holding an Object.</summary>
    private const string PrettyPrintedObject = """
        [input]

        ui_accept={
        "deadzone": 0.5,
        "events": [Object(InputEventKey,
        "resource_local_to_scene": false,
        "resource_name": "",
        "device": 16,
        "window_id": 0,
        "alt_pressed": false,
        "shift_pressed": false,
        "ctrl_pressed": false,
        "meta_pressed": false,
        "pressed": false,
        "keycode": 32,
        "physical_keycode": 0,
        "key_label": 0,
        "unicode": 0,
        "location": 0,
        "echo": false,
        "script": null
        )]
        }

        """;

    [Fact]
    public void ParsesWellFormattedFiles()
    {
        ConfigFileDocument document = ConfigFile.Parse(WellFormatted);

        Assert.Equal(new Variant.Str("Unnamed Player"), Fixture.Require(document, "player", "name"));
        Assert.Equal(new Variant.Str("Waiting\nfor\nGodot"), Fixture.Require(document, "player", "tagline"));
        Assert.Equal(new Variant.Color(new Color(0, 0.5f, 1, 1)), Fixture.Require(document, "player", "color"));
        Assert.Equal(new Variant.Vector2(new Vector2(3, 4)), Fixture.Require(document, "player", "position"));
        Assert.Equal(new Variant.Bool(true), Fixture.Require(document, "graphics", "antialiasing"));
        Assert.Equal(new Variant.Bool(false), Fixture.Require(document, "graphics", "antiAliasing"));

        Assert.Empty(ConfigFile.Parse("").SectionNames);
    }

    [Fact]
    public void RejectsMalformattedFiles()
    {
        ParseResult result = ConfigFile.ParseWithResult(Malformatted);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ReadsBackWhatGodotWrites()
    {
        ConfigFileDocument document = ConfigFile.Parse(SavedByGodot);

        Assert.Equal(new Variant.Str("Unnamed Player"), Fixture.Require(document, "player", "name"));
        Assert.Equal(new Variant.Str("Waiting\nfor\nGodot"), Fixture.Require(document, "player", "tagline"));
        Assert.Equal(new Variant.Color(new Color(0, 0.5f, 1, 1)), Fixture.Require(document, "player", "color"));
        Assert.Equal(new Variant.Vector2(new Vector2(3, 4)), Fixture.Require(document, "player", "position"));
        Assert.Equal(new Variant.Bool(true), Fixture.Require(document, "graphics", "antialiasing"));
        Assert.Equal(new Variant.Bool(false), Fixture.Require(document, "graphics", "antiAliasing"));
        Assert.Equal(new Variant.Int(42), Fixture.Require(document, "quoted", "静音"));
        Assert.Equal(new Variant.Int(7), Fixture.Require(document, "quoted", "a=b"));
    }

    [Fact]
    public void ReadsPrettyPrintedObjects()
    {
        ConfigFileDocument document = ConfigFile.Parse(PrettyPrintedObject);

        Variant.Dictionary action = (Variant.Dictionary)Fixture.Require(document, "input", "ui_accept");
        Assert.Equal(new Variant.Float(0.5), action.Items[new Variant.Str("deadzone")]);

        Variant.Array events = (Variant.Array)action.Items[new Variant.Str("events")];
        Variant.Object key = Assert.IsType<Variant.Object>(Assert.Single(events.Items));

        Assert.Equal("InputEventKey", key.ClassName);
        Assert.Equal(16, key.Properties.Count);
        Assert.Equal(new Variant.Bool(false), key.Properties[0].Value);
        Assert.Equal(new Variant.Int(32), key.Properties[9].Value);
        Assert.Equal(Variant.Nil.Instance, key.Properties[^1].Value);
    }
}
