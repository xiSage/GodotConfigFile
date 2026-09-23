namespace GodotConfigFile;

// Geometry, colour and opaque-handle value nodes.
//
// Inside this class the simple names Color/Vector2/... refer to the nested nodes, so the payload types are
// written fully qualified; the payload structs themselves are declared in GeometryTypes.cs.
public abstract partial class Variant
{
    /// <summary>A colour, written either as one of Godot's constructors or as a <c>#rrggbb</c> literal.</summary>
    /// <param name="value">The colour value.</param>
    public sealed class Color(GodotConfigFile.Color value) : Variant
    {
        /// <summary>Gets the colour value.</summary>
        public GodotConfigFile.Color Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Color;
    }

    /// <summary>A 2D float vector.</summary>
    /// <param name="value">The vector value.</param>
    public sealed class Vector2(GodotConfigFile.Vector2 value) : Variant
    {
        /// <summary>Gets the vector value.</summary>
        public GodotConfigFile.Vector2 Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Vector2;
    }

    /// <summary>A 2D integer vector.</summary>
    /// <param name="value">The vector value.</param>
    public sealed class Vector2I(GodotConfigFile.Vector2I value) : Variant
    {
        /// <summary>Gets the vector value.</summary>
        public GodotConfigFile.Vector2I Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Vector2I;
    }

    /// <summary>A 2D rectangle.</summary>
    /// <param name="value">The rectangle value.</param>
    public sealed class Rect2(GodotConfigFile.Rect2 value) : Variant
    {
        /// <summary>Gets the rectangle value.</summary>
        public GodotConfigFile.Rect2 Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Rect2;
    }

    /// <summary>A 2D integer rectangle.</summary>
    /// <param name="value">The rectangle value.</param>
    public sealed class Rect2I(GodotConfigFile.Rect2I value) : Variant
    {
        /// <summary>Gets the rectangle value.</summary>
        public GodotConfigFile.Rect2I Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Rect2I;
    }

    /// <summary>A 3D float vector.</summary>
    /// <param name="value">The vector value.</param>
    public sealed class Vector3(GodotConfigFile.Vector3 value) : Variant
    {
        /// <summary>Gets the vector value.</summary>
        public GodotConfigFile.Vector3 Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Vector3;
    }

    /// <summary>A 3D integer vector.</summary>
    /// <param name="value">The vector value.</param>
    public sealed class Vector3I(GodotConfigFile.Vector3I value) : Variant
    {
        /// <summary>Gets the vector value.</summary>
        public GodotConfigFile.Vector3I Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Vector3I;
    }

    /// <summary>A 4D float vector.</summary>
    /// <param name="value">The vector value.</param>
    public sealed class Vector4(GodotConfigFile.Vector4 value) : Variant
    {
        /// <summary>Gets the vector value.</summary>
        public GodotConfigFile.Vector4 Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Vector4;
    }

    /// <summary>A 4D integer vector.</summary>
    /// <param name="value">The vector value.</param>
    public sealed class Vector4I(GodotConfigFile.Vector4I value) : Variant
    {
        /// <summary>Gets the vector value.</summary>
        public GodotConfigFile.Vector4I Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Vector4I;
    }

    /// <summary>A 2D transform.</summary>
    /// <param name="value">The transform value.</param>
    public sealed class Transform2D(GodotConfigFile.Transform2D value) : Variant
    {
        /// <summary>Gets the transform value.</summary>
        public GodotConfigFile.Transform2D Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Transform2D;
    }

    /// <summary>A plane.</summary>
    /// <param name="value">The plane value.</param>
    public sealed class Plane(GodotConfigFile.Plane value) : Variant
    {
        /// <summary>Gets the plane value.</summary>
        public GodotConfigFile.Plane Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Plane;
    }

    /// <summary>A quaternion.</summary>
    /// <param name="value">The quaternion value.</param>
    public sealed class Quaternion(GodotConfigFile.Quaternion value) : Variant
    {
        /// <summary>Gets the quaternion value.</summary>
        public GodotConfigFile.Quaternion Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Quaternion;
    }

    /// <summary>An axis-aligned bounding box.</summary>
    /// <param name="value">The box value.</param>
    public sealed class Aabb(GodotConfigFile.Aabb value) : Variant
    {
        /// <summary>Gets the box value.</summary>
        public GodotConfigFile.Aabb Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Aabb;
    }

    /// <summary>A 3x3 basis.</summary>
    /// <param name="value">The basis value.</param>
    public sealed class Basis(GodotConfigFile.Basis value) : Variant
    {
        /// <summary>Gets the basis value.</summary>
        public GodotConfigFile.Basis Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Basis;
    }

    /// <summary>A 3D transform.</summary>
    /// <param name="value">The transform value.</param>
    public sealed class Transform3D(GodotConfigFile.Transform3D value) : Variant
    {
        /// <summary>Gets the transform value.</summary>
        public GodotConfigFile.Transform3D Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Transform3D;
    }

    /// <summary>A 4x4 projection matrix.</summary>
    /// <param name="value">The projection value.</param>
    public sealed class Projection(GodotConfigFile.Projection value) : Variant
    {
        /// <summary>Gets the projection value.</summary>
        public GodotConfigFile.Projection Value { get; } = value;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Projection;
    }

    /// <summary>An opaque resource id. <c>RID()</c> yields id 0.</summary>
    /// <param name="id">The numeric id.</param>
    public sealed class Rid(ulong id) : Variant
    {
        /// <summary>Gets the numeric id.</summary>
        public ulong Id { get; } = id;

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Rid;
    }

    /// <summary>An empty signal. Godot can only deserialize <c>Signal()</c>, and so can this library.</summary>
    public sealed class Signal : Variant
    {
        /// <summary>A shared instance, since <see cref="Signal"/> carries no state.</summary>
        public static Signal Instance { get; } = new();

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Signal;
    }

    /// <summary>An empty callable. Godot can only deserialize <c>Callable()</c>, and so can this library.</summary>
    public sealed class Callable : Variant
    {
        /// <summary>A shared instance, since <see cref="Callable"/> carries no state.</summary>
        public static Callable Instance { get; } = new();

        /// <inheritdoc />
        public override VariantType Kind => VariantType.Callable;
    }
}
