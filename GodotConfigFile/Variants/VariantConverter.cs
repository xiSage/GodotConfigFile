namespace GodotConfigFile;

/// <summary>
/// Converts <see cref="Variant"/> values to the closed set of types <see cref="ConfigFileDocument.GetValue{T}"/>
/// supports.
/// </summary>
/// <remarks>
/// <para>
/// The set is a whitelist, and every branch is an explicit type test. There is no reflection, no
/// <c>Convert.ChangeType</c> and no <c>Activator</c> anywhere in this library, so it stays usable under trimming
/// and ahead-of-time compilation.
/// </para>
/// <para>
/// Conversions are only the ones that cannot lose information silently: an <see cref="Variant.Int"/> converts to
/// <see cref="int"/>/<see cref="long"/>/<see cref="double"/>/<see cref="float"/> (with a range check), an
/// <see cref="Variant.Float"/> only to <see cref="double"/>/<see cref="float"/>, and a string never converts to a
/// number. <c>"5"</c> is not 5.
/// </para>
/// </remarks>
internal static class VariantConverter
{
    /// <summary>Converts a value to <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">The converted value.</param>
    /// <returns><see langword="true"/> when the conversion is supported and lossless enough.</returns>
    public static bool TryConvert<T>(Variant value, out T result)
    {
        object? converted = ConvertTo(typeof(T), value);
        if (converted is T typed)
        {
            result = typed;
            return true;
        }

        result = default!;
        return false;
    }

    private static object? ConvertTo(Type target, Variant value)
    {
        if (target == typeof(Variant))
        {
            return value;
        }

        if (target == typeof(bool))
        {
            return value is Variant.Bool b ? b.Value : null;
        }

        if (target == typeof(long))
        {
            return value is Variant.Int i ? i.Value : null;
        }

        if (target == typeof(int))
        {
            return value is Variant.Int i && i.Value is >= int.MinValue and <= int.MaxValue ? (int)i.Value : null;
        }

        if (target == typeof(double))
        {
            return value switch
            {
                Variant.Float f => f.Value,
                Variant.Int i => (double)i.Value,
                _ => null,
            };
        }

        if (target == typeof(float))
        {
            return value switch
            {
                Variant.Float f => (float)f.Value,
                Variant.Int i => (float)i.Value,
                _ => null,
            };
        }

        if (target == typeof(string))
        {
            return TryString(value, out string? text) ? text : null;
        }

        if (target == typeof(Color))
        {
            return value is Variant.Color v ? v.Value : null;
        }

        if (target == typeof(Vector2))
        {
            return value is Variant.Vector2 v ? v.Value : null;
        }

        if (target == typeof(Vector2I))
        {
            return value is Variant.Vector2I v ? v.Value : null;
        }

        if (target == typeof(Rect2))
        {
            return value is Variant.Rect2 v ? v.Value : null;
        }

        if (target == typeof(Rect2I))
        {
            return value is Variant.Rect2I v ? v.Value : null;
        }

        if (target == typeof(Vector3))
        {
            return value is Variant.Vector3 v ? v.Value : null;
        }

        if (target == typeof(Vector3I))
        {
            return value is Variant.Vector3I v ? v.Value : null;
        }

        if (target == typeof(Vector4))
        {
            return value is Variant.Vector4 v ? v.Value : null;
        }

        if (target == typeof(Vector4I))
        {
            return value is Variant.Vector4I v ? v.Value : null;
        }

        if (target == typeof(Transform2D))
        {
            return value is Variant.Transform2D v ? v.Value : null;
        }

        if (target == typeof(Plane))
        {
            return value is Variant.Plane v ? v.Value : null;
        }

        if (target == typeof(Quaternion))
        {
            return value is Variant.Quaternion v ? v.Value : null;
        }

        if (target == typeof(Aabb))
        {
            return value is Variant.Aabb v ? v.Value : null;
        }

        if (target == typeof(Basis))
        {
            return value is Variant.Basis v ? v.Value : null;
        }

        if (target == typeof(Transform3D))
        {
            return value is Variant.Transform3D v ? v.Value : null;
        }

        if (target == typeof(Projection))
        {
            return value is Variant.Projection v ? v.Value : null;
        }

        if (target == typeof(VariantDictionary))
        {
            return value is Variant.Dictionary v ? v.Items : null;
        }

        if (target == typeof(Variant[]))
        {
            return value is Variant.Array array ? array.Items.ToArray() : null;
        }

        if (target == typeof(byte[]))
        {
            return value is Variant.PackedByteArray packed ? packed.Items.ToArray() : null;
        }

        if (target == typeof(string[]))
        {
            return ToStringArray(value);
        }

