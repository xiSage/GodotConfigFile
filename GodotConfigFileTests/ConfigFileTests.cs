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
        public void BasicFunctionality()
        {
            // Test basic value setting and getting
            var config = new ConfigFile();
            
            // Set values in default section
            config.SetValue("name", "Godot");
            config.SetValue("version", 3.4f);
            config.SetValue("is_stable", true);
            config.SetValue("downloads", 1000000L);
            
            // Get values and verify
            Assert.Equal("Godot", config.GetValue<string>("name"));
            Assert.Equal(3.4f, config.GetValue<float>("version"));
            Assert.True(config.GetValue<bool>("is_stable"));
            Assert.Equal(1000000L, config.GetValue<long>("downloads"));
            
            // Test non-existent key with default value
            Assert.Equal("default", config.GetValue<string>("non_existent", "default"));
            Assert.Equal(0, config.GetValue<int>("non_existent"));
        }
        
        [Fact]
        public void SectionFunctionality()
        {
            var config = new ConfigFile();
            
            // Set values in different sections
            config.SetValue("window", "width", 1920);
            config.SetValue("window", "height", 1080);
            config.SetValue("graphics", "quality", "high");
            config.SetValue("graphics", "vsync", true);
            
            // Verify section existence
            Assert.True(config.HasSection("window"));
            Assert.True(config.HasSection("graphics"));
            Assert.False(config.HasSection("audio"));
            
            // Verify key existence
            Assert.True(config.HasKey("window", "width"));
            Assert.True(config.HasKey("graphics", "vsync"));
            Assert.False(config.HasKey("window", "vsync"));
            
            // Get values from sections
            Assert.Equal(1920, config.GetValueInSection<int>("window", "width"));
            Assert.Equal(1080, config.GetValueInSection<int>("window", "height"));
            Assert.Equal("high", config.GetValueInSection<string>("graphics", "quality"));
            Assert.True(config.GetValueInSection<bool>("graphics", "vsync"));
        }
        
        [Fact]
        public void SaveAndLoadFunctionality()
        {
            var config = new ConfigFile();
            
            // Set up test data
            config.SetValue("name", "Godot");
            config.SetValue("version", 3.4f);
            config.SetValue("window", "width", 1920);
            config.SetValue("window", "height", 1080);
            config.SetValue("graphics", "quality", "high");
            config.SetValue("graphics", "vsync", true);
            
            // Test saving to string
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                config.SaveToStream(writer);
            }
            string configText = sb.ToString();
            
            // Verify the saved text contains expected content
            Assert.Contains("name=Godot", configText);
            Assert.Contains("version=3.4", configText);
            Assert.Contains("[window]", configText);
            Assert.Contains("width=1920", configText);
            Assert.Contains("[graphics]", configText);
            Assert.Contains("quality=high", configText);
            
            // Test loading from string
            var loadedConfig = new ConfigFile();
            using (var reader = new StringReader(configText))
            {
                loadedConfig.LoadFromStream(reader);
            }
            
            // Verify loaded data matches original
            Assert.Equal("Godot", loadedConfig.GetValue<string>("name"));
            Assert.Equal(3.4f, loadedConfig.GetValue<float>("version"));
            Assert.Equal(1920, loadedConfig.GetValueInSection<int>("window", "width"));
            Assert.Equal(1080, loadedConfig.GetValueInSection<int>("window", "height"));
            Assert.Equal("high", loadedConfig.GetValueInSection<string>("graphics", "quality"));
            Assert.True(loadedConfig.GetValueInSection<bool>("graphics", "vsync"));
        }
        
        [Fact]
        public void MergeFunctionality()
        {
            var config1 = new ConfigFile();
            var config2 = new ConfigFile();
            
            // Set up config1
            config1.SetValue("name", "Godot");
            config1.SetValue("version", 3.4f);
            config1.SetValue("window", "width", 1920);
            config1.SetValue("window", "height", 1080);
            
            // Set up config2 (to merge)
            config2.SetValue("name", "Godot Engine");
            config2.SetValue("author", "Godot Contributors");
            config2.SetValue("window", "width", 3840);
            config2.SetValue("window", "vsync", true);
            
            // Merge without overwriting
            config1.Merge(config2, overwrite: false);
            
            // Verify values were merged correctly
            Assert.Equal("Godot", config1.GetValue<string>("name")); // Should not be overwritten
            Assert.Equal("Godot Contributors", config1.GetValue<string>("author")); // New key
            Assert.Equal(1920, config1.GetValueInSection<int>("window", "width")); // Should not be overwritten
            Assert.Equal(1080, config1.GetValueInSection<int>("window", "height")); // Original value preserved
            Assert.True(config1.GetValueInSection<bool>("window", "vsync")); // New key
            
            // Merge with overwriting
            var config3 = new ConfigFile();
            config3.SetValue("name", "Godot");
            config3.SetValue("version", 3.4f);
            config3.Merge(config2, overwrite: true);
            
            // Verify values were overwritten correctly
            Assert.Equal("Godot Engine", config3.GetValue<string>("name")); // Should be overwritten
            Assert.Equal("Godot Contributors", config3.GetValue<string>("author")); // New key
            Assert.Equal(3840, config3.GetValueInSection<int>("window", "width")); // Should be overwritten
        }
        
        [Fact]
        public void TypeHandling()
        {
            var config = new ConfigFile();
            
            // Set various types
            config.SetValue("bool_true", true);
            config.SetValue("bool_false", false);
            config.SetValue("int", 42);
            config.SetValue("float", 3.14f);
            config.SetValue("double", 2.71828);
            config.SetValue("long", 1234567890L);
            config.SetValue("string", "test");
            
            // Save and load to test type parsing
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                config.SaveToStream(writer);
            }
            
            var loadedConfig = new ConfigFile();
            using (var reader = new StringReader(sb.ToString()))
            {
                loadedConfig.LoadFromStream(reader);
            }
            
            // Verify types are correctly parsed
            Assert.True(loadedConfig.GetValue<bool>("bool_true"));
            Assert.False(loadedConfig.GetValue<bool>("bool_false"));
            Assert.Equal(42, loadedConfig.GetValue<int>("int"));
            Assert.Equal(3.14f, loadedConfig.GetValue<float>("float"));
            Assert.InRange(loadedConfig.GetValue<double>("double"), 2.71827, 2.71829); // Use range for double comparison
            Assert.Equal(1234567890L, loadedConfig.GetValue<long>("long"));
            Assert.Equal("test", loadedConfig.GetValue<string>("string"));
        }
        
        [Fact]
        public void EraseFunctionality()
        {
            var config = new ConfigFile();
            
            // Set up test data
            config.SetValue("name", "Godot");
            config.SetValue("version", 3.4f);
            config.SetValue("window", "width", 1920);
            config.SetValue("window", "height", 1080);
            config.SetValue("graphics", "quality", "high");
            
            // Erase a key
            config.EraseKey("version");
            Assert.False(config.HasKey("version"));
            
            // Erase a key from a section
            config.EraseKey("window", "height");
            Assert.False(config.HasKey("window", "height"));
            Assert.True(config.HasKey("window", "width")); // Other key in same section should remain
            
            // Erase an entire section
            config.EraseSection("window");
            Assert.False(config.HasSection("window"));
            Assert.False(config.HasKey("window", "width"));
            
            // Verify other sections remain
            Assert.True(config.HasSection("graphics"));
            
            // Clear all
            config.Clear();
            Assert.False(config.HasSection("graphics"));
            Assert.False(config.HasKey("name"));
        }
        
        [Fact]
        public void WellFormattedParsing()
        {
            var config = new ConfigFile();
            
            // Test parsing a well-formatted config string
            string configText = @"name=Godot
version=3.4
is_stable=true

[window]
width=1920
height=1080

[graphics]
quality=high
vsync=true
";
            
            using (var reader = new StringReader(configText))
            {
                config.LoadFromStream(reader);
            }
            
            // Verify parsing was successful
            Assert.Equal("Godot", config.GetValue<string>("name"));
            Assert.Equal(3.4f, config.GetValue<float>("version"));
            Assert.True(config.GetValue<bool>("is_stable"));
            Assert.Equal(1920, config.GetValueInSection<int>("window", "width"));
            Assert.Equal(1080, config.GetValueInSection<int>("window", "height"));
            Assert.Equal("high", config.GetValueInSection<string>("graphics", "quality"));
            Assert.True(config.GetValueInSection<bool>("graphics", "vsync"));
        }
        
        [Fact]
        public void MalformattedParsing()
        {
            var config = new ConfigFile();
            
            // Test parsing a malformatted config string (should still parse, just ignore invalid parts)
            string configText = @"name=Godot
version=3.4
invalid_line_without_equals_sign

[window
width=1920
height=1080

[graphics]
quality=high
vsync=true
";
            
            using (var reader = new StringReader(configText))
            {
                config.LoadFromStream(reader);
            }
            
            // Verify that valid parts were parsed correctly
            Assert.Equal("Godot", config.GetValue<string>("name"));
            Assert.Equal(3.4f, config.GetValue<float>("version"));
            // The [window section is malformed, so its values should not be parsed
            Assert.False(config.HasKey("window", "width"));
            // The [graphics] section is correctly formatted, so its values should be parsed
            Assert.Equal("high", config.GetValueInSection<string>("graphics", "quality"));
            Assert.True(config.GetValueInSection<bool>("graphics", "vsync"));
        }
        
        [Fact]
        public void CommentHandling()
        {
            var config = new ConfigFile();
            
            // Test parsing config with comments
            string configText = @"name=Godot # This is a comment
version=3.4 // Another comment

# Section comment
[window]
width=1920 ; Inline comment
height=1080

// Empty line above
[graphics]
quality=high
";
            
            using (var reader = new StringReader(configText))
            {
                config.LoadFromStream(reader);
            }
            
            // Verify comments were ignored and values parsed correctly
            Assert.Equal("Godot", config.GetValue<string>("name"));
            Assert.Equal(3.4f, config.GetValue<float>("version"));
            Assert.Equal(1920, config.GetValueInSection<int>("window", "width"));
            Assert.Equal(1080, config.GetValueInSection<int>("window", "height"));
            Assert.Equal("high", config.GetValueInSection<string>("graphics", "quality"));
        }
        
        [Fact]
        public void EdgeCases()
        {
            var config = new ConfigFile();
            
            // Test empty config
            Assert.Empty(config.GetSections());
            Assert.Empty(config.GetKeys());
            
            // Test saving and loading empty config
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                config.SaveToStream(writer);
            }
            
            var loadedConfig = new ConfigFile();
            using (var reader = new StringReader(sb.ToString()))
            {
                loadedConfig.LoadFromStream(reader);
            }
            
            Assert.Empty(loadedConfig.GetSections());
            
            // Test very large numbers
            config.SetValue("large_int", int.MaxValue);
            config.SetValue("large_long", long.MaxValue);
            config.SetValue("large_float", float.MaxValue);
            
            Assert.Equal(int.MaxValue, config.GetValue<int>("large_int"));
            Assert.Equal(long.MaxValue, config.GetValue<long>("large_long"));
            Assert.Equal(float.MaxValue, config.GetValue<float>("large_float"));
            
            // Test special characters in strings
            config.SetValue("special_chars", "!@#$%^&*()_+-=[]{}|;:,.<>?\"'\n\t");
            Assert.Equal("!@#$%^&*()_+-=[]{}|;:,.<>?\"'\n\t", config.GetValue<string>("special_chars"));
        }
    }
}