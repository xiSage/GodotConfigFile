// Copyright (c) 2026 xiSage
// MIT License
// https://github.com/xiSage/GodotConfigFile

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GodotConfigFile
{
    /// <summary>
    /// A class for reading and writing Godot-style INI configuration files.
    /// Supports sections, key-value pairs, arrays, dictionaries, and various data types.
    /// </summary>
    public class ConfigFile
    {
        private readonly Dictionary<string, Dictionary<string, object>> _data = new Dictionary<string, Dictionary<string, object>>();
        private const string DEFAULT_SECTION = "";

        ///// <summary>
        ///// Sets a value in the configuration file under the specified section and key.
        ///// </summary>
        ///// <param name="section">The section name. If null or empty, the default section is used.</param>
        ///// <param name="key">The key name.</param>
        ///// <param name="value">The value to set. Can be a primitive type, string, array, or dictionary.</param>
        //public void SetValue(string section, string key, object value)
        //{
        //    SetValue<object>(section, key, value);
        //}

        /// <summary>
        /// Sets a value in the configuration file under the specified section and key.
        /// </summary>
        /// <typeparam name="T">The type of the value being set.</typeparam>
        /// <param name="section">The section name. If null or empty, the default section is used.</param>
        /// <param name="key">The key name.</param>
        /// <param name="value">The value to set. Can be a primitive type, string, array, or dictionary.</param>
        public void SetValue<T>(string section, string key, T value)
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

        /// <summary>
        /// Gets a value from the configuration file with type safety.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="section">The section name. If null or empty, the default section is used.</param>
        /// <param name="key">The key name.</param>
        /// <param name="defaultValue">The default value to return if the key doesn't exist or conversion fails.</param>
        /// <returns>The converted value or the default value if conversion fails.</returns>
        public T GetValue<T>(string section, string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(section))
                section = DEFAULT_SECTION;

            if (_data.TryGetValue(section, out var sectionDict) && sectionDict.TryGetValue(key, out var value))
            {
                try
                {
                    // If T is object, return the raw value directly
                    if (typeof(T) == typeof(object))
                    {
                        return (T)value;
                    }
                    // Handle array types
                    else if (typeof(T).IsArray)
                    {
                        if (value is object[] arrayValue)
                        {
                            Type elementType = typeof(T).GetElementType();
                            Array result = Array.CreateInstance(elementType, arrayValue.Length);
                            for (int i = 0; i < arrayValue.Length; i++)
                            {
                                result.SetValue(ConvertArrayElement(arrayValue[i], elementType), i);
                            }
                            return (T)(object)result;
                        }
                    }
                    // Handle dictionary types
                    else if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    {
                        if (value is SortedDictionary<string, object> sortedDictValue)
                        {
                            Type keyType = typeof(T).GetGenericArguments()[0];
                            Type valueType = typeof(T).GetGenericArguments()[1];
                            object result = Activator.CreateInstance(typeof(T));
                            var addMethod = typeof(T).GetMethod("Add");

                            foreach (var kvp in sortedDictValue)
                            {
                                object convertedKey = Convert.ChangeType(kvp.Key, keyType);
                                object convertedValue = ConvertDictValue(kvp.Value, valueType);
                                _ = addMethod.Invoke(result, new object[] { convertedKey, convertedValue });
                            }

                            return (T)result;
                        }
                    }
                    // Handle other types using Convert.ChangeType
                    else
                    {
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                }
                catch
                {
                    // Fallback to defaultValue if conversion fails
                }
            }

            return defaultValue;
        }

        // Helper method to recursively convert array elements
        private object ConvertArrayElement(object value, Type targetType)
        {
            if (targetType.IsArray && value is object[] arrayValue)
            {
                Type elementType = targetType.GetElementType();
                Array result = Array.CreateInstance(elementType, arrayValue.Length);
                for (int i = 0; i < arrayValue.Length; i++)
                {
                    result.SetValue(ConvertArrayElement(arrayValue[i], elementType), i);
                }
                return result;
            }
            else
            {
                return Convert.ChangeType(value, targetType);
            }
        }

        // Helper method to recursively convert dictionary values
        private object ConvertDictValue(object value, Type targetType)
        {
            // If target type is array and value is object array, convert recursively
            if (targetType.IsArray && value is object[])
            {
                return ConvertArrayElement(value, targetType);
            }
            // If target type is dictionary and value is SortedDictionary, convert recursively
            else if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>) && value is SortedDictionary<string, object> sortedDictValue)
            {
                Type keyType = targetType.GetGenericArguments()[0];
                Type valueType = targetType.GetGenericArguments()[1];
                object result = Activator.CreateInstance(targetType);
                var addMethod = targetType.GetMethod("Add");

                foreach (var kvp in sortedDictValue)
                {
                    object convertedKey = Convert.ChangeType(kvp.Key, keyType);
                    object convertedValue = ConvertDictValue(kvp.Value, valueType);
                    _ = addMethod.Invoke(result, new object[] { convertedKey, convertedValue });
                }

                return result;
            }
            // Otherwise, use direct conversion
            else
            {
                return Convert.ChangeType(value, targetType);
            }
        }

        /// <summary>
        /// Checks if a section exists in the configuration file.
        /// </summary>
        /// <param name="section">The section name. If null or empty, the default section is checked.</param>
        /// <returns>True if the section exists, false otherwise.</returns>
        public bool HasSection(string section)
        {
            if (string.IsNullOrEmpty(section))
                section = DEFAULT_SECTION;

            return _data.ContainsKey(section);
        }

        /// <summary>
        /// Checks if a key exists within a specific section.
        /// </summary>
        /// <param name="section">The section name. If null or empty, the default section is used.</param>
        /// <param name="key">The key name to check.</param>
        /// <returns>True if the key exists in the section, false otherwise.</returns>
        public bool HasSectionKey(string section, string key)
        {
            if (string.IsNullOrEmpty(section))
                section = DEFAULT_SECTION;

            return _data.TryGetValue(section, out var sectionDict) && sectionDict.ContainsKey(key);
        }

        /// <summary>
        /// Removes an entire section from the configuration file.
        /// </summary>
        /// <param name="section">The section name to remove. If null or empty, the default section is removed.</param>
        public void EraseSection(string section)
        {
            if (string.IsNullOrEmpty(section))
                section = DEFAULT_SECTION;

            _ = _data.Remove(section);
        }

        /// <summary>
        /// Removes a specific key from a section.
        /// If the section becomes empty after removing the key, the section is also removed.
        /// </summary>
        /// <param name="section">The section name. If null or empty, the default section is used.</param>
        /// <param name="key">The key name to remove.</param>
        public void EraseSectionKey(string section, string key)
        {
            if (string.IsNullOrEmpty(section))
                section = DEFAULT_SECTION;

            if (_data.TryGetValue(section, out var sectionDict))
            {
                _ = sectionDict.Remove(key);
                if (sectionDict.Count == 0)
                {
                    _ = _data.Remove(section);
                }
            }
        }

        /// <summary>
        /// Gets a list of all section names in the configuration file.
        /// </summary>
        /// <returns>A list containing all section names.</returns>
        public List<string> GetSections()
        {
            return new List<string>(_data.Keys);
        }

        /// <summary>
        /// Gets a list of all keys in the specified section.
        /// </summary>
        /// <param name="section">The section name. If null or empty, the default section is used.</param>
        /// <returns>A list containing all keys in the specified section, or an empty list if the section doesn't exist.</returns>
        public List<string> GetSectionKeys(string section)
        {
            if (string.IsNullOrEmpty(section))
                section = DEFAULT_SECTION;

            return _data.TryGetValue(section, out var sectionDict) ? new List<string>(sectionDict.Keys) : new List<string>();
        }

        /// <summary>
        /// Clears all sections and key-value pairs from the configuration file.
        /// </summary>
        public void Clear()
        {
            _data.Clear();
        }

        /// <summary>
        /// Loads a configuration file from the specified path.
        /// </summary>
        /// <param name="path">The file path to load from.</param>
        public void Load(string path)
        {
            using (var reader = new StreamReader(path, Encoding.UTF8))
            {
                Parse(reader.ReadToEnd());
            }
        }

        /// <summary>
        /// Parses configuration data from a string.
        /// </summary>
        /// <param name="data">The string containing the configuration data.</param>
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
                        string valueStr = line.Substring(equalsIndex + 1).Trim();

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
                        // Check for array or dictionary multi-line values
                        else if (valueStr.StartsWith("[") && !valueStr.EndsWith("]"))
                        {
                            isMultiLineValue = true;
                            multiLineEndMarker = "]";
                        }
                        else if (valueStr.StartsWith("{") && !valueStr.EndsWith("}"))
                        {
                            isMultiLineValue = true;
                            multiLineEndMarker = "}";
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
                            _ = multiLineValue.Append("\n");

                            // Continue reading lines until we find the end marker
                            for (++i; i < lines.Length; i++)
                            {
                                string multiLine = lines[i];
                                _ = multiLineValue.Append(multiLine);

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
                                else if (multiLineEndMarker == "]" || multiLineEndMarker == "}")
                                {
                                    // Check if this line contains the closing bracket or brace
                                    // Count brackets or braces to find the matching one
                                    int openCount = 0;
                                    int closeCount = 0;
                                    char openChar = multiLineEndMarker == "]" ? '[' : '{';
                                    char closeChar = multiLineEndMarker == "]" ? ']' : '}';

                                    // Count brackets in the entire accumulated value
                                    string accumulated = multiLineValue.ToString();
                                    for (int j = 0; j < accumulated.Length; j++)
                                    {
                                        if (accumulated[j] == openChar)
                                        {
                                            openCount++;
                                        }
                                        else if (accumulated[j] == closeChar)
                                        {
                                            closeCount++;
                                        }
                                    }

                                    // If we have a matching number of open and close brackets, we're done
                                    if (openCount == closeCount && openCount > 0)
                                    {
                                        break;
                                    }
                                }

                                // Add a newline unless we're at the end of the file
                                if (i < lines.Length - 1)
                                {
                                    _ = multiLineValue.Append("\n");
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

        /// <summary>
        /// Saves the configuration to a file at the specified path.
        /// </summary>
        /// <param name="path">The file path to save to.</param>
        public void Save(string path)
        {
            File.WriteAllText(path, EncodeToText(), Encoding.UTF8);
        }

        /// <summary>
        /// Encodes the configuration data to a text string in Godot INI format.
        /// </summary>
        /// <returns>A string containing the configuration data in Godot INI format.</returns>
        public string EncodeToText()
        {
            var sb = new StringBuilder();
            bool firstSection = true;

            foreach (var section in _data)
            {
                if (!firstSection)
                {
                    _ = sb.AppendLine();
                }
                firstSection = false;

                // Write section header if not default section
                if (section.Key != DEFAULT_SECTION)
                {
                    _ = sb.AppendLine($"[{section.Key}]");
                    _ = sb.AppendLine();
                }

                // Write key-value pairs
                foreach (var keyValue in section.Value)
                {
                    _ = sb.AppendLine($"{FormatKey(keyValue.Key)}={FormatValue(keyValue.Value)}");
                }
            }

            return sb.ToString();
        }

        // Helper methods
        private object ParseValue(string value)
        {
            string trimmedValue = value.Trim();

            // Try to parse as bool
            if (bool.TryParse(trimmedValue, out var boolValue))
                return boolValue;

            // Try to parse as int
            if (int.TryParse(trimmedValue, out var intValue))
                return intValue;

            // Try to parse as float
            if (float.TryParse(trimmedValue, out var floatValue))
                return floatValue;

            // Try to parse as double
            if (double.TryParse(trimmedValue, out var doubleValue))
                return doubleValue;

            // Try to parse as long
            if (long.TryParse(trimmedValue, out var longValue))
                return longValue;

            // Try to parse as string with quotes
            if (trimmedValue.StartsWith("\""))
            {
                // Remove quotes and handle escaped characters
                string unquoted = trimmedValue.Trim('"')
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t")
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\");
                return unquoted;
            }

            // Try to parse as array
            if (trimmedValue.StartsWith("["))
            {
                return ParseArray(trimmedValue);
            }

            // Try to parse as dictionary
            if (trimmedValue.StartsWith("{"))
            {
                return ParseDictionary(trimmedValue);
            }

            // Default to string
            return trimmedValue;
        }

        private object[] ParseArray(string value)
        {
            // Remove external brackets
            string arrayContent = value.Substring(1, value.Length - 2).Trim();

            // Handle empty array
            if (string.IsNullOrEmpty(arrayContent))
            {
                return Array.Empty<object>();
            }

            // Split into elements, handling commas inside quotes, arrays, and dictionaries
            var elements = new List<string>();
            int depth = 0;
            bool inQuotes = false;
            int startIndex = 0;

            for (int i = 0; i < arrayContent.Length; i++)
            {
                char c = arrayContent[i];

                // Handle quotes
                if (c == '"')
                {
                    // Check if the quote is escaped
                    bool isEscaped = false;
                    for (int j = i - 1; j >= 0 && arrayContent[j] == '\\'; j--)
                    {
                        isEscaped = !isEscaped;
                    }
                    if (!isEscaped)
                    {
                        inQuotes = !inQuotes;
                    }
                }
                // Handle brackets and braces
                else if (!inQuotes)
                {
                    if (c == '[' || c == '{')
                    {
                        depth++;
                    }
                    else if (c == ']' || c == '}')
                    {
                        depth--;
                    }
                    // Handle comma at top level
                    else if (c == ',' && depth == 0)
                    {
                        string element = arrayContent.Substring(startIndex, i - startIndex).Trim();
                        elements.Add(element);
                        startIndex = i + 1;
                    }
                }
            }

            // Add the last element
            string lastElement = arrayContent.Substring(startIndex).Trim();
            elements.Add(lastElement);

            // Parse each element recursively
            var parsedElements = new List<object>();
            foreach (var element in elements)
            {
                parsedElements.Add(ParseValue(element));
            }

            return parsedElements.ToArray();
        }

        private SortedDictionary<string, object> ParseDictionary(string value)
        {
            // Remove external braces
            string dictContent = value.Substring(1, value.Length - 2).Trim();

            // Handle empty dictionary
            if (string.IsNullOrEmpty(dictContent))
            {
                return new SortedDictionary<string, object>();
            }

            // Split into key-value pairs, handling commas inside quotes, arrays, and dictionaries
            var keyValuePairs = new List<string>();
            int depth = 0;
            bool inQuotes = false;
            int startIndex = 0;

            for (int i = 0; i < dictContent.Length; i++)
            {
                char c = dictContent[i];

                // Handle quotes
                if (c == '"')
                {
                    // Check if the quote is escaped
                    bool isEscaped = false;
                    for (int j = i - 1; j >= 0 && dictContent[j] == '\\'; j--)
                    {
                        isEscaped = !isEscaped;
                    }
                    if (!isEscaped)
                    {
                        inQuotes = !inQuotes;
                    }
                }
                // Handle brackets and braces
                else if (!inQuotes)
                {
                    if (c == '[' || c == '{')
                    {
                        depth++;
                    }
                    else if (c == ']' || c == '}')
                    {
                        depth--;
                    }
                    // Handle comma at top level
                    else if (c == ',' && depth == 0)
                    {
                        string pair = dictContent.Substring(startIndex, i - startIndex).Trim();
                        keyValuePairs.Add(pair);
                        startIndex = i + 1;
                    }
                }
            }

            // Add the last key-value pair
            string lastPair = dictContent.Substring(startIndex).Trim();
            keyValuePairs.Add(lastPair);

            // Parse each key-value pair
            var dict = new SortedDictionary<string, object>();
            foreach (var pair in keyValuePairs)
            {
                // Find the separator that separates key and value, at top level
                // Support both colon and equals sign (Lua style)
                int separatorIndex = -1;
                char separator = ':';
                int depth2 = 0;
                bool inQuotes2 = false;

                for (int i = 0; i < pair.Length; i++)
                {
                    char c = pair[i];

                    if (c == '"')
                    {
                        // Check if the quote is escaped
                        bool isEscaped = false;
                        for (int j = i - 1; j >= 0 && pair[j] == '\\'; j--)
                        {
                            isEscaped = !isEscaped;
                        }
                        if (!isEscaped)
                        {
                            inQuotes2 = !inQuotes2;
                        }
                    }
                    else if (!inQuotes2)
                    {
                        if (c == '[' || c == '{')
                        {
                            depth2++;
                        }
                        else if (c == ']' || c == '}')
                        {
                            depth2--;
                        }
                        else if ((c == ':' || c == '=') && depth2 == 0)
                        {
                            separatorIndex = i;
                            separator = c;
                            break;
                        }
                    }
                }

                if (separatorIndex == -1)
                {
                    // Invalid key-value pair, skip
                    continue;
                }

                // Extract key and value
                string key = pair.Substring(0, separatorIndex).Trim();
                string valuePart = pair.Substring(separatorIndex + 1).Trim();

                // Process key based on separator type
                if (separator == ':')
                {
                    // For colon syntax, key must be quoted
                    if (key.StartsWith("\""))
                    {
                        // Remove quotes from quoted key
                        key = key.Substring(1, key.Length - 2)
                            .Replace("\\\"", "\"").Replace("\\\\", "\\")
                            .Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t");
                    }
                    // For colon syntax, unquoted keys are not allowed, skip this pair
                    else
                    {
                        continue;
                    }
                }
                else if (separator == '=')
                {
                    // For equals syntax, key is always treated as an identifier (string)
                    // Remove quotes if present (though not recommended for Lua syntax)
                    if (key.StartsWith("\""))
                    {
                        key = key.Substring(1, key.Length - 2)
                            .Replace("\\\"", "\"").Replace("\\\\", "\\")
                            .Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t");
                    }
                }

                // Parse value recursively
                dict[key] = ParseValue(valuePart);
            }

            return dict;
        }

        private string FormatKey(string key)
        {
            // Quote keys with spaces, newlines, equals signs, or quotes
            return key.Contains(" ") || key.Contains("\n") || key.Contains("\r") || key.Contains("\t") || key.Contains("=") || key.Contains("\"")
                ? $"\"{key.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t")}\""
                : key;
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
                return strValue.Contains(" ") || strValue.Contains("\n") || strValue.Contains("\r") || strValue.Contains("\t") || strValue.Contains("\"")
                    ? $"\"{strValue.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t")}\""
                    : strValue;
            }

            return value is object[] arrayValue
                ? FormatArray(arrayValue)
                : value is SortedDictionary<string, object> dictValue ? FormatDictionary(dictValue) : value.ToString();
        }

        private string FormatArray(object[] array)
        {
            if (array.Length == 0)
            {
                return "[]";
            }

            var elements = new List<string>();
            foreach (var element in array)
            {
                elements.Add(FormatValue(element));
            }

            return "[" + string.Join(", ", elements) + "]";
        }

        private string FormatDictionary(SortedDictionary<string, object> dict)
        {
            if (dict.Count == 0)
            {
                return "{}";
            }

            var pairs = new List<string>();
            foreach (var kvp in dict)
            {
                string key = kvp.Key;
                // Always quote keys for colon syntax output
                key = $"\"{key.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t")}\"";
                string value = FormatValue(kvp.Value);
                pairs.Add($"{key}: {value}");
            }

            return "{ " + string.Join(", ", pairs) + " }";
        }
    }
}