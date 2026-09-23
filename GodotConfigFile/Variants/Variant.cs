namespace GodotConfigFile;

/// <summary>
/// A value parsed from a Godot configuration file.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Variant"/> is an immutable hierarchy: every value kind is a nested sealed class, so values are
/// inspected with pattern matching (<c>value is Variant.Int i</c>). The node classes deliberately live inside
/// <see cref="Variant"/>, which is why their geometry payloads have to be written fully qualified (for example
/// <c>Variant.Color</c> holds a <see cref="GodotConfigFile.Color"/>).
/// </para>
/// <para>
/// Equality is type-sensitive and structural: <see cref="Int"/> 1 and <see cref="Float"/> 1.0 are different
/// values, containers compare by content, and NaN is equal to NaN. This matches Godot's key-comparison
/// semantics rather than the numeric promotion GDScript performs for <c>==</c> (deviation D12).
/// </para>
/// <para>
/// Dictionary *keys* use a slightly wider rule — String and StringName with the same text are the same key —
/// which is why <see cref="VariantComparer"/> exists next to this class.
/// </para>
/// </remarks>
public abstract partial class Variant : IEquatable<Variant>
{
    /// <summary>Initializes a new instance of the <see cref="Variant"/> class.</summary>
    private protected Variant()
    {
    }

    /// <summary>Gets which kind of value this is.</summary>
    public abstract VariantType Kind { get; }

    /// <summary>Determines whether two values are equal, using the rules described on the type.</summary>
    /// <param name="other">The value to compare with.</param>
    /// <returns><see langword="true"/> when the two values are equal.</returns>
    public bool Equals(Variant? other) => other is not null && VariantEquality.Equals(this, other);

    /// <inheritdoc />
    public sealed override bool Equals(object? obj) => obj is Variant other && VariantEquality.Equals(this, other);

    /// <inheritdoc />
    public sealed override int GetHashCode() => VariantEquality.GetHashCode(this);

    /// <summary>Determines whether two values are equal.</summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true"/> when the two values are equal.</returns>
    public static bool operator ==(Variant? left, Variant? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two values are different.</summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true"/> when the two values are different.</returns>
    public static bool operator !=(Variant? left, Variant? right) => !(left == right);

    /// <summary>
    /// Renders the value for debugging.
    /// </summary>
    /// <returns>A Godot-flavoured representation.</returns>
    /// <remarks>
    /// This library does not implement serialization. The text returned here is for logs and test failures only:
    /// it is not a writer, it is not guaranteed to be stable, and it is not guaranteed to parse back.
    /// </remarks>
    public override string ToString() => VariantDebug.Format(this);
}