        if (target == typeof(int[]))
        {
            return ToIntArray(value);
        }

        if (target == typeof(long[]))
        {
            return ToLongArray(value);
        }

        if (target == typeof(float[]))
        {
            return ToFloatArray(value);
        }

        if (target == typeof(double[]))
        {
            return ToDoubleArray(value);
        }

        if (target == typeof(bool[]))
        {
            return ToBoolArray(value);
        }

        if (target == typeof(Color[]))
        {
            return ToColorArray(value);
        }

        return null;
    }

    private static bool TryString(Variant value, out string text)
    {
        switch (value)
        {
            case Variant.Str s:
                text = s.Value;
                return true;
            case Variant.StringName s:
                text = s.Value;
                return true;
            case Variant.NodePath s:
                text = s.Value;
                return true;
            default:
                text = string.Empty;
                return false;
        }
    }

    private static string[]? ToStringArray(Variant value)
    {
        switch (value)
        {
            case Variant.PackedStringArray packed:
                return [.. packed.Items];
            case Variant.Array array:
                {
                    string[] result = new string[array.Items.Count];
                    for (int i = 0; i < result.Length; i++)
                    {
                        if (!TryString(array.Items[i], out string text))
                        {
                            return null;
                        }

                        result[i] = text;
                    }

                    return result;
                }

            default:
                return null;
        }
    }

    private static int[]? ToIntArray(Variant value)
    {
        switch (value)
        {
            case Variant.PackedInt32Array packed:
                return [.. packed.Items];
            case Variant.Array array:
                {
                    int[] result = new int[array.Items.Count];
                    for (int i = 0; i < result.Length; i++)
                    {
                        if (array.Items[i] is not Variant.Int item || item.Value is < int.MinValue or > int.MaxValue)
                        {
                            return null;
                        }

                        result[i] = (int)item.Value;
                    }

                    return result;
                }

            default:
                return null;
        }
    }

    private static long[]? ToLongArray(Variant value)
    {
        switch (value)
        {
            case Variant.PackedInt64Array packed:
                return [.. packed.Items];
            case Variant.Array array:
                {
                    long[] result = new long[array.Items.Count];
                    for (int i = 0; i < result.Length; i++)
                    {
                        if (array.Items[i] is not Variant.Int item)
                        {
                            return null;
                        }

                        result[i] = item.Value;
                    }

                    return result;
                }

            default:
                return null;
        }
    }

    private static float[]? ToFloatArray(Variant value)
    {
        switch (value)
        {
            case Variant.PackedFloat32Array packed:
                return [.. packed.Items];
            case Variant.Array array:
                {
                    float[] result = new float[array.Items.Count];
                    for (int i = 0; i < result.Length; i++)
                    {
                        switch (array.Items[i])
                        {
                            case Variant.Float item:
                                result[i] = (float)item.Value;
                                break;
                            case Variant.Int item:
                                result[i] = (float)item.Value;
                                break;
                            default:
                                return null;
                        }
                    }

                    return result;
                }

            default:
                return null;
        }
    }

    private static double[]? ToDoubleArray(Variant value)
    {
        switch (value)
        {
            case Variant.PackedFloat64Array packed:
                return [.. packed.Items];
            case Variant.Array array:
                {
                    double[] result = new double[array.Items.Count];
                    for (int i = 0; i < result.Length; i++)
                    {
                        switch (array.Items[i])
                        {
                            case Variant.Float item:
                                result[i] = item.Value;
                                break;
                            case Variant.Int item:
                                result[i] = item.Value;
                                break;
                            default:
                                return null;
                        }
                    }

                    return result;
                }

            default:
                return null;
        }
    }

    private static bool[]? ToBoolArray(Variant value)
    {
        return value is not Variant.Array array ? null : ToBoolArray(array);

        static bool[]? ToBoolArray(Variant.Array array)
        {
            bool[] result = new bool[array.Items.Count];
            for (int i = 0; i < result.Length; i++)
            {
                if (array.Items[i] is not Variant.Bool item)
                {
                    return null;
                }

                result[i] = item.Value;
            }

            return result;
        }
    }

    private static Color[]? ToColorArray(Variant value)
    {
        switch (value)
        {
            case Variant.PackedColorArray packed:
                return [.. packed.Items];
            case Variant.Array array:
                {
                    Color[] result = new Color[array.Items.Count];
                    for (int i = 0; i < result.Length; i++)
                    {
                        if (array.Items[i] is not Variant.Color item)
                        {
                            return null;
                        }

                        result[i] = item.Value;
                    }

                    return result;
                }

            default:
                return null;
        }
    }
}
