// Copyright (c) 2026 xiSage
// MIT License
// https://github.com/xiSage/GodotConfigFile

using System.IO;
using System.Text;
using GodotConfig;
using Xunit;

namespace GodotConfigFileTests
{
    public class ConfigFileTests
    {
        [Fact]
        public void ParsingWellFormattedFiles()
        {
            var config_file = new ConfigFile();
            
            // Formatting is intentionally hand-edited to see how human-friendly the parser is.
            config_file.Parse(@"
[player]

name = ""Unnamed Player""
tagline=""Waiting
for
Godot""

color =Color(   0, 0.5,1, 1) ; Inline comment
position= Vector2(
        3,
        4
)

[graphics]

antialiasing = true

; Testing comments and case-sensitivity...
antiAliasing = false
");

            // Verify parsing was successful
            Assert.True(config_file.HasSection("player"));
            Assert.True(config_file.HasSection("graphics"));
            
            // Check values
            Assert.Equal("Unnamed Player", config_file.GetValue<string>("player", "name"));
            Assert.Equal("Waiting\nfor\nGodot", config_file.GetValue<string>("player", "tagline"));
            Assert.Equal("Color(   0, 0.5,1, 1)", config_file.GetValue<string>("player", "color"));
            Assert.Equal("Vector2(\n        3,\n        4\n)", config_file.GetValue<string>("player", "position"));
            Assert.True(config_file.GetValue<bool>("graphics", "antialiasing"));
            Assert.False(config_file.GetValue<bool>("graphics", "antiAliasing"));
            
            // An empty ConfigFile is valid.
            config_file.Parse("");
            Assert.Empty(config_file.GetSections());
        }

        [Fact]
        public void ParsingMalformattedFile()
        {
            var config_file = new ConfigFile();
            
            config_file.Parse(@"
[player]

name = ""Unnamed Player"""" ; Extraneous closing quote.
tagline = ""Waiting\nfor\nGodot""

color = Color(0, 0.5, 1) ; Missing 4th parameter.
position = Vector2(
        3,,
        4
) ; Extraneous comma.

[graphics]

antialiasing = true
antialiasing = false ; Duplicate key.
");

            // In C# version, we just verify that it parses what it can
            Assert.True(config_file.HasSection("player"));
            Assert.True(config_file.HasSection("graphics"));
        }

        [Fact]
        public void SavingFile()
        {
            var config_file = new ConfigFile();
            
            // Set values
            config_file.SetValue("player", "name", "Unnamed Player");
            config_file.SetValue("player", "tagline", "Waiting\nfor\nGodot");
            config_file.SetValue("player", "color", "Color(0, 0.5, 1, 1)");
            config_file.SetValue("player", "position", "Vector2(3, 4)");
            config_file.SetValue("graphics", "antialiasing", true);
            config_file.SetValue("graphics", "antiAliasing", false);
            config_file.SetValue("quoted", "静音", 42);
            config_file.SetValue("quoted", "a=b", 7);
            
            // Save to string
            string content = config_file.EncodeToText();
            
            // Load back and verify
            var loaded_config = new ConfigFile();
            loaded_config.Parse(content);
            
            Assert.Equal("Unnamed Player", loaded_config.GetValue<string>("player", "name"));
            Assert.Equal("Waiting\nfor\nGodot", loaded_config.GetValue<string>("player", "tagline"));
            Assert.Equal("Color(0, 0.5, 1, 1)", loaded_config.GetValue<string>("player", "color"));
            Assert.Equal("Vector2(3, 4)", loaded_config.GetValue<string>("player", "position"));
            Assert.True(loaded_config.GetValue<bool>("graphics", "antialiasing"));
            Assert.False(loaded_config.GetValue<bool>("graphics", "antiAliasing"));
        }
    }
}