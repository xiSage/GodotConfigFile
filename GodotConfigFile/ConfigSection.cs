namespace GodotConfigFile;

/// <summary>
/// One section of a <see cref="ConfigFileDocument"/>: an insertion-ordered map of key names to values.
/// </summary>
/// <remarks>
/// Key names are case-sensitive and compared ordinally, like Godot's. Assigning a key that is already present
/// replaces the value without moving the key, so the order always reflects first appearance.
/// </remarks>
public sealed class ConfigSection
{
    private readonly OrderedMap<string, Variant> _values = new(StringComparer.Ordinal);
    private IReadOnlyList<string>? _keyNames;

    internal ConfigSection(string name)
    {
        Name = name;
    }

    /// <summary>Gets the section name. <see cref="ConfigFile.RootSection"/> is the section holding top-level keys.</summary>
    public string Name { get; }

    /// <summary>Gets the key names, in insertion order.</summary>
    public IReadOnlyList<string> KeyNames => _keyNames ??= [.. _values.Entries.Select(entry => entry.Key)];

    /// <summary>Gets the number of keys.</summary>
    public int Count => _values.Count;

    /// <summary>Gets the value stored under <paramref name="key"/>.</summary>
    /// <param name="key">The key name.</param>
    /// <returns>The value, or <see langword="null"/> when the key is not present.</returns>
    public Variant? this[string key] => TryGetVariant(key, out Variant value) ? value : null;

    /// <summary>Looks up a value.</summary>
    /// <param name="key">The key name.</param>
    /// <param name="value">The stored value, or <see cref="Variant.Nil.Instance"/> when the key is absent.</param>
    /// <returns><see langword="true"/> when the key is present.</returns>
    public bool TryGetVariant(string key, out Variant value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_values.TryGetValue(key, out Variant? found))
        {
            value = found;
            return true;
        }

        value = Variant.Nil.Instance;
        return false;
    }

    /// <summary>Determines whether the section contains <paramref name="key"/>.</summary>
    /// <param name="key">The key name.</param>
    /// <returns><see langword="true"/> when the key is present.</returns>
    public bool ContainsKey(string key) => _values.ContainsKey(key);

    /// <summary>Adds or replaces a key while a document is being built.</summary>
    /// <param name="key">The key name.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when an existing key was replaced.</returns>
    internal bool Set(string key, Variant value)
    {
        if (_values.Set(key, value))
        {
            return true;
        }

        _keyNames = null;
        return false;
    }

    /// <summary>Removes a key.</summary>
    /// <param name="key">The key name.</param>
    /// <returns><see langword="true"/> when the key was present.</returns>
    internal bool Remove(string key)
    {
        if (!_values.Remove(key))
        {
            return false;
        }

        _keyNames = null;
        return true;
    }

    /// <summary>Returns a copy that shares no mutable state with this section.</summary>
    /// <returns>The copy.</returns>
    internal ConfigSection Clone()
    {
        ConfigSection copy = new(Name);
        foreach (KeyValuePair<string, Variant> entry in _values.Entries)
        {
            copy.Set(entry.Key, entry.Value);
        }

        return copy;
    }
}
