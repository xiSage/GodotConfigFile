using GodotConfigFile;

// Smoke test for the trim/AOT check in CI: it exercises parsing and typed lookups from a native-compiled binary,
// so any reflection creeping into the library would show up as a trimming warning or a runtime failure.

const string Text = """
    [application]

    config/name="AotSmoke"
    config/features=PackedStringArray("4.7", "GL Compatibility")

    [rendering]

    renderer/rendering_method="gl_compatibility"
    """;

ConfigFileDocument document = ConfigFile.Parse(Text);

Console.WriteLine(document.GetValue("application", "config/name", "(missing)"));
Console.WriteLine(string.Join(", ", document.GetValue<string[]>("application", "config/features", [])));
Console.WriteLine(document.GetValue("rendering", "renderer/rendering_method", "(missing)"));

ParseResult failed = ConfigFile.ParseWithResult("broken=\n");
Console.WriteLine($"failed parse kept {failed.Document.SectionNames.Count} section(s), success={failed.Success}");

return document.HasSection("application") && document.HasSection("rendering") && !failed.Success ? 0 : 1;
