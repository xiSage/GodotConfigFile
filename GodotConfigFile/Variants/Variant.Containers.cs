namespace GodotConfigFile;

// Container and opaque-value nodes.
public abstract partial class Variant
{
    /// <summary>An ordered array, optionally typed.</summary>
    /// <param name="items">The elements, in source order.</param>
    /// <param name="elementType">The declared element type, or <see langword="null"/> when the array is untyped.</param>
    public sealed class Array(IReadOnlyList<Variant> items, VariantType? elementType = null) : Variant
    {
        /// <summary>Gets the elements, in source order.</summary>
        public IReadOnlyList<Variant> Items { get; } = items;

        /// <summary>Gets the declared element type, or <see langword="null"/> when the array is untyped.</summary>
        public VariantType? ElementType { get; } = elementType;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Array;
    }

    /// <summary>An ordered dictionary whose keys are arbitrary values.</summary>
    /// <param name="items">The entries, in source order.</param>
    /// <param name="keyType">The declared key type, or <see langword="null"/> when the dictionary is untyped.</param>
    /// <param name="valueType">The declared value type, or <see langword="null"/> when the dictionary is untyped.</param>
    public sealed class Dictionary(
        VariantDictionary items,
        VariantType? keyType = null,
        VariantType? valueType = null) : Variant
    {
        /// <summary>Gets the entries, in source order.</summary>
        public VariantDictionary Items { get; } = items;

        /// <summary>Gets the declared key type, or <see langword="null"/> when the dictionary is untyped.</summary>
        public VariantType? KeyType { get; } = keyType;

        /// <summary>Gets the declared value type, or <see langword="null"/> when the dictionary is untyped.</summary>
        public VariantType? ValueType { get; } = valueType;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Dictionary;
    }

    /// <summary>A packed array of bytes.</summary>
    /// <param name="items">The bytes.</param>
    public sealed class PackedByteArray(IReadOnlyList<byte> items) : Variant
    {
        /// <summary>Gets the bytes.</summary>
        public IReadOnlyList<byte> Items { get; } = items;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.PackedByteArray;
    }

    /// <summary>A packed array of 32-bit integers.</summary>
    /// <param name="items">The elements.</param>
    public sealed class PackedInt32Array(IReadOnlyList<int> items) : Variant
    {
        /// <summary>Gets the elements.</summary>
        public IReadOnlyList<int> Items { get; } = items;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.PackedInt32Array;
    }

    /// <summary>A packed array of 64-bit integers.</summary>
    /// <param name="items">The elements.</param>
    public sealed class PackedInt64Array(IReadOnlyList<long> items) : Variant
    {
        /// <summary>Gets the elements.</summary>
        public IReadOnlyList<long> Items { get; } = items;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.PackedInt64Array;
    }

    /// <summary>A packed array of 32-bit floats.</summary>
    /// <param name="items">The elements.</param>
    public sealed class PackedFloat32Array(IReadOnlyList<float> items) : Variant
    {
        /// <summary>Gets the elements.</summary>
        public IReadOnlyList<float> Items { get; } = items;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.PackedFloat32Array;
    }

    /// <summary>A packed array of 64-bit floats.</summary>
    /// <param name="items">The elements.</param>
    public sealed class PackedFloat64Array(IReadOnlyList<double> items) : Variant
    {
        /// <summary>Gets the elements.</summary>
        public IReadOnlyList<double> Items { get; } = items;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.PackedFloat64Array;
    }

    /// <summary>A packed array of strings.</summary>
    /// <param name="items">The elements.</param>
    public sealed class PackedStringArray(IReadOnlyList<string> items) : Variant
    {
        /// <summary>Gets the elements.</summary>
        public IReadOnlyList<string> Items { get; } = items;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.PackedStringArray;
    }

