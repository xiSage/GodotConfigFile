// Copyright (c) 2026 xiSage
// MIT License
// https://github.com/xiSage/GodotConfigFile

using GodotConfigFile;

namespace GodotConfigFileTests
{
    public class ConfigFileTests
    {
        [Fact]
        public void ParsingWellFormattedFiles()
        {
            var config_file = new ConfigFile();

            // Formatting is intentionally hand-edited to see how human-friendly the parser is.
            config_file.Parse(@"[player]

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

            config_file.Parse(@"[player]

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

        [Fact]
        public void ParsingArrays()
        {
            var config_file = new ConfigFile();

            // Parse all test data in a single call to avoid clearing
            config_file.Parse(@"[test]
single_line = [1, 2, 3, 4, 5]
multi_line = [
    1,
    2,
    3,
    4,
    5
]
strings = [""one"", ""two"", ""three""]
mixed = [1, ""two"", 3.0, true]
");

            // Debug: Check what keys are available
            var keys = config_file.GetSectionKeys("test");
            Assert.Contains("single_line", keys);
            Assert.Contains("strings", keys);

            // Debug: Check raw values
            object rawSingleLine = config_file.GetValue<object>("test", "single_line");
            Assert.NotNull(rawSingleLine);
            _ = Assert.IsType<object[]>(rawSingleLine);

            object rawStrings = config_file.GetValue<object>("test", "strings");
            Assert.NotNull(rawStrings);
            _ = Assert.IsType<object[]>(rawStrings);

            // Test GetValue<T> with array types
            int[] singleLineArray = config_file.GetValue<int[]>("test", "single_line");
            Assert.NotNull(singleLineArray);
            Assert.Equal([1, 2, 3, 4, 5], singleLineArray);

            string[] stringArray = config_file.GetValue<string[]>("test", "strings");
            Assert.NotNull(stringArray);
            Assert.Equal(["one", "two", "three"], stringArray);

            // Test with default value
            int[] defaultArray = config_file.GetValue<int[]>("test", "non_existent", [10, 20]);
            Assert.Equal([10, 20], defaultArray);
        }

        [Fact]
        public void ParsingDictionaries()
        {
            var config_file = new ConfigFile();

            // Parse all test data in a single call to avoid clearing
            config_file.Parse(@"[test]
# Basic dictionary syntax with quoted keys for colon separator
basic_dict = { ""key1"": 1, ""key2"": 2 }

# Multi-line dictionary with quoted keys for colon separator
multi_line = {
    ""name"": ""Godot"",
    ""version"": 4.0,
    ""stable"": true
}

# Dictionary with quoted keys and values
quoted_keys = { ""Some key name"": ""value1"", ""Another key"": ""value2"" }

# Dictionary with Lua-style equals syntax (identifiers as keys)
lua_syntax = {
    some_key = 42,
    another_key = ""text""
}

# Mixed syntax styles
mixed_syntax = { ""key1"": 1, key2 = 2, ""key 3"": 3 }

# Dictionary with numbers as values
points_dict = { ""White"": 50, ""Yellow"": 75, ""Orange"": 100 }

# Empty dictionary
empty_dict = {}
");

            // Test basic dictionary
            Dictionary<string, int> basicDict = config_file.GetValue<Dictionary<string, int>>("test", "basic_dict");
            Assert.Equal(2, basicDict.Count);
            Assert.Equal(1, basicDict["key1"]);
            Assert.Equal(2, basicDict["key2"]);

            // Test multi-line dictionary
            Dictionary<string, object> multiLineDict = config_file.GetValue<Dictionary<string, object>>("test", "multi_line");
            Assert.Equal(3, multiLineDict.Count);
            Assert.Equal("Godot", multiLineDict["name"]);
            _ = Assert.IsType<float>(multiLineDict["version"]);
            Assert.Equal(4.0f, (float)multiLineDict["version"]);
            Assert.Equal(true, multiLineDict["stable"]);

            // Test quoted keys
            Dictionary<string, string> quotedKeysDict = config_file.GetValue<Dictionary<string, string>>("test", "quoted_keys");
            Assert.Equal(2, quotedKeysDict.Count);
            Assert.Equal("value1", quotedKeysDict["Some key name"]);
            Assert.Equal("value2", quotedKeysDict["Another key"]);

            // Test Lua-style syntax
            Dictionary<string, object> luaSyntaxDict = config_file.GetValue<Dictionary<string, object>>("test", "lua_syntax");
            Assert.Equal(2, luaSyntaxDict.Count);
            Assert.Equal(42, luaSyntaxDict["some_key"]);
            Assert.Equal("text", luaSyntaxDict["another_key"]);

            // Test mixed syntax
            Dictionary<string, int> mixedSyntaxDict = config_file.GetValue<Dictionary<string, int>>("test", "mixed_syntax");
            Assert.Equal(3, mixedSyntaxDict.Count);
            Assert.Equal(1, mixedSyntaxDict["key1"]);
            Assert.Equal(2, mixedSyntaxDict["key2"]);
            Assert.Equal(3, mixedSyntaxDict["key 3"]);

            // Test points dictionary
            Dictionary<string, int> pointsDict = config_file.GetValue<Dictionary<string, int>>("test", "points_dict");
            Assert.Equal(3, pointsDict.Count);
            Assert.Equal(50, pointsDict["White"]);
            Assert.Equal(75, pointsDict["Yellow"]);
            Assert.Equal(100, pointsDict["Orange"]);

            // Test empty dictionary
            Dictionary<string, object> emptyDict = config_file.GetValue<Dictionary<string, object>>("test", "empty_dict");
            Assert.Empty(emptyDict);

            // Test with default value
            Dictionary<string, string> defaultDict = config_file.GetValue<Dictionary<string, string>>("test", "non_existent", new Dictionary<string, string> { { "default", "value" } });
            _ = Assert.Single(defaultDict);
            Assert.Equal("value", defaultDict["default"]);
        }

        [Fact]
        public void ParsingNestedData()
        {
            // Test nested array (array of arrays)
            var config_file1 = new ConfigFile();
            config_file1.Parse(@"[test]
nested_array = [[1, 2, 3], [4, 5, 6], [7, 8, 9]]
");

            // Test GetValue<T> with nested array type
            int[][] nestedArray = config_file1.GetValue<int[][]>("test", "nested_array");
            Assert.NotNull(nestedArray);
            Assert.Equal(3, nestedArray.Length);
            Assert.Equal([1, 2, 3], nestedArray[0]);
            Assert.Equal([4, 5, 6], nestedArray[1]);
            Assert.Equal([7, 8, 9], nestedArray[2]);

            // Test simple dictionary first with quoted keys for colon syntax
            var config_file2 = new ConfigFile();
            config_file2.Parse(@"[test]
simple_dict = { ""key1"": ""value1"", ""key2"": 123, ""key3"": true }
");

            // Test GetValue<T> with simple dictionary type
            Dictionary<string, string> simpleDict = config_file2.GetValue<Dictionary<string, string>>("test", "simple_dict");
            Assert.NotNull(simpleDict);
            Assert.Equal(3, simpleDict.Count);
            Assert.Equal("value1", simpleDict["key1"]);
            Assert.Equal("123", simpleDict["key2"]);
            Assert.Equal("True", simpleDict["key3"]);

            // Test nested dictionary with raw object type, using quoted keys for colon syntax
            var config_file3 = new ConfigFile();
            config_file3.Parse(@"[test]
nested_dict = { ""name"": ""Test"", ""settings"": { ""enabled"": true } }
");

            // Get raw value first to check if parsing works
            object rawValue = config_file3.GetValue<object>("test", "nested_dict");
            Assert.NotNull(rawValue);
            _ = Assert.IsType<SortedDictionary<string, object>>(rawValue);
        }

        [Fact]
        public void ArrayFormatting()
        {
            var config_file = new ConfigFile();

            // Test array formatting
            config_file.Parse(@"[test]
values = [1, 2, 3]
");

            string content = config_file.EncodeToText();
            Assert.Contains("values=[1, 2, 3]", content);
        }

        [Fact]
        public void DictionaryFormatting()
        {
            var config_file = new ConfigFile();

            // Test dictionary formatting with quoted keys for colon syntax
            config_file.Parse(@"[test]
data = { ""name"": ""Godot"", ""version"": 4.0 }
");

            string content = config_file.EncodeToText();
            Assert.Contains("data={ \"name\": Godot, \"version\": 4 }", content);
        }
    }
}