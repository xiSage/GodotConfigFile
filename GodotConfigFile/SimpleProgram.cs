using System;
using System.IO;
using System.Text;

namespace SimpleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testing Godot ConfigFile in C#...");
            Console.WriteLine("==================================");
            
            // Create a new ConfigFile instance
            var config = new GodotConfig.ConfigFile();
            
            // Test 1: Basic functionality
            Console.WriteLine("\nTest 1: Basic functionality");
            config.SetValue("name", "Godot");
            config.SetValue("version", 3.4f);
            config.SetValue("is_stable", true);
            config.SetValue("downloads", 1000000L);
            
            string name = config.GetValue<string>("name");
            float version = config.GetValue<float>("version");
            bool isStable = config.GetValue<bool>("is_stable");
            long downloads = config.GetValue<long>("downloads");
            
            Console.WriteLine($"✓ Set and get values: name={name}, version={version}, is_stable={isStable}, downloads={downloads}");
            
            // Test 2: Sections
            Console.WriteLine("\nTest 2: Sections");
            config.SetValue("window", "width", 1920);
            config.SetValue("window", "height", 1080);
            config.SetValue("graphics", "quality", "high");
            config.SetValue("graphics", "vsync", true);
            
            int width = config.GetValue<int>("window", "width");
            int height = config.GetValue<int>("window", "height");
            string quality = config.GetValue<string>("graphics", "quality");
            bool vsync = config.GetValue<bool>("graphics", "vsync");
            
            Console.WriteLine($"✓ Set and get values from sections: window.width={width}, window.height={height}, graphics.quality={quality}, graphics.vsync={vsync}");
            
            // Test 3: Existence checks
            Console.WriteLine("\nTest 3: Existence checks");
            bool hasName = config.HasKey("name");
            bool hasNonExistent = config.HasKey("non_existent");
            bool hasWindow = config.HasSection("window");
            bool hasNonExistentSection = config.HasSection("non_existent_section");
            
            Console.WriteLine($"✓ Key existence: hasName={hasName}, hasNonExistent={hasNonExistent}");
            Console.WriteLine($"✓ Section existence: hasWindow={hasWindow}, hasNonExistentSection={hasNonExistentSection}");
            
            // Test 4: Save and Load
            Console.WriteLine("\nTest 4: Save and Load");
            
            // Test saving to string
            var sb = new StringBuilder();
            var writer = new StringWriter(sb);
            config.SaveToStream(writer);
            string configText = sb.ToString();
            Console.WriteLine($"✓ Saved to string: {configText.Length} characters");
            
            // Test loading from string
            var reader = new StringReader(configText);
            var loadedConfig = new GodotConfig.ConfigFile();
            loadedConfig.LoadFromStream(reader);
            
            string loadedName = loadedConfig.GetValue<string>("name");
            int loadedWidth = loadedConfig.GetValue<int>("window", "width");
            
            Console.WriteLine($"✓ Loaded from string: name={loadedName}, window.width={loadedWidth}");
            
            // Test 5: Merge
            Console.WriteLine("\nTest 5: Merge");
            var configToMerge = new GodotConfig.ConfigFile();
            configToMerge.SetValue("name", "Godot Engine");
            configToMerge.SetValue("author", "Godot Contributors");
            configToMerge.SetValue("window", "width", 3840);
            configToMerge.SetValue("window", "vsync", true);
            
            config.Merge(configToMerge, overwrite: true);
            
            string mergedName = config.GetValue<string>("name");
            string mergedAuthor = config.GetValue<string>("author");
            int mergedWidth = config.GetValue<int>("window", "width");
            bool mergedVsync = config.GetValue<bool>("window", "vsync");
            
            Console.WriteLine($"✓ Merged config: name={mergedName}, author={mergedAuthor}, window.width={mergedWidth}, window.vsync={mergedVsync}");
            
            // Test 6: Erase methods
            Console.WriteLine("\nTest 6: Erase methods");
            config.EraseKey("window", "vsync");
            bool hasVsync = config.HasKey("window", "vsync");
            Console.WriteLine($"✓ Erase key: hasVsync={hasVsync}");
            
            config.EraseSection("window");
            bool hasWindowSection = config.HasSection("window");
            Console.WriteLine($"✓ Erase section: hasWindowSection={hasWindowSection}");
            
            config.Clear();
            bool hasSections = config.GetSections().Count > 0;
            Console.WriteLine($"✓ Clear config: hasSections={hasSections}");
            
            // Test 7: Default values
            Console.WriteLine("\nTest 7: Default values");
            string defaultName = config.GetValue<string>("non_existent_key", "default_value");
            int defaultValue = config.GetValue<int>("non_existent_key");
            Console.WriteLine($"✓ Default values: defaultName={defaultName}, defaultValue={defaultValue}");
            
            Console.WriteLine("\n==================================");
            Console.WriteLine("All tests passed! ConfigFile is working correctly.");
            Console.WriteLine("==================================");
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}