    /// <summary>A packed array of 2D vectors.</summary>
    /// <param name="items">The elements.</param>
    public sealed class PackedVector2Array(IReadOnlyList<GodotConfigFile.Vector2> items) : Variant
    {
        /// <summary>Gets the elements.</summary>
        public IReadOnlyList<GodotConfigFile.Vector2> Items { get; } = items;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.PackedVector2Array;
    }

    /// <summary>A packed array of 3D vectors.</summary>
    /// <param name="items">The elements.</param>
    public sealed class PackedVector3Array(IReadOnlyList<GodotConfigFile.Vector3> items) : Variant
    {
        /// <summary>Gets the elements.</summary>
        public IReadOnlyList<GodotConfigFile.Vector3> Items { get; } = items;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.PackedVector3Array;
    }

    /// <summary>A packed array of 4D vectors.</summary>
    /// <param name="items">The elements.</param>
    public sealed class PackedVector4Array(IReadOnlyList<GodotConfigFile.Vector4> items) : Variant
    {
        /// <summary>Gets the elements.</summary>
        public IReadOnlyList<GodotConfigFile.Vector4> Items { get; } = items;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.PackedVector4Array;
    }

    /// <summary>A packed array of colours.</summary>
    /// <param name="items">The elements.</param>
    public sealed class PackedColorArray(IReadOnlyList<GodotConfigFile.Color> items) : Variant
    {
        /// <summary>Gets the elements.</summary>
        public IReadOnlyList<GodotConfigFile.Color> Items { get; } = items;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.PackedColorArray;
    }

    /// <summary>
    /// An <c>Object(Type, ...)</c> literal, kept opaque (deviation D7).
    /// </summary>
    /// <param name="className">The class name as written.</param>
    /// <param name="properties">The property assignments, in source order.</param>
    /// <remarks>
    /// Godot instantiates the class through <c>ClassDB</c> and fails the whole parse when the type is unknown.
    /// This library has no Godot runtime, so it records what the literal said and never fails on it.
    /// </remarks>
    public sealed class Object(string className, IReadOnlyList<KeyValuePair<string, Variant>> properties) : Variant
    {
        /// <summary>Gets the class name as written.</summary>
        public string ClassName { get; } = className;

        /// <summary>Gets the property assignments, in source order.</summary>
        public IReadOnlyList<KeyValuePair<string, Variant>> Properties { get; } = properties;

        /// <inheritdoc />
        /// <remarks>
        /// <see cref="Variant.Resource"/> shares this kind, because Godot has no separate variant type for
        /// resource references; match on the node class to tell them apart.
        /// </remarks>
        public override VariantType Kind => VariantType.Object;
    }

    /// <summary>
    /// A <c>Resource(...)</c>, <c>SubResource(...)</c> or <c>ExtResource(...)</c> literal, kept opaque (deviation D7).
    /// </summary>
    /// <param name="keyword">Which of the three spellings was used.</param>
    /// <param name="uid">The <c>uid://</c> reference, when the literal carried one.</param>
    /// <param name="path">The <c>res://</c> path, when the literal carried one.</param>
    /// <remarks>
    /// Godot loads the resource through <c>ResourceLoader</c> and fails the parse when it cannot be found, which
    /// is why <c>ExtResource(...)</c> is unusable in a plain <c>ConfigFile</c>. This library records the
    /// reference instead of resolving it.
    /// </remarks>
    public sealed class Resource(string keyword, string? uid, string? path) : Variant
    {
        /// <summary>Gets which spelling was used: <c>Resource</c>, <c>SubResource</c> or <c>ExtResource</c>.</summary>
        public string Keyword { get; } = keyword;

        /// <summary>Gets the <c>uid://</c> reference, when present.</summary>
        public string? Uid { get; } = uid;

        /// <summary>Gets the resource path, when present.</summary>
        public string? Path { get; } = path;

        /// <inheritdoc />
        /// <remarks>
        /// Shar ed with <see cref="Variant.Object"/>, as in Godot; match on the node class to tell them apart.
        /// </remarks>
        public override VariantType Kind => VariantType.Object;
    }
}
