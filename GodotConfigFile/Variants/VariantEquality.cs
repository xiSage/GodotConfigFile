namespace GodotConfigFile;

/// <summary>
/// Structural equality and hashing for <see cref="Variant"/>.
/// </summary>
/// <remarks>
/// The rules are: type-sensitive (Int 1 is not Float 1.0), recursive for containers, NaN equals NaN, and
/// dictionary values are hashed in an order-independent way so that hash and equality agree (deviation D11).
/// Dictionary *keys* are matched with <see cref="VariantComparer"/>, because String and StringName are the same
/// key there.
/// </remarks>
internal static class VariantEquality
{
    /// <summary>Determines whether two values are equal.</summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true"/> when the two values are structurally equal.</returns>
    public static bool Equals(Variant left, Variant right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left.Kind != right.Kind)
        {
            return false;
        }

        return left switch
        {
            Variant.Nil => true,
            Variant.Bool a when right is Variant.Bool b => a.Value == b.Value,
            Variant.Int a when right is Variant.Int b => a.Value == b.Value,
            Variant.Float a when right is Variant.Float b => a.Value.Equals(b.Value),
            Variant.Str a when right is Variant.Str b => string.Equals(a.Value, b.Value, StringComparison.Ordinal),
            Variant.StringName a when right is Variant.StringName b => string.Equals(a.Value, b.Value, StringComparison.Ordinal),
            Variant.NodePath a when right is Variant.NodePath b => string.Equals(a.Value, b.Value, StringComparison.Ordinal),
            Variant.Color a when right is Variant.Color b => a.Value.Equals(b.Value),
            Variant.Vector2 a when right is Variant.Vector2 b => a.Value.Equals(b.Value),
            Variant.Vector2I a when right is Variant.Vector2I b => a.Value.Equals(b.Value),
            Variant.Rect2 a when right is Variant.Rect2 b => a.Value.Equals(b.Value),
            Variant.Rect2I a when right is Variant.Rect2I b => a.Value.Equals(b.Value),
            Variant.Vector3 a when right is Variant.Vector3 b => a.Value.Equals(b.Value),
            Variant.Vector3I a when right is Variant.Vector3I b => a.Value.Equals(b.Value),
            Variant.Vector4 a when right is Variant.Vector4 b => a.Value.Equals(b.Value),
            Variant.Vector4I a when right is Variant.Vector4I b => a.Value.Equals(b.Value),
            Variant.Transform2D a when right is Variant.Transform2D b => a.Value.Equals(b.Value),
            Variant.Plane a when right is Variant.Plane b => a.Value.Equals(b.Value),
            Variant.Quaternion a when right is Variant.Quaternion b => a.Value.Equals(b.Value),
            Variant.Aabb a when right is Variant.Aabb b => a.Value.Equals(b.Value),
            Variant.Basis a when right is Variant.Basis b => a.Value.Equals(b.Value),
            Variant.Transform3D a when right is Variant.Transform3D b => a.Value.Equals(b.Value),
            Variant.Projection a when right is Variant.Projection b => a.Value.Equals(b.Value),
            Variant.Rid a when right is Variant.Rid b => a.Id == b.Id,
            Variant.Signal => true,
            Variant.Callable => true,
            Variant.Array a when right is Variant.Array b => a.ElementType == b.ElementType && SequenceEquals(a.Items, b.Items),
            Variant.Dictionary a when right is Variant.Dictionary b => DictionaryEquals(a, b),
            Variant.PackedByteArray a when right is Variant.PackedByteArray b => SequenceEquals(a.Items, b.Items),
            Variant.PackedInt32Array a when right is Variant.PackedInt32Array b => SequenceEquals(a.Items, b.Items),
            Variant.PackedInt64Array a when right is Variant.PackedInt64Array b => SequenceEquals(a.Items, b.Items),
            Variant.PackedFloat32Array a when right is Variant.PackedFloat32Array b => SequenceEquals(a.Items, b.Items),
            Variant.PackedFloat64Array a when right is Variant.PackedFloat64Array b => SequenceEquals(a.Items, b.Items),
            Variant.PackedStringArray a when right is Variant.PackedStringArray b => SequenceEquals(a.Items, b.Items),
            Variant.PackedVector2Array a when right is Variant.PackedVector2Array b => SequenceEquals(a.Items, b.Items),
            Variant.PackedVector3Array a when right is Variant.PackedVector3Array b => SequenceEquals(a.Items, b.Items),
            Variant.PackedVector4Array a when right is Variant.PackedVector4Array b => SequenceEquals(a.Items, b.Items),
            Variant.PackedColorArray a when right is Variant.PackedColorArray b => SequenceEquals(a.Items, b.Items),
            Variant.Object a when right is Variant.Object b => ObjectEquals(a, b),
            Variant.Resource a when right is Variant.Resource b => ResourceEquals(a, b),
            _ => false,
        };
    }

    /// <summary>Computes a hash code consistent with <see cref="Equals(Variant, Variant)"/>.</summary>
    /// <param name="value">The value to hash.</param>
    /// <returns>The hash code.</returns>
    public static int GetHashCode(Variant value)
    {
        HashCode hash = new();
        hash.Add((int)value.Kind);

        switch (value)
        {
            case Variant.Nil:
            case Variant.Signal:
            case Variant.Callable:
                break;
            case Variant.Bool v:
                hash.Add(v.Value);
                break;
            case Variant.Int v:
                hash.Add(v.Value);
                break;
            case Variant.Float v:
                hash.Add(v.Value);
                break;
            case Variant.Str v:
                hash.Add(v.Value, StringComparer.Ordinal);
                break;
            case Variant.StringName v:
                hash.Add(v.Value, StringComparer.Ordinal);
                break;
            case Variant.NodePath v:
                hash.Add(v.Value, StringComparer.Ordinal);
                break;
            case Variant.Color v:
                hash.Add(v.Value);
                break;
            case Variant.Vector2 v:
                hash.Add(v.Value);
                break;
            case Variant.Vector2I v:
                hash.Add(v.Value);
                break;
            case Variant.Rect2 v:
                hash.Add(v.Value);
                break;
            case Variant.Rect2I v:
                hash.Add(v.Value);
                break;
            case Variant.Vector3 v:
                hash.Add(v.Value);
                break;
            case Variant.Vector3I v:
                hash.Add(v.Value);
                break;
            case Variant.Vector4 v:
                hash.Add(v.Value);
                break;
            case Variant.Vector4I v:
                hash.Add(v.Value);
                break;
            case Variant.Transform2D v:
                hash.Add(v.Value);
                break;
            case Variant.Plane v:
                hash.Add(v.Value);
                break;
            case Variant.Quaternion v:
                hash.Add(v.Value);
                break;
            case Variant.Aabb v:
                hash.Add(v.Value);
                break;
            case Variant.Basis v:
                hash.Add(v.Value);
                break;
            case Variant.Transform3D v:
                hash.Add(v.Value);
                break;
            case Variant.Projection v:
                hash.Add(v.Value);
                break;
            case Variant.Rid v:
                hash.Add(v.Id);
                break;
            case Variant.Array v:
                hash.Add(v.ElementType);
                foreach (Variant item in v.Items)
                {
                    hash.Add(GetHashCode(item));
                }

                break;
            case Variant.Dictionary v:
                hash.Add(v.KeyType);
                hash.Add(v.ValueType);

                // Order-independent, to match the order-insensitive equality above (deviation D11).
                int entries = 0;
                foreach (KeyValuePair<Variant, Variant> entry in v.Items)
                {
                    entries ^= HashCode.Combine(VariantComparer.Instance.GetHashCode(entry.Key), GetHashCode(entry.Value));
                }

                hash.Add(entries);
                break;
            case Variant.PackedByteArray v:
                AddSequence(ref hash, v.Items);
                break;
            case Variant.PackedInt32Array v:
                AddSequence(ref hash, v.Items);
                break;
            case Variant.PackedInt64Array v:
                AddSequence(ref hash, v.Items);
                break;
            case Variant.PackedFloat32Array v:
                AddSequence(ref hash, v.Items);
                break;
            case Variant.PackedFloat64Array v:
                AddSequence(ref hash, v.Items);
                break;
            case Variant.PackedStringArray v:
                AddStringSequence(ref hash, v.Items);
                break;
            case Variant.PackedVector2Array v:
                AddSequence(ref hash, v.Items);
                break;
            case Variant.PackedVector3Array v:
                AddSequence(ref hash, v.Items);
                break;
            case Variant.PackedVector4Array v:
                AddSequence(ref hash, v.Items);
                break;
            case Variant.PackedColorArray v:
                AddSequence(ref hash, v.Items);
                break;
            case Variant.Object v:
                hash.Add(v.ClassName, StringComparer.Ordinal);
                foreach (KeyValuePair<string, Variant> property in v.Properties)
                {
                    hash.Add(property.Key, StringComparer.Ordinal);
                    hash.Add(GetHashCode(property.Value));
                }

                break;
            case Variant.Resource v:
                hash.Add(v.Keyword, StringComparer.Ordinal);
                hash.Add(v.Uid);
                hash.Add(v.Path);
                break;
            default:
                break;
        }

        return hash.ToHashCode();
    }

    private static bool DictionaryEquals(Variant.Dictionary left, Variant.Dictionary right)
    {
        if (left.KeyType != right.KeyType || left.ValueType != right.ValueType)
        {
            return false;
        }

        if (left.Items.Count != right.Items.Count)
        {
            return false;
        }

        foreach (KeyValuePair<Variant, Variant> entry in left.Items)
        {
            if (!right.Items.TryGetValue(entry.Key, out Variant other))
            {
                return false;
            }

            if (!Equals(entry.Value, other))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ObjectEquals(Variant.Object left, Variant.Object right)
    {
        if (!string.Equals(left.ClassName, right.ClassName, StringComparison.Ordinal))
        {
            return false;
        }

        if (left.Properties.Count != right.Properties.Count)
        {
            return false;
        }

        for (int i = 0; i < left.Properties.Count; i++)
        {
            if (!string.Equals(left.Properties[i].Key, right.Properties[i].Key, StringComparison.Ordinal))
            {
                return false;
            }

            if (!Equals(left.Properties[i].Value, right.Properties[i].Value))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ResourceEquals(Variant.Resource left, Variant.Resource right) =>
        string.Equals(left.Keyword, right.Keyword, StringComparison.Ordinal) &&
        string.Equals(left.Uid, right.Uid, StringComparison.Ordinal) &&
        string.Equals(left.Path, right.Path, StringComparison.Ordinal);

    private static bool SequenceEquals<T>(IReadOnlyList<T> left, IReadOnlyList<T> right)
        where T : IEquatable<T>
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (int i = 0; i < left.Count; i++)
        {
            if (!left[i].Equals(right[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static void AddSequence<T>(ref HashCode hash, IReadOnlyList<T> items)
    {
        hash.Add(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            hash.Add(items[i]);
        }
    }

    private static void AddStringSequence(ref HashCode hash, IReadOnlyList<string> items)
    {
        hash.Add(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            hash.Add(items[i], StringComparer.Ordinal);
        }
    }
}
