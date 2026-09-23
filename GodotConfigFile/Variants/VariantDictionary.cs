using System.Collections;

namespace GodotConfigFile;

/// <summary>
/// An insertion-ordered dictionary with <see cref="Variant"/> keys, using Godot's key semantics.
/// </summary>
/// <remarks>
/// <para>
/// Keys are compared with <see cref="VariantComparer"/>, so String and StringName with the same text are one
/// key. Assigning an existing key replaces the value <em>in place</em>: the entry keeps the position of its
/// first occurrence, which is what Godot's ordered hash map does.
/// </para>
/// <para>
/// This type deliberately does not implement <see cref="IReadOnlyDictionary{TKey,TValue}"/>. That interface
/// implies lookups agree with <c>Equals</c>/<c>GetHashCode</c>, and here they deliberately do not — that is the
/// whole point of <see cref="VariantComparer"/>.
/// </para>
/// </remarks>
public sealed class VariantDictionary : IReadOnlyCollection<KeyValuePair<Variant, Variant>>
{
    private readonly OrderedMap<Variant, Variant> _entries = new(VariantComparer.Instance);
    private IReadOnlyList<Variant>? _keys;
    private IReadOnlyList<Variant>? _values;

    /// <summary>Initializes an empty dictionary.</summary>
    public VariantDictionary()
    {
    }

    /// <summary>Initializes a dictionary from a sequence of entries, keeping their order.</summary>
    /// <param name="entries">The entries to add.</param>
    /// <remarks>Later duplicates overwrite earlier ones without moving them, as in Godot.</remarks>
    public VariantDictionary(IEnumerable<KeyValuePair<Variant, Variant>> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        foreach (KeyValuePair<Variant, Variant> entry in entries)
        {
            Set(entry.Key, entry.Value);
        }
    }

    /// <summary>Gets the number of entries.</summary>
    public int Count => _entries.Count;

    /// <summary>Gets the keys, in insertion order.</summary>
    public IReadOnlyList<Variant> Keys => _keys ??= [.. _entries.Entries.Select(entry => entry.Key)];

    /// <summary>Gets the values, in insertion order.</summary>
    public IReadOnlyList<Variant> Values => _values ??= [.. _entries.Entries.Select(entry => entry.Value)];

    /// <summary>Gets the value stored under <paramref name="key"/>.</summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>The stored value.</returns>
    /// <exception cref="KeyNotFoundException">The key is not present.</exception>
    public Variant this[Variant key]
    {
        get
        {
            if (TryGetValue(key, out Variant value))
            {
                return value;
            }

            throw new KeyNotFoundException($"The dictionary has no entry for the key {key}.");
        }
    }

    /// <summary>Determines whether the dictionary contains <paramref name="key"/>.</summary>
    /// <param name="key">The key to look up.</param>
    /// <returns><see langword="true"/> when the key is present.</returns>
    public bool ContainsKey(Variant key) => _entries.ContainsKey(key);

    /// <summary>Looks up a value.</summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">The stored value, or <see cref="Variant.Nil.Instance"/> when the key is missing.</param>
    /// <returns><see langword="true"/> when the key is present.</returns>
    public bool TryGetValue(Variant key, out Variant value)
    {
        if (_entries.TryGetValue(key, out Variant? found))
        {
            value = found;
            return true;
        }

        value = Variant.Nil.Instance;
        return false;
    }

    /// <summary>Returns an enumerator over the entries, in insertion order.</summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<KeyValuePair<Variant, Variant>> GetEnumerator() => _entries.Entries.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Adds or replaces an entry while the parser is building the dictionary.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    internal void Set(Variant key, Variant value)
    {
        if (_entries.Set(key, value))
        {
            // The key kept its position, but the cached values no longer describe the dictionary.
            _values = null;
            return;
        }

        _keys = null;
        _values = null;
    }
}
