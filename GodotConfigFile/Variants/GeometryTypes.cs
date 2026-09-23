namespace GodotConfigFile;

// Godot's real_t is float unless the engine is built with precision=double, so every geometry type below
// uses float components, exactly like a default Godot build. Variant.Float, in contrast, is a double.

/// <summary>A 2D vector with <see cref="float"/> components.</summary>
/// <param name="X">The X component.</param>
/// <param name="Y">The Y component.</param>
public readonly record struct Vector2(float X, float Y);

/// <summary>A 2D vector with 32-bit integer components.</summary>
/// <param name="X">The X component.</param>
/// <param name="Y">The Y component.</param>
public readonly record struct Vector2I(int X, int Y);

/// <summary>A 2D axis-aligned rectangle.</summary>
/// <param name="Position">The top-left corner.</param>
/// <param name="Size">The size, which may be negative.</param>
public readonly record struct Rect2(Vector2 Position, Vector2 Size);

/// <summary>A 2D axis-aligned rectangle with 32-bit integer components.</summary>
/// <param name="Position">The top-left corner.</param>
/// <param name="Size">The size, which may be negative.</param>
public readonly record struct Rect2I(Vector2I Position, Vector2I Size);

/// <summary>A 3D vector with <see cref="float"/> components.</summary>
/// <param name="X">The X component.</param>
/// <param name="Y">The Y component.</param>
/// <param name="Z">The Z component.</param>
public readonly record struct Vector3(float X, float Y, float Z);

/// <summary>A 3D vector with 32-bit integer components.</summary>
/// <param name="X">The X component.</param>
/// <param name="Y">The Y component.</param>
/// <param name="Z">The Z component.</param>
public readonly record struct Vector3I(int X, int Y, int Z);

/// <summary>A 4D vector with <see cref="float"/> components.</summary>
/// <param name="X">The X component.</param>
/// <param name="Y">The Y component.</param>
/// <param name="Z">The Z component.</param>
/// <param name="W">The W component.</param>
public readonly record struct Vector4(float X, float Y, float Z, float W);

/// <summary>A 4D vector with 32-bit integer components.</summary>
/// <param name="X">The X component.</param>
/// <param name="Y">The Y component.</param>
/// <param name="Z">The Z component.</param>
/// <param name="W">The W component.</param>
public readonly record struct Vector4I(int X, int Y, int Z, int W);

/// <summary>A 2D transform stored as its three column vectors.</summary>
/// <param name="X">The X basis column.</param>
/// <param name="Y">The Y basis column.</param>
/// <param name="Origin">The translation column.</param>
public readonly record struct Transform2D(Vector2 X, Vector2 Y, Vector2 Origin);

/// <summary>A plane in 3D space, stored as a normal and a distance.</summary>
/// <param name="Normal">The normal vector.</param>
/// <param name="D">The distance from the origin along the normal.</param>
public readonly record struct Plane(Vector3 Normal, float D);

/// <summary>A quaternion.</summary>
/// <param name="X">The X component.</param>
/// <param name="Y">The Y component.</param>
/// <param name="Z">The Z component.</param>
/// <param name="W">The W (scalar) component.</param>
public readonly record struct Quaternion(float X, float Y, float Z, float W);

/// <summary>An axis-aligned bounding box.</summary>
/// <param name="Position">The position of the box.</param>
/// <param name="Size">The size of the box, which may be negative.</param>
public readonly record struct Aabb(Vector3 Position, Vector3 Size);

/// <summary>A 3x3 basis matrix stored as its three column vectors.</summary>
/// <param name="X">The X basis column.</param>
/// <param name="Y">The Y basis column.</param>
/// <param name="Z">The Z basis column.</param>
public readonly record struct Basis(Vector3 X, Vector3 Y, Vector3 Z);

/// <summary>A 3D transform: a <see cref="Basis"/> plus an origin.</summary>
/// <param name="Basis">The rotation/scale part.</param>
/// <param name="Origin">The translation part.</param>
public readonly record struct Transform3D(Basis Basis, Vector3 Origin);

/// <summary>A 4x4 projection matrix stored as its four column vectors.</summary>
/// <param name="X">The X column.</param>
/// <param name="Y">The Y column.</param>
/// <param name="Z">The Z column.</param>
/// <param name="W">The W column.</param>
public readonly record struct Projection(Vector4 X, Vector4 Y, Vector4 Z, Vector4 W);

/// <summary>An RGBA colour with <see cref="float"/> components in the 0..1 range.</summary>
/// <param name="R">The red component.</param>
/// <param name="G">The green component.</param>
/// <param name="B">The blue component.</param>
/// <param name="A">The alpha component.</param>
public readonly record struct Color(float R, float G, float B, float A);
