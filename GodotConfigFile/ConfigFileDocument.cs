namespace GodotConfigFile;

/// <summary>
/// The result of parsing a Godot configuration file: an immutable, insertion-ordered map of sections.
/// </summary>
/// <remarks>
/// <para>
/// There are no setters. Parsing produces a document; combining documents is done with the pure function
/// <see cref="Merge"/>. Top-level keys (those written before any section header) live in the section named
/// <see cref="ConfigFile.RootSection"/>, which is the empty string, exactly as in Godot.
/// </para>
/// <para>
/// A <c>key = null</c> assignment erases the key, and erases the section too when it becomes empty — Godot's
/// behaviour. Parse with <see cref="ParseOptions.PreserveNilKeys"/> to keep the null instead, which is what
/// <see cref="Merge"/> needs in order to delete a key from the base document.
/// </para>
/// </remarks>
public sealed class ConfigFileDocument
{
    private readonly OrderedMap<string, ConfigSection> _sections = new(StringComparer.Ordinal);
    private IReadOnlyList<string>? _sectionNames;

    /// <summary>Initializes an empty document.</summary>
    public ConfigFileDocument()
    {
    }

    /// <summary>Gets a shared empty document.</summary>
    public static ConfigFileDocument Empty { get; } = new();

    /// <summary>
    /// Gets the section names, in insertion order. <see cref="ConfigFile.RootSection"/> always comes first when it
    /// is present, because Godot inserts the section-less map at the front.
    /// </summary>
    public IReadOnlyList<string> SectionNames => _sectionNames ??= [.. _sections.Entries.Select(entry => entry.Key)];

    /// <summary>Gets the section holding top-level keys, or <see langword="null"/> when the file had none.</summary>
    public ConfigSection? Root => GetSection(ConfigFile.RootSection);

    /// <summary>Determines whether the document has <paramref name="section"/>.</summary>
    /// <param name="section">The section name, or <see cref="ConfigFile.RootSection"/> for top-level keys.</param>
    /// <returns><see langword="true"/> when the section is present.</returns>
    public bool HasSection(string section)
    {
        ArgumentNullException.ThrowIfNull(section);
        return _sections.ContainsKey(section);
    }

    /// <summary>Gets a section.</summary>
    /// <param name="section">The section name, or <see cref="ConfigFile.RootSection"/> for top-level keys.</param>
    /// <returns>The section, or <see langword="null"/> when it is not present.</returns>
    public ConfigSection? GetSection(string section)
    {
        ArgumentNullException.ThrowIfNull(section);
        return _sections.TryGetValue(section, out ConfigSection? found) ? found : null;
    }

    /// <summary>Looks up a value.</summary>
    /// <param name="section">The section name.</param>
    /// <param name="key">The key name.</param>
    /// <param name="value">The stored value, or <see cref="Variant.Nil.Instance"/> when it is absent.</param>
    /// <returns><see langword="true"/> when the section and key are both present.</returns>
    public bool TryGetVariant(string section, string key, out Variant value)
    {
        ConfigSection? found = GetSection(section);
        if (found is not null && found.TryGetVariant(key, out value))
        {
            return true;
        }

        value = Variant.Nil.Instance;
        return false;
    }

    /// <summary>
    /// Reads a value and converts it to <typeparamref name="T"/>, returning <paramref name="defaultValue"/> when the
    /// key is missing or the stored type cannot be converted.
    /// </summary>
    /// <typeparam name="T">The target type. See the README for the list of supported conversions.</typeparam>
    /// <param name="section">The section name.</param>
    /// <param name="key">The key name.</param>
    /// <param name="defaultValue">The value to return when the lookup or the conversion fails.</param>
    /// <returns>The converted value, or <paramref name="defaultValue"/>.</returns>
    /// <remarks>
    /// Conversion is explicit and never goes through strings: a <see cref="Variant.Str"/> holding <c>"5"</c> is not
    /// converted to the number 5. That is deliberate, so that a mistyped value fails loudly instead of silently.
    /// </remarks>
    public T GetValue<T>(string section, string key, T defaultValue = default!)
    {
        return TryGetValue(section, key, out T value) ? value : defaultValue;
    }

    /// <summary>Reads a value and converts it to <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="section">The section name.</param>
    /// <param name="key">The key name.</param>
    /// <param name="value">The converted value, or <see langword="default"/> on failure.</param>
    /// <returns><see langword="true"/> when the key exists and converts to <typeparamref name="T"/>.</returns>
    public bool TryGetValue<T>(string section, string key, out T value)
    {
        if (TryGetVariant(section, key, out Variant stored) &&
            VariantConverter.TryConvert(stored, out T converted))
        {
            value = converted;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Returns a new document with <paramref name="other"/> laid over this one.
    /// </summary>
    /// <param name="other">The document whose keys win.</param>
    /// <returns>A new document; neither input is modified.</returns>
    /// <remarks>
    /// This is Godot's "parse merges into the existing ConfigFile" behaviour, expressed without mutation. A key
    /// already present keeps its original position; new keys are appended. A <see cref="Variant.Nil"/> value in
    /// <paramref name="other"/> deletes the key (and the section, when it becomes empty), which requires
    /// <paramref name="other"/> to have been parsed with <see cref="ParseOptions.PreserveNilKeys"/>.
    /// </remarks>
    public ConfigFileDocument Merge(ConfigFileDocument other)
    {
        ArgumentNullException.ThrowIfNull(other);

        ConfigFileDocument merged = Clone();

        foreach (string sectionName in other.SectionNames)
        {
            ConfigSection section = other.GetSection(sectionName)!;
            foreach (string key in section.KeyNames)
            {
                Variant value = section[key]!;
                if (value.Kind == VariantType.Nil)
                {
                    ConfigSection? target = merged.GetSection(sectionName);
                    if (target is not null && target.Remove(key) && target.Count == 0)
                    {
                        merged.RemoveSection(sectionName);
                    }

                    continue;
                }

                merged.GetOrAddSection(sectionName).Set(key, value);
            }
        }

        return merged;
    }

    /// <summary>Gets an existing section or creates it.</summary>
    /// <param name="section">The section name.</param>
    /// <returns>The section.</returns>
    internal ConfigSection GetOrAddSection(string section)
    {
        if (_sections.TryGetValue(section, out ConfigSection? found))
        {
            return found;
        }

        ConfigSection created = new(section);
        _sectionNames = null;

        // Godot inserts the section-less map at the front, so top-level keys always list first.
        if (section.Length == 0)
        {
            _sections.SetFirst(section, created);
        }
        else
        {
            _sections.Set(section, created);
        }

        return created;
    }

    /// <summary>Removes a section.</summary>
    /// <param name="section">The section name.</param>
    internal void RemoveSection(string section)
    {
        if (_sections.Remove(section))
        {
            _sectionNames = null;
        }
    }

    private ConfigFileDocument Clone()
    {
        ConfigFileDocument copy = new();
        foreach (KeyValuePair<string, ConfigSection> entry in _sections.Entries)
        {
            copy._sections.Set(entry.Key, entry.Value.Clone());
        }

        return copy;
    }
}
