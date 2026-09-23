using System.Globalization;
using System.Text;

namespace GodotConfigFile;

/// <summary>
/// Renders <see cref="Variant"/> values for logs and test failures.
/// </summary>
/// <remarks>
/// This is a debug formatter, not a writer. The library implements parsing only: the text produced here is not
/// guaranteed to be stable and is not guaranteed to parse back.
/// </remarks>
internal static class VariantDebug
{
    /// <summary>Formats a value.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>A Godot-flavoured representation.</returns>
    public static string Format(Variant value)
    {
        StringBuilder builder = new();
        Append(builder, value);
        return builder.ToString();
    }

    private static void Append(StringBuilder builder, Variant value)
    {
        switch (value)
        {
            case Variant.Nil:
                builder.Append("null");
                break;
            case Variant.Bool v:
                builder.Append(v.Value ? "true" : "false");
                break;
            case Variant.Int v:
                builder.Append(v.Value.ToString(CultureInfo.InvariantCulture));
                break;
            case Variant.Float v:
                AppendDouble(builder, v.Value);
                break;
            case Variant.Str v:
                AppendQuoted(builder, v.Value);
                break;
            case Variant.StringName v:
                builder.Append('&');
                AppendQuoted(builder, v.Value);
                break;
            case Variant.NodePath v:
                builder.Append("NodePath(");
                AppendQuoted(builder, v.Value);
                builder.Append(')');
                break;
            case Variant.Color v:
                AppendFloats(builder, "Color", v.Value.R, v.Value.G, v.Value.B, v.Value.A);
                break;
            case Variant.Vector2 v:
                AppendFloats(builder, "Vector2", v.Value.X, v.Value.Y);
                break;
            case Variant.Vector2I v:
                AppendFloats(builder, "Vector2i", v.Value.X, v.Value.Y);
                break;
            case Variant.Rect2 v:
                AppendFloats(builder, "Rect2", v.Value.Position.X, v.Value.Position.Y, v.Value.Size.X, v.Value.Size.Y);
                break;
            case Variant.Rect2I v:
                AppendFloats(builder, "Rect2i", v.Value.Position.X, v.Value.Position.Y, v.Value.Size.X, v.Value.Size.Y);
                break;
            case Variant.Vector3 v:
                AppendFloats(builder, "Vector3", v.Value.X, v.Value.Y, v.Value.Z);
                break;
            case Variant.Vector3I v:
                AppendFloats(builder, "Vector3i", v.Value.X, v.Value.Y, v.Value.Z);
                break;
            case Variant.Vector4 v:
                AppendFloats(builder, "Vector4", v.Value.X, v.Value.Y, v.Value.Z, v.Value.W);
                break;
            case Variant.Vector4I v:
                AppendFloats(builder, "Vector4i", v.Value.X, v.Value.Y, v.Value.Z, v.Value.W);
                break;
            case Variant.Transform2D v:
                AppendFloats(builder, "Transform2D", v.Value.X.X, v.Value.X.Y, v.Value.Y.X, v.Value.Y.Y, v.Value.Origin.X, v.Value.Origin.Y);
                break;
            case Variant.Plane v:
                AppendFloats(builder, "Plane", v.Value.Normal.X, v.Value.Normal.Y, v.Value.Normal.Z, v.Value.D);
                break;
            case Variant.Quaternion v:
                AppendFloats(builder, "Quaternion", v.Value.X, v.Value.Y, v.Value.Z, v.Value.W);
                break;
            case Variant.Aabb v:
                AppendFloats(builder, "AABB", v.Value.Position.X, v.Value.Position.Y, v.Value.Position.Z, v.Value.Size.X, v.Value.Size.Y, v.Value.Size.Z);
                break;
            case Variant.Basis v:
                AppendFloats(builder, "Basis", v.Value.X.X, v.Value.X.Y, v.Value.X.Z, v.Value.Y.X, v.Value.Y.Y, v.Value.Y.Z, v.Value.Z.X, v.Value.Z.Y, v.Value.Z.Z);
                break;
            case Variant.Transform3D v:
                AppendFloats(builder, "Transform3D",
                    v.Value.Basis.X.X, v.Value.Basis.X.Y, v.Value.Basis.X.Z,
                    v.Value.Basis.Y.X, v.Value.Basis.Y.Y, v.Value.Basis.Y.Z,
                    v.Value.Basis.Z.X, v.Value.Basis.Z.Y, v.Value.Basis.Z.Z,
                    v.Value.Origin.X, v.Value.Origin.Y, v.Value.Origin.Z);
                break;
            case Variant.Projection v:
                AppendFloats(builder, "Projection",
                    v.Value.X.X, v.Value.X.Y, v.Value.X.Z, v.Value.X.W,
                    v.Value.Y.X, v.Value.Y.Y, v.Value.Y.Z, v.Value.Y.W,
                    v.Value.Z.X, v.Value.Z.Y, v.Value.Z.Z, v.Value.Z.W,
                    v.Value.W.X, v.Value.W.Y, v.Value.W.Z, v.Value.W.W);
                break;
            case Variant.Rid v:
                builder.Append(CultureInfo.InvariantCulture, $"RID({v.Id})");
                break;
            case Variant.Signal:
                builder.Append("Signal()");
                break;
            case Variant.Callable:
                builder.Append("Callable()");
                break;
            case Variant.Array v:
                AppendArray(builder, v);
                break;
            case Variant.Dictionary v:
                AppendDictionary(builder, v);
                break;
            case Variant.PackedByteArray v:
                AppendSequence(builder, "PackedByteArray", v.Items);
                break;
            case Variant.PackedInt32Array v:
                AppendSequence(builder, "PackedInt32Array", v.Items);
                break;
            case Variant.PackedInt64Array v:
                AppendSequence(builder, "PackedInt64Array", v.Items);
                break;
            case Variant.PackedFloat32Array v:
                AppendSequence(builder, "PackedFloat32Array", v.Items);
                break;
            case Variant.PackedFloat64Array v:
                AppendSequence(builder, "PackedFloat64Array", v.Items);
                break;
            case Variant.PackedStringArray v:
                AppendSequence(builder, "PackedStringArray", v.Items);
                break;
            case Variant.PackedVector2Array v:
                AppendSequence(builder, "PackedVector2Array", v.Items);
                break;
            case Variant.PackedVector3Array v:
                AppendSequence(builder, "PackedVector3Array", v.Items);
                break;
            case Variant.PackedVector4Array v:
                AppendSequence(builder, "PackedVector4Array", v.Items);
                break;
            case Variant.PackedColorArray v:
                AppendSequence(builder, "PackedColorArray", v.Items);
                break;
            case Variant.Object v:
                builder.Append(CultureInfo.InvariantCulture, $"Object({v.ClassName}");
                foreach (KeyValuePair<string, Variant> property in v.Properties)
                {
                    builder.Append(", ");
                    AppendQuoted(builder, property.Key);
                    builder.Append(": ");
                    Append(builder, property.Value);
                }

                builder.Append(')');
                break;
            case Variant.Resource v:
                builder.Append(CultureInfo.InvariantCulture, $"{v.Keyword}({v.Uid ?? "-"}, {v.Path ?? "-"})");
                break;
            default:
                builder.Append(value.Kind);
                break;
        }
    }

