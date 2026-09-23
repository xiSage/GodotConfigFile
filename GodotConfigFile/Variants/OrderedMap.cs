namespace GodotConfigFile;

/// <summary>
/// An insertion-ordered map: a key keeps the position of its first insertion, and assigning an existing key
/// replaces its value in place.
/// </summary>
/// <remarks>
/// Godot's <c>HashMap</c> behaves this way, and all three ordered maps in this library — sections, key names and
/// dictionary entries — depend on it, so the rule lives here once instead of three times. Removal re-indexes the
/// entries that followed, which is fine because removals only happen while a document is being assembled.
/// </remarks>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
internal sealed class OrderedMap<TKey, TValue>
    where TKey : notnull
{
    private readonly IEqualityComparer<TKey> _comparer;
    private readonly List<KeyValuePair<TKey, TValue>> _entries = [];
    private readonly Dictionary<TKey, int> _index;

    /// <summary>Initializes an empty map.</summary>
    /// <param name="comparer">How keys are compared.</param>
    public OrderedMap(IEqualityComparer<TKey> comparer)
    {
        _comparer = comparer;
        _index = new Dictionary<TKey, int>(comparer);
    }

    /// <summary>Gets the number of entries.</summary>
    public int Count => _entries.Count;

    /// <summary>Gets the entries, in insertion order.</summary>
    public IReadOnlyList<KeyValuePair<TKey, TValue>> Entries => _entries;

    /// <summary>Determines whether a key is present.</summary>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> when the key is present.</returns>
    public bool ContainsKey(TKey key) => _index.ContainsKey(key);

    /// <summary>Looks up a value.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The stored value, or the default when the key is absent.</param>
    /// <returns><see langword="true"/> when the key is present.</returns>
    public bool TryGetValue(TKey key, out TValue value)
    {
        if (_index.TryGetValue(key, out int position))
        {
            value = _entries[position].Value;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>Adds a key at the end, or replaces the value of an existing one in place.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when an existing key was replaced.</returns>
    public bool Set(TKey key, TValue value) => SetAt(key, value, front: false);

    /// <summary>Adds a new key at the front, or replaces the value of an existing one in place.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true"/> when an existing key was replaced.</returns>
    public bool SetFirst(TKey key, TValue value) => SetAt(key, value, front: true);

    /// <summary>Removes a key.</summary>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> when the key was present.</returns>
    public bool Remove(TKey key)
    {
        if (!_index.TryGetValue(key, out int position))
        {
            return false;
        }

        _index.Remove(key);
        _entries.RemoveAt(position);

        for (int i = position; i < _entries.Count; i++)
        {
            _index[_entries[i].Key] = i;
        }

        return true;
    }

    /// <summary>Returns a copy that shares no mutable state with this map.</summary>
    /// <returns>The copy.</returns>
    public OrderedMap<TKey, TValue> Clone()
    {
        OrderedMap<TKey, TValue> copy = new(_comparer);
        foreach (KeyValuePair<TKey, TValue> entry in _entries)
        {
            copy._entries.Add(entry);
        }

        for (int i = 0; i < copy._entries.Count; i++)
        {
            copy._index[copy._entries[i].Key] = i;
        }

        return copy;
    }

    private bool SetAt(TKey key, TValue value, bool front)
    {
        if (_index.TryGetValue(key, out int position))
        {
            _entries[position] = new KeyValuePair<TKey, TValue>(key, value);
            return true;
        }

        KeyValuePair<TKey, TValue> entry = new(key, value);

        if (front)
        {
            _entries.Insert(0, entry);
            for (int i = 0; i < _entries.Count; i++)
            {
                _index[_entries[i].Key] = i;
            }
        }
        else
        {
            _index[key] = _entries.Count;
            _entries.Add(entry);
        }

        return false;
    }
}
