namespace GodotConfigFile;

// Scalar and string-like value nodes.
public abstract partial class Variant
{
    /// <summary>The absence of a value, produced by <c>null</c> and <c>nil</c>.</summary>
    /// <remarks>
    /// A top-level <c>key = null</c> normally erases the key instead of storing a <see cref="Nil"/>; see
    /// <see cref="ParseOptions.PreserveNilKeys"/>. Inside arrays and dictionaries a null is kept as
    /// <see cref="Nil"/>, exactly as in Godot.
    /// </remarks>
    public sealed class Nil : Variant
    {
        /// <summary>A shared instance, since <see cref="Nil"/> carries no state.</summary>
        public static Nil Instance { get; } = new();

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Nil;
    }

    /// <summary>A boolean.</summary>
    /// <param name="value">The boolean value.</param>
    public sealed class Bool(bool value) : Variant
    {
        /// <summary>Gets the boolean value.</summary>
        public bool Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Bool;
    }

    /// <summary>A signed 64-bit integer.</summary>
    /// <param name="value">The integer value.</param>
    public sealed class Int(long value) : Variant
    {
        /// <summary>Gets the integer value.</summary>
        public long Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Int;
    }

    /// <summary>A double-precision float. Distinct from <see cref="Int"/>.</summary>
    /// <param name="value">The float value.</param>
    public sealed class Float(double value) : Variant
    {
        /// <summary>Gets the float value.</summary>
        public double Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Float;
    }

    /// <summary>A string. Named <c>Str</c> so that it does not shadow <see cref="string"/> inside the hierarchy.</summary>
    /// <param name="value">The string value.</param>
    public sealed class Str(string value) : Variant
    {
        /// <summary>Gets the string value.</summary>
        public string Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.String;
    }

    /// <summary>An interned string, written <c>&amp;"name"</c> (or the deprecated <c>@"name"</c>).</summary>
    /// <param name="value">The string value.</param>
    public sealed class StringName(string value) : Variant
    {
        /// <summary>Gets the string value.</summary>
        public string Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.StringName;
    }

    /// <summary>A node path, written <c>NodePath("a/b")</c>.</summary>
    /// <param name="value">The path text, stored as written.</param>
    public sealed class NodePath(string value) : Variant
    {
        /// <summary>Gets the path text.</summary>
        public string Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.NodePath;
    }
}
