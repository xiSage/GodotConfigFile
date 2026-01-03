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
- ✅ Save to and load from streams
- ✅ Merge config files
- ✅ Erase keys and sections
- ✅ Clear all data
- ✅ Default values support
- ✅ .NET Standard 2.0 compatible
- ✅ Independent library (no Godot dependency)
- ✅ Comprehensive xUnit tests
- ✅ Well-formatted config file parsing
- ✅ Comment handling support

## Namespace

```csharp
using GodotConfig;
```

## Usage

### Basic Usage

```csharp
using GodotConfig;

// Create a new ConfigFile instance
var config = new ConfigFile();

// Set values in the default section
config.SetValue("name", "Godot");
config.SetValue("version", 3.4f);
config.SetValue("is_stable", true);
config.SetValue("downloads", 1000000L);

// Get values with type safety
string name = config.GetValue<string>("name");
float version = config.GetValue<float>("version");
bool isStable = config.GetValue<bool>("is_stable");
long downloads = config.GetValue<long>("downloads");

// Set values in custom sections
config.SetValue("window", "width", 1920);
config.SetValue("window", "height", 1080);
config.SetValue("graphics", "quality", "high");
config.SetValue("graphics", "vsync", true);

// Get values from sections
int width = config.GetValueInSection<int>("window", "width");
int height = config.GetValueInSection<int>("window", "height");
string quality = config.GetValueInSection<string>("graphics", "quality");
bool vsync = config.GetValueInSection<bool>("graphics", "vsync");
```

### Save and Load

```csharp
using GodotConfig;
using System.IO;
using System.Text;

var config = new ConfigFile();
config.SetValue("name", "Godot");
config.SetValue("window", "width", 1920);

// Save to file
config.Save("config.cfg");

// Load from file
var loadedConfig = new ConfigFile();
loadedConfig.Load("config.cfg");

// Save to stream
using (var stream = new MemoryStream())
using (var writer = new StreamWriter(stream, Encoding.UTF8))
{
    config.SaveToStream(writer);
    writer.Flush();
    // Use the stream...
}

// Load from stream
using (var stream = new MemoryStream())
using (var reader = new StreamReader(stream, Encoding.UTF8))
{
    loadedConfig.LoadFromStream(reader);
    // Use the loaded config...
}
```

### Merge Configs

```csharp
using GodotConfig;

var config1 = new ConfigFile();
config1.SetValue("name", "Godot");
config1.SetValue("version", 3.4f);

var config2 = new ConfigFile();
config2.SetValue("name", "Godot Engine");  // Will overwrite
config2.SetValue("author", "Contributors");  // New key
config2.SetValue("window", "width", 3840);  // Will overwrite

// Merge with overwrite
config1.Merge(config2, overwrite: true);

// Merge without overwrite
config1.Merge(config2, overwrite: false);
```

### Check Existence

```csharp
using GodotConfig;

var config = new ConfigFile();
config.SetValue("name", "Godot");
config.SetValue("window", "width", 1920);

// Check if a key exists
bool hasName = config.HasKey("name");
bool hasWindowWidth = config.HasKey("window", "width");

// Check if a section exists
bool hasWindowSection = config.HasSection("window");
bool hasGraphicsSection = config.HasSection("graphics");
```

### Erase and Clear

```csharp
using GodotConfig;

var config = new ConfigFile();
config.SetValue("name", "Godot");
config.SetValue("window", "width", 1920);
config.SetValue("window", "height", 1080);

// Erase a key
config.EraseKey("name");
config.EraseKey("window", "height");

// Erase a section
config.EraseSection("window");

// Clear all data
config.Clear();
```

### Get All Sections and Keys

```csharp
using GodotConfig;

var config = new ConfigFile();
config.SetValue("name", "Godot");
config.SetValue("window", "width", 1920);
config.SetValue("window", "height", 1080);
config.SetValue("graphics", "quality", "high");

// Get all sections
var sections = config.GetSections();

// Get all keys in a section
var windowKeys = config.GetKeys("window");
var defaultKeys = config.GetKeys();
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
cd GodotConfigFile
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
cd GodotConfigFileTests
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov /p:CoverletOutput=./lcov.info
```

## License

MIT License - See LICENSE.txt for details

## Code Examples

### Creating and Using ConfigFile

```csharp
using GodotConfig;
using System;

var config = new ConfigFile();

// Set values
config.SetValue("game", "title", "My Game");
config.SetValue("game", "version", "1.0.0");
config.SetValue("settings", "volume", 0.8f);
config.SetValue("settings", "fullscreen", true);

// Get values
string title = config.GetValueInSection<string>("game", "title");
string version = config.GetValueInSection<string>("game", "version");
float volume = config.GetValueInSection<float>("settings", "volume", 1.0f); // With default
bool fullscreen = config.GetValueInSection<bool>("settings", "fullscreen");

Console.WriteLine($"Game: {title} {version}");
Console.WriteLine($"Settings: Volume={volume}, Fullscreen={fullscreen}");
```

### Saving and Loading

```csharp
using GodotConfig;

var config = new ConfigFile();
config.SetValue("app", "name", "ConfigTest");
config.SetValue("app", "version", 1.2f);

// Save to file
config.Save("app_config.cfg");

// Load from file
var loaded = new ConfigFile();
loaded.Load("app_config.cfg");

string name = loaded.GetValueInSection<string>("app", "name");
float version = loaded.GetValueInSection<float>("app", "version");

Console.WriteLine($"Loaded: {name} v{version}");
```

## Example Output

```
Testing Godot ConfigFile in C#...
==================================

Test 1: Basic functionality
✓ Set and get values: name=Godot, version=3.4, is_stable=True, downloads=1000000

Test 2: Sections
✓ Set and get values from sections: window.width=1920, window.height=1080, graphics.quality=high, graphics.vsync=True

Test 3: Existence checks
✓ Key existence: hasName=True, hasNonExistent=False
✓ Section existence: hasWindow=True, hasNonExistentSection=False

Test 4: Save and Load
✓ Saved to string: 137 characters
✓ Loaded from string: name=Godot, window.width=1920

Test 5: Merge
✓ Merged config: name=Godot Engine, author=Godot Contributors, window.width=3840, window.vsync=True

Test 6: Erase methods
✓ Erase key: hasVsync=False
✓ Erase section: hasWindowSection=False
✓ Clear config: hasSections=False

Test 7: Default values
✓ Default values: defaultName=default_value, defaultValue=0

==================================
All tests passed! ConfigFile is working correctly.
==================================
```

## Contributing

Feel free to submit issues, fork the repository and send pull requests.

## History

This library is a C# implementation of Godot Engine's `config_file.cpp`, designed to be used as an independent library without any Godot dependencies.

## Differences from Godot's C++ Implementation

1. Uses C# generics for type safety
2. Follows C# naming conventions (PascalCase methods, camelCase parameters)
3. Supports more .NET types by default
4. Uses `TextReader` and `TextWriter` for stream operations
5. All operations are instance-based (no static methods)
6. More intuitive API for working with the default section
7. Independent library (no Godot dependency)
8. Uses modern C# features
9. Renamed `GetValue(section, key)` to `GetValueInSection(section, key)` to avoid method overload ambiguity
10. Improved comment handling (supports #, ;, and // comments)
11. More robust parsing of malformed config files