using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GodotConfig
{
    public class ConfigFile
    {
        private Dictionary<string, Dictionary<string, object>> _data = new Dictionary<string, Dictionary<string, object>>();
        private const string DEFAULT_SECTION = "";

        // Methods to set values
        public void SetValue(string section, string key, object value)
        {
            if (string.IsNullOrEmpty(section))
                section = DEFAULT_SECTION;

            if (!_data.TryGetValue(section, out var sectionDict))
            {
                sectionDict = new Dictionary<string, object>();
                _data[section] = sectionDict;
            }

            sectionDict[key] = value;
        }

        public void SetValue(string key, object value)
        {
            SetValue(DEFAULT_SECTION, key, value);
        }

        // Methods to get values with type safety
        public T GetValue<T>(string section, string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(section))
                section = DEFAULT_SECTION;

            if (_data.TryGetValue(section, out var sectionDict) && sectionDict.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            return GetValue(DEFAULT_SECTION, key, defaultValue);
        }

        // Methods to check existence
        public bool HasSection(string section)
        {
            if (string.IsNullOrEmpty(section))
                section = DEFAULT_SECTION;

            return _data.ContainsKey(section);
        }

        public bool HasKey(string section, string key)
        {
            if (string.IsNullOrEmpty(section))
                section = DEFAULT_SECTION;

            return _data.TryGetValue(section, out var sectionDict) && sectionDict.ContainsKey(key);
        }

        public bool HasKey(string key)
        {
            return HasKey(DEFAULT_SECTION, key);
        }

        // Methods to remove sections and keys
        public void EraseSection(string section)
        {
            if (string.IsNullOrEmpty(section))
                section = DEFAULT_SECTION;

            _data.Remove(section);
        }

        public void EraseKey(string section, string key)
        {
            if (string.IsNullOrEmpty(section))
                section = DEFAULT_SECTION;

            if (_data.TryGetValue(section, out var sectionDict))
                sectionDict.Remove(key);
        }

        public void EraseKey(string key)
        {
            EraseKey(DEFAULT_SECTION, key);
        }

        // Methods to get all sections and keys
        public List<string> GetSections()
        {
            return new List<string>(_data.Keys);
        }

        public List<string> GetKeys(string section)
        {
            if (string.IsNullOrEmpty(section))
                section = DEFAULT_SECTION;

            if (_data.TryGetValue(section, out var sectionDict))
                return new List<string>(sectionDict.Keys);

            return new List<string>();
        }

        public List<string> GetKeys()
        {
            return GetKeys(DEFAULT_SECTION);
        }

        // Clear all data
        public void Clear()
        {
            _data.Clear();
        }

        // Load from file
        public void Load(string path)
        {
            using (var reader = new StreamReader(path, Encoding.UTF8))
            {
                LoadFromStream(reader);
            }
        }

        public void LoadFromStream(TextReader reader)
        {
            Clear();
            string currentSection = DEFAULT_SECTION;
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//"))
                    continue;

                // Check for section header
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2).Trim();
                    continue;
                }

                // Check for key-value pair
                int equalsIndex = line.IndexOf('=');
                if (equalsIndex > 0)
                {
                    string key = line.Substring(0, equalsIndex).Trim();
                    string value = line.Substring(equalsIndex + 1).Trim();

                    SetValue(currentSection, key, ParseValue(value));
                }
            }
        }

        // Save to file
        public void Save(string path)
        {
            using (var writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                SaveToStream(writer);
            }
        }

        public void SaveToStream(TextWriter writer)
        {
            foreach (var section in _data)
            {
                // Write section header if not default section
                if (section.Key != DEFAULT_SECTION)
                    writer.WriteLine($"[{section.Key}]");

                // Write key-value pairs
                foreach (var keyValue in section.Value)
                {
                    writer.WriteLine($"{keyValue.Key}={FormatValue(keyValue.Value)}");
                }

                // Add empty line between sections for readability
                if (section.Key != DEFAULT_SECTION)
                    writer.WriteLine();
            }
        }

        // Merge with another ConfigFile
        public void Merge(ConfigFile other, bool overwrite = false)
        {
            foreach (var section in other._data)
            {
                foreach (var keyValue in section.Value)
                {
                    if (!HasKey(section.Key, keyValue.Key) || overwrite)
                        SetValue(section.Key, keyValue.Key, keyValue.Value);
                }
            }
        }

        // Helper methods
        private object ParseValue(string value)
        {
            // Try to parse as bool
            if (bool.TryParse(value, out var boolValue))
                return boolValue;

            // Try to parse as int
            if (int.TryParse(value, out var intValue))
                return intValue;

            // Try to parse as float
            if (float.TryParse(value, out var floatValue))
                return floatValue;

            // Try to parse as double
            if (double.TryParse(value, out var doubleValue))
                return doubleValue;

            // Try to parse as long
            if (long.TryParse(value, out var longValue))
                return longValue;

            // Try to parse as ulong
            if (ulong.TryParse(value, out var ulongValue))
                return ulongValue;

            // Try to parse as byte
            if (byte.TryParse(value, out var byteValue))
                return byteValue;

            // Try to parse as sbyte
            if (sbyte.TryParse(value, out var sbyteValue))
                return sbyteValue;

            // Try to parse as short
            if (short.TryParse(value, out var shortValue))
                return shortValue;

            // Try to parse as ushort
            if (ushort.TryParse(value, out var ushortValue))
                return ushortValue;

            // Try to parse as uint
            if (uint.TryParse(value, out var uintValue))
                return uintValue;

            // Try to parse as decimal
            if (decimal.TryParse(value, out var decimalValue))
                return decimalValue;

            // Default to string
            return value;
        }

        private string FormatValue(object value)
        {
            return value?.ToString() ?? string.Empty;
        }
    }
}