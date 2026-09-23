namespace GodotConfigFile;

/// <summary>
/// The kind of a <see cref="Variant"/>.
/// </summary>
/// <remarks>
/// Members mirror Godot's <c>Variant::Type</c> enum, in the same order and starting at zero. The names follow
/// Godot's C# bindings (<c>Vector2I</c>, <c>Aabb</c>, <c>Rid</c>) rather than the C++ spelling.
/// </remarks>
public enum VariantType
{
    /// <summary>The absence of a value.</summary>
    Nil = 0,

    /// <summary>A boolean.</summary>
    Bool,

    /// <summary>A signed 64-bit integer.</summary>
    Int,

    /// <summary>A double-precision float. Distinct from <see cref="Int"/>; <c>1</c> and <c>1.0</c> are different types.</summary>
    Float,

    /// <summary>A UTF-8 string.</summary>
    String,

    /// <summary>A 2D float vector.</summary>
    Vector2,

    /// <summary>A 2D integer vector.</summary>
    Vector2I,

    /// <summary>A 2D axis-aligned rectangle.</summary>
    Rect2,

    /// <summary>A 2D integer rectangle.</summary>
    Rect2I,

    /// <summary>A 3D float vector.</summary>
    Vector3,

    /// <summary>A 3D integer vector.</summary>
    Vector3I,

    /// <summary>A 2D transform.</summary>
    Transform2D,

    /// <summary>A 4D float vector.</summary>
    Vector4,

    /// <summary>A 4D integer vector.</summary>
    Vector4I,

    /// <summary>A plane in 3D space.</summary>
    Plane,

    /// <summary>A quaternion.</summary>
    Quaternion,

    /// <summary>An axis-aligned bounding box.</summary>
    Aabb,

    /// <summary>A 3x3 basis.</summary>
    Basis,

    /// <summary>A 3D transform.</summary>
    Transform3D,

    /// <summary>A 4x4 projection matrix.</summary>
    Projection,

    /// <summary>A colour with float components.</summary>
    Color,

    /// <summary>An interned string.</summary>
    StringName,

    /// <summary>A path to a node.</summary>
    NodePath,

    /// <summary>An opaque resource id.</summary>
    Rid,

    /// <summary>An object; here only ever produced by <c>Object(...)</c> and kept opaque.</summary>
    Object,

    /// <summary>A callable; only the empty form <c>Callable()</c> is supported.</summary>
    Callable,

    /// <summary>A signal; only the empty form <c>Signal()</c> is supported.</summary>
    Signal,

    /// <summary>An ordered dictionary with <see cref="Variant"/> keys.</summary>
    Dictionary,

    /// <summary>An ordered, optionally typed array.</summary>
    Array,

    /// <summary>A packed array of bytes.</summary>
    PackedByteArray,

    /// <summary>A packed array of 32-bit integers.</summary>
    PackedInt32Array,

    /// <summary>A packed array of 64-bit integers.</summary>
    PackedInt64Array,

    /// <summary>A packed array of 32-bit floats.</summary>
    PackedFloat32Array,

    /// <summary>A packed array of 64-bit floats.</summary>
    PackedFloat64Array,

    /// <summary>A packed array of strings.</summary>
    PackedStringArray,

    /// <summary>A packed array of 2D float vectors.</summary>
    PackedVector2Array,

    /// <summary>A packed array of 3D float vectors.</summary>
    PackedVector3Array,

    /// <summary>A packed array of colours.</summary>
    PackedColorArray,

    /// <summary>A packed array of 4D float vectors.</summary>
    PackedVector4Array,
}
