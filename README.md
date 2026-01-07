[![NuGet Version](https://img.shields.io/nuget/v/xiSage.GodotConfigFile)](https://www.nuget.org/packages/xiSage.GodotConfigFile)

# Godot ConfigFile for C#

A C# implementation of Godot's `config_file.cpp` that follows C# conventions and targets .NET Standard 2.0.

## Project Structure

```
GodotConfigFile/
├── GodotConfigFile.slnx                # Solution file
├── .gitattributes                      # Git attributes
├── .gitignore                          # Git ignore rules
├── LICENSE.txt                         # MIT License
├── README.md                           # This file
├── .vscode/                            # VSCode configuration
│   ├── launch.json
│   └── tasks.json
├── GodotConfigFile/                    # Core library directory
│   ├── ConfigFile.cs                   # Core ConfigFile class
│   └── GodotConfigFile.csproj          # Library project (netstandard2.0)
└── GodotConfigFileTests/               # Comprehensive test project
    ├── ConfigFileTests.cs              # xUnit test cases
    └── GodotConfigFileTests.csproj     # Test project (net8.0)
```

## Features

- ✅ Type-safe value setting and getting
- ✅ Support for sections
- ✅ Check existence of keys and sections
- ✅ Save to and load from files
- ✅ Parse from string and encode to text
- ✅ Erase keys and sections
- ✅ Clear all data
- ✅ Default values support
- ✅ .NET Standard 2.0 compatible
- ✅ Independent library (no Godot dependency)
- ✅ Comprehensive xUnit tests
- ✅ Well-formatted config file parsing
- ✅ Comment handling support
- ✅ Godot ConfigFile compatibility
- ✅ Multi-line string support
- ✅ Quoted key support
- ✅ Array support
- ✅ Dictionary support

## Namespace

```csharp
using GodotConfigFile;
```

## Usage

### Basic Usage

```csharp
using GodotConfigFile;

// Create a new ConfigFile instance
var config = new ConfigFile();

// Set values in custom sections
config.SetValue<string>("player", "name", "Unnamed Player");
config.SetValue<int>("player", "health", 100);
config.SetValue<bool>("graphics", "antialiasing", true);
config.SetValue<string>("graphics", "quality", "high");

// Get values with type safety
string name = config.GetValue<string>("player", "name");
int health = config.GetValue<int>("player", "health");
bool antialiasing = config.GetValue<bool>("graphics", "antialiasing");
string quality = config.GetValue<string>("graphics", "quality");
```

### Save and Load

```csharp
using GodotConfigFile;
using System.IO;

var config = new ConfigFile();
config.SetValue<string>("player", "name", "Unnamed Player");
config.SetValue<bool>("graphics", "antialiasing", true);

// Save to file
config.Save("config.cfg");

// Load from file
var loadedConfig = new ConfigFile();
loadedConfig.Load("config.cfg");
```

### Parse and Encode to Text

```csharp
using GodotConfigFile;

var config = new ConfigFile();

// Parse from string
config.Parse(@"[player]
name=\"Unnamed Player\"
health=100

[graphics]
antialiasing=true
quality=high");

// Encode to text
string configText = config.EncodeToText();

// Use the encoded text...
```

### Check Existence

```csharp
using GodotConfigFile;

var config = new ConfigFile();
config.SetValue<string>("player", "name", "Unnamed Player");
config.SetValue<int>("player", "health", 100);

// Check if a section exists
bool hasPlayerSection = config.HasSection("player");
bool hasGraphicsSection = config.HasSection("graphics");

// Check if a key exists in a section
bool hasName = config.HasSectionKey("player", "name");
bool hasHealth = config.HasSectionKey("player", "health");
bool hasQuality = config.HasSectionKey("player", "quality");  // False
```

### Erase and Clear

```csharp
using GodotConfigFile;

var config = new ConfigFile();
config.SetValue<string>("player", "name", "Unnamed Player");
config.SetValue<int>("player", "health", 100);
config.SetValue<bool>("graphics", "antialiasing", true);

// Erase a key in a section
config.EraseSectionKey("player", "health");

// Erase a section
config.EraseSection("graphics");

// Clear all data
config.Clear();
```

### Get All Sections and Keys

```csharp
using GodotConfigFile;

var config = new ConfigFile();
config.SetValue<string>("player", "name", "Unnamed Player");
config.SetValue<int>("player", "health", 100);
config.SetValue<bool>("graphics", "antialiasing", true);
config.SetValue<string>("graphics", "quality", "high");

// Get all sections
var sections = config.GetSections();  // ["player", "graphics"]

// Get all keys in a section
var playerKeys = config.GetSectionKeys("player");  // ["name", "health"]
var graphicsKeys = config.GetSectionKeys("graphics");  // ["antialiasing", "quality"]
```

## Installation

1. Add the `ConfigFile.cs` to your project
2. Ensure your project targets .NET Standard 2.0 or later
3. Include the `GodotConfig` namespace in your code

## File Format

The config file format is compatible with Godot's `.cfg` files:

```ini
name=Godot
version=3.4
is_stable=true
downloads=1000000

[window]
width=1920
height=1080

[graphics]
quality=high
vsync=true
```

## Build

### Build the Library

```bash
# Build the main library project
dotnet build GodotConfigFile/GodotConfigFile.csproj
# or build all projects in the directory
dotnet build
```

### Run Tests

```bash
# Run the comprehensive xUnit tests
dotnet test --project GodotConfigFileTests/GodotConfigFileTests.csproj

# Run tests with coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov /p:CoverletOutput=./lcov.info
```

## License

MIT License - See LICENSE.txt for details

## Code Examples

### Creating and Using ConfigFile

```csharp
using GodotConfigFile;
using System;

var config = new ConfigFile();

// Set values
config.SetValue<string>("game", "title", "My Game");
config.SetValue<string>("game", "version", "1.0.0");
config.SetValue<float>("settings", "volume", 0.8f);
config.SetValue<bool>("settings", "fullscreen", true);

// Get values
string title = config.GetValue<string>("game", "title");
string version = config.GetValue<string>("game", "version");
float volume = config.GetValue<float>("settings", "volume", 1.0f); // With default
bool fullscreen = config.GetValue<bool>("settings", "fullscreen");

Console.WriteLine($"Game: {title} {version}");
Console.WriteLine($"Settings: Volume={volume}, Fullscreen={fullscreen}");
```

### Saving and Loading

```csharp
using GodotConfigFile;

var config = new ConfigFile();
config.SetValue<string>("app", "name", "ConfigTest");
config.SetValue<float>("app", "version", 1.2f);

// Save to file
config.Save("app_config.cfg");

// Load from file
var loaded = new ConfigFile();
loaded.Load("app_config.cfg");

string name = loaded.GetValue<string>("app", "name");
float version = loaded.GetValue<float>("app", "version");

Console.WriteLine($"Loaded: {name} v{version}");
```

## Contributing

Feel free to submit issues, fork the repository and send pull requests.

## History

This library is a C# implementation of Godot Engine's `config_file.cpp`, designed to be used as an independent library without any Godot dependencies.

## Compatibility with Godot

This library is designed to be fully compatible with Godot Engine's `config_file.cpp` implementation. It:

- Follows Godot's config file format exactly
- Supports all Godot config file features
- Produces files that can be read by Godot
- Can read files produced by Godot
- Has a similar API to Godot's ConfigFile

### API Comparison

| Godot C++ Method | C# Method |
|------------------|-----------|
| `setValue(section, key, value)` | `SetValue<T>(string section, string key, T value)` |
| `getValue(section, key, default)` | `GetValue<T>(string section, string key, T defaultValue = default)` |
| `has_section(section)` | `HasSection(string section)` |
| `has_section_key(section, key)` | `HasSectionKey(string section, string key)` |
| `erase_section(section)` | `EraseSection(string section)` |
| `erase_section_key(section, key)` | `EraseSectionKey(string section, string key)` |
| `get_sections()` | `GetSections()` |
| `get_section_keys(section)` | `GetSectionKeys(string section)` |
| `clear()` | `Clear()` |
| `load(path)` | `Load(string path)` |
| `parse(data)` | `Parse(string data)` |
| `save(path)` | `Save(string path)` |
| `encode_to_text()` | `EncodeToText()` |
