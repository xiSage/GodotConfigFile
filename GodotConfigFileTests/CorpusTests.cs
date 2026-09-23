namespace GodotConfigFileTests;

/// <summary>
/// Real files written by Godot and by Godot projects, checked in under <c>corpus/</c>.
/// </summary>
public class CorpusTests
{
    /// <summary>Every corpus file, with the feature it is here to cover.</summary>
    public static TheoryData<string> AllFiles =>
    [
        "godot/scene_groups_cache.cfg",              // 0 bytes
        "godot/compatibility-test-project.godot",    // smallest real project.godot
        "godot/android-assets-project.godot",        // comments, PackedStringArray, nested key names
        "godot/gdscript-tests-project.godot",        // multi-line dictionary
        "godot/global_script_class_cache.cfg",       // no section headers, StringName values
        "real/script_editor_cache.cfg",              // non-ASCII value, Array[int], section names with '://'
        "real/xisage-project.godot",                 // Color(...), PackedStringArray, apostrophes in strings
        "real/export_presets.cfg",                   // section names with dots, empty PackedStringArray()
        "real/editstate.cfg",                        // deepest nesting, several vector constructors
        "real/godot-swf-project.godot",
        "real/godot-vlc-project.godot",              // custom section
    ];

    [Theory]
    [MemberData(nameof(AllFiles))]
    public void ParsesWithoutErrors(string relative)
    {
        string path = Fixture.CorpusPath(relative);
        Assert.True(File.Exists(path), $"Missing corpus file {path}.");

        ParseResult result = ConfigFile.ParseUtf8WithResult(File.ReadAllBytes(path));

        Assert.True(
            result.Success,
            $"{relative} produced {string.Join("; ", result.Diagnostics)}");
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void EmptyFileHasNothingInIt()
    {
        ParseResult result = ConfigFile.ParseUtf8WithResult(File.ReadAllBytes(Fixture.CorpusPath("godot/scene_groups_cache.cfg")));

        Assert.True(result.Success);
        Assert.Empty(result.Document.SectionNames);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void SmallestProjectSettingsFile()
    {
        ConfigFileDocument document = Fixture.ParseCorpus("godot/compatibility-test-project.godot");

        Assert.Equal(new Variant.Int(5), Fixture.Require(document, "", "config_version"));
        Assert.Equal(2, document.GetSection("application")!.Count);
    }

    [Fact]
    public void ProjectSettingsWithCommentsAndFeatureList()
    {
        ConfigFileDocument document = Fixture.ParseCorpus("godot/android-assets-project.godot");

        Assert.Equal(["", "animation", "application", "debug", "rendering"], document.SectionNames);
        Assert.Equal(new Variant.Int(5), Fixture.Require(document, "", "config_version"));
        Assert.Equal(
            new Variant.Str("Godot App Instrumentation Tests"),
            Fixture.Require(document, "application", "config/name"));
        Assert.Equal<string[]>(
            ["4.7", "GL Compatibility"],
            document.GetValue<string[]>("application", "config/features", []));
        Assert.Equal(3, document.GetSection("rendering")!.Count);

        // A dot in a key name is just a character: it is not a hierarchy separator.
        Assert.Equal(
            new Variant.Str("gl_compatibility"),
            Fixture.Require(document, "rendering", "renderer/rendering_method.mobile"));
    }

    [Fact]
    public void CacheFileWithoutAnySectionHeader()
    {
        ConfigFileDocument document = Fixture.ParseCorpus("godot/global_script_class_cache.cfg");

        Assert.Equal([""], document.SectionNames);
        Variant.Array list = Assert.IsType<Variant.Array>(Fixture.Require(document, "", "list"));
        Assert.NotEmpty(list.Items);

        Variant.Dictionary first = Assert.IsType<Variant.Dictionary>(list.Items[0]);
        Assert.Contains(
            first.Items.Values,
            value => value.Kind == VariantType.StringName);
    }

    [Fact]
    public void EditorCacheWithNonAsciiAndTypedArrays()
    {
        ConfigFileDocument document = Fixture.ParseCorpus("real/script_editor_cache.cfg");

        Assert.Equal(9, document.SectionNames.Count);
        Assert.All(document.SectionNames, name => Assert.StartsWith("res://", name, StringComparison.Ordinal));

        // Both of these live inside dictionary values, so the search has to descend.
        List<Variant> everything = [.. AllValues(document).SelectMany(Descendants)];

        Assert.Contains(everything, value => value is Variant.Str { Value: "纯文本" });
        Assert.Contains(everything, value => value is Variant.Array { ElementType: VariantType.Int });
    }

    [Fact]
    public void ExportPresetsWithDottedSections()
    {
        ConfigFileDocument document = Fixture.ParseCorpus("real/export_presets.cfg");

        Assert.Equal(21, document.GetSection("preset.0")!.Count);
        Assert.Equal(23, document.GetSection("preset.0.options")!.Count);
        Assert.Equal(new Variant.PackedStringArray([]), Fixture.Require(document, "preset.0", "patches"));
        Assert.Equal(
            new Variant.Color(new Color(0, 0, 0, 1)),
            Fixture.Require(document, "preset.0.options", "progressive_web_app/background_color"));
    }

    [Fact]
    public void EditStateWithTheDeepestNesting()
    {
        ConfigFileDocument document = Fixture.ParseCorpus("real/editstate.cfg");
        ConfigSection states = document.GetSection("editor_states")!;

        Assert.Equal(4, states.Count);

        Variant.Array selected = (Variant.Array)Fixture.Require(document, "editor_states", "selected_nodes");
        Assert.Equal(VariantType.NodePath, selected.ElementType);
        Assert.Empty(selected.Items);
    }

    [Fact]
    public void ProjectFilesWithCustomSections()
    {
        ConfigFileDocument document = Fixture.ParseCorpus("real/godot-vlc-project.godot");

        Assert.True(document.HasSection("vlc"));
        Assert.True(document.HasSection("application"));
        Assert.Equal(new Variant.Int(0), Fixture.Require(document, "vlc", "log_level"));

        ConfigFileDocument swf = Fixture.ParseCorpus("real/godot-swf-project.godot");
        Assert.Equal(["", "animation", "application", "dotnet"], swf.SectionNames);
    }

    private static IEnumerable<Variant> AllValues(ConfigFileDocument document)
    {
        foreach (string sectionName in document.SectionNames)
        {
            ConfigSection section = document.GetSection(sectionName)!;
            foreach (string key in section.KeyNames)
            {
                yield return section[key]!;
            }
        }
    }

    /// <summary>Walks a value and everything nested inside it.</summary>
    private static IEnumerable<Variant> Descendants(Variant value)
    {
        yield return value;

        switch (value)
        {
            case Variant.Array array:
                foreach (Variant item in array.Items.SelectMany(Descendants))
                {
                    yield return item;
                }

                break;

            case Variant.Dictionary dictionary:
                foreach (KeyValuePair<Variant, Variant> entry in dictionary.Items)
                {
                    foreach (Variant nested in Descendants(entry.Key).Concat(Descendants(entry.Value)))
                    {
                        yield return nested;
                    }
                }

                break;

            case Variant.Object obj:
                foreach (Variant nested in obj.Properties.SelectMany(property => Descendants(property.Value)))
                {
                    yield return nested;
                }

                break;
        }
    }
}
