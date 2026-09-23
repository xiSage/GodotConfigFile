namespace GodotConfigFile;

/// <summary>
/// The comparer Godot uses for dictionary keys.
/// </summary>
/// <remarks>
/// <para>
/// It differs from <see cref="Variant.Equals(Variant?)"/> in exactly one way: a <see cref="Variant.Str"/> and a
/// <see cref="Variant.StringName"/> with the same text are the <em>same</em> key, so <c>{"a": 1, &amp;"a": 2}</c>
/// collapses into a single entry with the value 2 — just like Godot, whose key table uses
/// <c>StringLikeVariantComparator</c>. Everything else stays type-sensitive: <c>1</c> and <c>1.0</c> are two
/// different keys, and a <see cref="Variant.NodePath"/> is never merged with a string.
/// </para>
/// <para>
/// Use this comparer when you build a dictionary of <see cref="Variant"/> keys yourself and want Godot's
/// behaviour. <see cref="VariantDictionary"/> already does.
/// </para>
/// </remarks>
public sealed class VariantComparer : IEqualityComparer<Variant>
{
    /// <summary>Gets the shared instance.</summary>
    public static VariantComparer Instance { get; } = new();

    private VariantComparer()
    {
    }

    /// <summary>Determines whether two values are the same dictionary key.</summary>
    /// <param name="x">The left value.</param>
    /// <param name="y">The right value.</param>
    /// <returns><see langword="true"/> when both values would occupy the same key slot in Godot.</returns>
    public bool Equals(Variant? x, Variant? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        if (x.Kind == y.Kind)
        {
            return VariantEquality.Equals(x, y);
        }

        return (x, y) switch
        {
            (Variant.Str a, Variant.StringName b) => string.Equals(a.Value, b.Value, StringComparison.Ordinal),
            (Variant.StringName a, Variant.Str b) => string.Equals(a.Value, b.Value, StringComparison.Ordinal),
            _ => false,
        };
    }

    /// <summary>Computes a hash code consistent with <see cref="Equals(Variant?, Variant?)"/>.</summary>
    /// <param name="obj">The value to hash.</param>
    /// <returns>The hash code.</returns>
    public int GetHashCode(Variant obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        // String and StringName must hash identically for the merge above to work.
        return obj switch
        {
            Variant.Str s => StringHash(s.Value),
            Variant.StringName s => StringHash(s.Value),
            _ => VariantEquality.GetHashCode(obj),
        };
    }

    /// <summary>
    /// The djb2 variant Godot uses for strings. The absolute values are an implementation detail; what matters
    /// is that <see cref="Variant.Str"/> and <see cref="Variant.StringName"/> share one hash.
    /// </summary>
    private static int StringHash(string text)
    {
        unchecked
        {
            uint hash = 5381;
            foreach (char c in text)
            {
                hash = ((hash << 5) + hash) + c;
            }

            return (int)hash;
        }
    }
}