    private static void AppendArray(StringBuilder builder, Variant.Array array)
    {
        if (array.ElementType is VariantType elementType)
        {
            builder.Append(CultureInfo.InvariantCulture, $"Array[{elementType}]");
        }

        builder.Append('[');
        for (int i = 0; i < array.Items.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            Append(builder, array.Items[i]);
        }

        builder.Append(']');
    }

    private static void AppendDictionary(StringBuilder builder, Variant.Dictionary dictionary)
    {
        builder.Append('{');
        bool first = true;
        foreach (KeyValuePair<Variant, Variant> entry in dictionary.Items)
        {
            if (!first)
            {
                builder.Append(", ");
            }

            first = false;
            Append(builder, entry.Key);
            builder.Append(": ");
            Append(builder, entry.Value);
        }

        builder.Append('}');
    }

    private static void AppendSequence<T>(StringBuilder builder, string name, IReadOnlyList<T> items)
    {
        builder.Append(CultureInfo.InvariantCulture, $"{name}(");
        for (int i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(CultureInfo.InvariantCulture, $"{items[i]}");
        }

        builder.Append(')');
    }

    private static void AppendFloats(StringBuilder builder, string name, params float[] values)
    {
        builder.Append(CultureInfo.InvariantCulture, $"{name}(");
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(values[i].ToString("R", CultureInfo.InvariantCulture));
        }

        builder.Append(')');
    }

    private static void AppendDouble(StringBuilder builder, double value)
    {
        if (double.IsPositiveInfinity(value))
        {
            builder.Append("inf");
        }
        else if (double.IsNegativeInfinity(value))
        {
            builder.Append("-inf");
        }
        else if (double.IsNaN(value))
        {
            builder.Append("nan");
        }
        else
        {
            builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }
    }

    private static void AppendQuoted(StringBuilder builder, string text)
    {
        builder.Append('"');
        foreach (char c in text)
        {
            if (c is '"' or '\\')
            {
                builder.Append('\\');
            }

            builder.Append(c);
        }

        builder.Append('"');
    }
}
