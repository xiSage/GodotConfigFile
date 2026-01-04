// Copyright (c) 2026 xiSage
// MIT License
// https://github.com/xiSage/GodotConfigFile

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

        // Methods to check existence
        public bool HasSection(string section)
        {
            if (string.IsNullOrEmpty(section))
                section = DEFAULT_SECTION;

            return _data.ContainsKey(section);
        }

        public bool HasSectionKey(string section, string key)
        {
            if (string.IsNullOrEmpty(section))
                section = DEFAULT_SECTION;

            return _data.TryGetValue(section, out var sectionDict) && sectionDict.ContainsKey(key);
        }

        // Methods to remove sections and keys
        public void EraseSection(string section)
        {
            if (string.IsNullOrEmpty(section))
                section = DEFAULT_SECTION;

            _data.Remove(section);
        }

        public void EraseSectionKey(string section, string key)
        {
            if (string.IsNullOrEmpty(section))
                section = DEFAULT_SECTION;

            if (_data.TryGetValue(section, out var sectionDict))
            {
                sectionDict.Remove(key);
                if (sectionDict.Count == 0)
                {
                    _data.Remove(section);
                }
            }
        }

        // Methods to get all sections and keys
        public List<string> GetSections()
        {
            return new List<string>(_data.Keys);
        }

        public List<string> GetSectionKeys(string section)
        {
            if (string.IsNullOrEmpty(section))
                section = DEFAULT_SECTION;

            if (_data.TryGetValue(section, out var sectionDict))
                return new List<string>(sectionDict.Keys);

            return new List<string>();
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
                Parse(reader.ReadToEnd());
            }
        }

        // Parse from string
        public void Parse(string data)
        {
            Clear();
            string currentSection = DEFAULT_SECTION;
            
            // First, replace all CRLF with LF to ensure consistent line endings
            data = data.Replace("\r\n", "\n");
            
            // Split the data into lines, preserving empty lines and comments
            string[] lines = data.Split('\n');
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd();
                
                // Skip empty lines and comments
                if (string.IsNullOrEmpty(line) || line.StartsWith(";"))
                {
                    continue;
                }
                
                // Check for section header
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2).Trim();
                    continue;
                }
                
                // Check for key-value pair
                // Parse the line into key and value, handling quoted keys
                string key = string.Empty;
                string valueStr = string.Empty;
                int equalsIndex = -1;
                
                // Skip leading whitespace
                int startIndex = 0;
                while (startIndex < line.Length && char.IsWhiteSpace(line[startIndex]))
                {
                    startIndex++;
                }
                
                if (startIndex < line.Length)
                {
                    if (line[startIndex] == '"')
                    {
                        // Quoted key
                        int keyStart = startIndex + 1;
                        int keyEnd = -1;
                        
                        for (int j = keyStart; j < line.Length; j++)
                        {
                            if (line[j] == '"')
                            {
                                // Check if the quote is escaped
                                bool isEscaped = false;
                                for (int k = j - 1; k >= 0 && line[k] == '\\'; k--)
                                {
                                    isEscaped = !isEscaped;
                                }
                                if (!isEscaped)
                                {
                                    keyEnd = j;
                                    break;
                                }
                            }
                        }
                        
                        if (keyEnd != -1)
                        {
                            // Extract the quoted key (without quotes)
                            string quotedKey = line.Substring(keyStart, keyEnd - keyStart);
                            // Unescape the key
                            quotedKey = quotedKey.Replace("\\\"", "\"")
                                               .Replace("\\\\", "\\")
                                               .Replace("\\n", "\n")
                                               .Replace("\\r", "\r")
                                               .Replace("\\t", "\t");
                            key = quotedKey;
                            
                            // Find the equals sign after the key
                            equalsIndex = line.IndexOf('=', keyEnd + 1);
                        }
                    }
                    else
                    {
                        // Unquoted key
                        // Find the first equals sign that's not inside quotes
                        bool inQuotes = false;
                        for (int j = startIndex; j < line.Length; j++)
                        {
                            char c = line[j];
                            if (c == '"')
                            {
                                // Check if the quote is escaped
                                bool isEscaped = false;
                                for (int k = j - 1; k >= 0 && line[k] == '\\'; k--)
                                {
                                    isEscaped = !isEscaped;
                                }
                                if (!isEscaped)
                                {
                                    inQuotes = !inQuotes;
                                }
                            }
                            else if (c == '=' && !inQuotes)
                            {
                                equalsIndex = j;
                                break;
                            }
                        }
                        
                        if (equalsIndex != -1)
                        {
                            key = line.Substring(startIndex, equalsIndex - startIndex).Trim();
                        }
                    }
                    
                    if (equalsIndex != -1)
                    {
                        // Extract the value part
                        valueStr = line.Substring(equalsIndex + 1).Trim();
                        
                        // Remove comments from value string
                        int commentIndex = valueStr.IndexOf(';');
                        if (commentIndex != -1)
                        {
                            valueStr = valueStr.Substring(0, commentIndex).Trim();
                        }
                        
                        // Check if this is a multi-line value
                        bool isMultiLineValue = false;
                        string multiLineEndMarker = string.Empty;
                        
                        // Check for quoted multi-line string
                        if (valueStr.StartsWith("\"") && !valueStr.EndsWith("\""))
                        {
                            isMultiLineValue = true;
                            multiLineEndMarker = "\"";
                        }
                        // Check for Color or Vector2 multi-line values
                        else if ((valueStr.StartsWith("Color(") && !valueStr.EndsWith(")")) || 
                                 (valueStr.StartsWith("Vector2(") && !valueStr.EndsWith(")")) ||
                                 (valueStr.StartsWith("Vector3(") && !valueStr.EndsWith(")")) ||
                                 (valueStr.StartsWith("Rect2(") && !valueStr.EndsWith(")")) ||
                                 (valueStr.StartsWith("Rect3(") && !valueStr.EndsWith(")")))
                        {
                            isMultiLineValue = true;
                            multiLineEndMarker = ")";
                        }
                        
                        if (isMultiLineValue)
                        {
                            // This is a multi-line value, continue reading until we find the end marker
                            StringBuilder multiLineValue = new StringBuilder(valueStr);
                            multiLineValue.Append("\n");
                            
                            // Continue reading lines until we find the end marker
                            for (i = i + 1; i < lines.Length; i++)
                            {
                                string multiLine = lines[i];
                                multiLineValue.Append(multiLine);
                                
                                if (multiLineEndMarker == "\"")
                                {
                                    // Check if this line contains the closing quote
                                    bool foundClosingQuote = false;
                                    for (int j = 0; j < multiLine.Length; j++)
                                    {
                                        if (multiLine[j] == '"')
                                        {
                                            // Check if the quote is escaped
                                            bool isEscaped = false;
                                            for (int k = j - 1; k >= 0 && multiLine[k] == '\\'; k--)
                                            {
                                                isEscaped = !isEscaped;
                                            }
                                            if (!isEscaped)
                                            {
                                                foundClosingQuote = true;
                                                break;
                                            }
                                        }
                                    }
                                    
                                    if (foundClosingQuote)
                                    {
                                        break;
                                    }
                                }
                                else if (multiLineEndMarker == ")")
                                {
                                    // Check if this line contains the closing parenthesis
                                    // Count parentheses to find the matching one
                                    int openParens = 0;
                                    int closeParens = 0;
                                    
                                    // Count parentheses in the entire accumulated value
                                    string accumulated = multiLineValue.ToString();
                                    for (int j = 0; j < accumulated.Length; j++)
                                    {
                                        if (accumulated[j] == '(')
                                        {
                                            openParens++;
                                        }
                                        else if (accumulated[j] == ')')
                                        {
                                            closeParens++;
                                        }
                                    }
                                    
                                    // If we have a matching number of open and close parentheses, we're done
                                    if (openParens == closeParens && openParens > 0)
                                    {
                                        break;
                                    }
                                }
                                
                                // Add a newline unless we're at the end of the file
                                if (i < lines.Length - 1)
                                {
                                    multiLineValue.Append("\n");
                                }
                            }
                            
                            valueStr = multiLineValue.ToString();
                        }
                        
                        // Parse the value and set it
                        SetValue(currentSection, key, ParseValue(valueStr));
                    }
                }
            }
        }

        // Save to file
        public void Save(string path)
        {
            File.WriteAllText(path, EncodeToText(), Encoding.UTF8);
        }

        // Encode to text
        public string EncodeToText()
        {
            var sb = new StringBuilder();
            bool firstSection = true;
            
            foreach (var section in _data)
            {
                if (!firstSection)
                {
                    sb.AppendLine();
                }
                firstSection = false;
                
                // Write section header if not default section
                if (section.Key != DEFAULT_SECTION)
                {
                    sb.AppendLine($"[{section.Key}]");
                    sb.AppendLine();
                }

                // Write key-value pairs
                foreach (var keyValue in section.Value)
                {
                    sb.AppendLine($"{FormatKey(keyValue.Key)}={FormatValue(keyValue.Value)}");
                }
            }
            
            return sb.ToString();
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

            // Try to parse as string with quotes
            if (value.StartsWith("\""))
            {
                // Remove quotes and handle escaped characters
                string unquoted = value.Trim('"')
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t")
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\");
                return unquoted;
            }

            // Default to string
            return value;
        }

        private string FormatKey(string key)
        {
            // Quote keys with spaces, newlines, equals signs, or quotes
            if (key.Contains(" ") || key.Contains("\n") || key.Contains("\r") || key.Contains("\t") || key.Contains("=") || key.Contains("\""))
            {
                return $"\"{key.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t")}\"";
            }
            return key;
        }
        
        private string FormatValue(object value)
        {
            if (value == null)
                return string.Empty;
            
            if (value is bool boolValue)
            {
                return boolValue ? "true" : "false";
            }
            
            if (value is string strValue)
            {
                // Quote strings with spaces or newlines
                if (strValue.Contains(" ") || strValue.Contains("\n") || strValue.Contains("\r") || strValue.Contains("\t") || strValue.Contains("\""))
                {
                    return $"\"{strValue.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t")}\"";
                }
                return strValue;
            }
            
            return value.ToString();
        }
    }
}