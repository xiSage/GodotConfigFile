using System.Globalization;

namespace GodotConfigFileTests;

/// <summary>
/// The value model: equality, hashing, and Godot's dictionary-key semantics.
/// </summary>
public class ValueModelTests
{
    [Fact]
    public void IntAndFloatAreDifferentValues()
    {
        Assert.NotEqual<Variant>(new Variant.Int(1), new Variant.Float(1.0));
        Assert.False(((Variant)new Variant.Int(1)).Equals(new Variant.Float(1.0)));
        Assert.Equal(new Variant.Int(1), new Variant.Int(1));
    }

    [Fact]
    public void NaNEqualsNaN()
    {
        // Deviation D12: Variant equality follows Godot's key path, where NaN is equal to NaN.
        Assert.Equal<Variant>(new Variant.Float(double.NaN), new Variant.Float(double.NaN));
        Assert.Equal(new Variant.Float(double.NaN).GetHashCode(), new Variant.Float(double.NaN).GetHashCode());
    }

    [Fact]
    public void NegativeZeroHashesLikeZero()
    {
        Assert.Equal<Variant>(new Variant.Float(-0.0), new Variant.Float(0.0));
        Assert.Equal(new Variant.Float(-0.0).GetHashCode(), new Variant.Float(0.0).GetHashCode());
    }

    [Fact]
    public void StringAndStringNameAreDifferentValues()
    {
        Assert.NotEqual<Variant>(new Variant.Str("a"), new Variant.StringName("a"));
    }

    [Fact]
    public void GeometryComparesByComponents()
    {
        Assert.Equal<Variant>(new Variant.Vector2(new Vector2(1, 2)), new Variant.Vector2(new Vector2(1, 2)));
        Assert.NotEqual<Variant>(new Variant.Vector2(new Vector2(1, 2)), new Variant.Vector2(new Vector2(1, 3)));
        Assert.Equal<Variant>(new Variant.Color(new Color(1, 0, 0, 1)), new Variant.Color(new Color(1, 0, 0, 1)));
        Assert.NotEqual<Variant>(new Variant.Vector2(new Vector2(1, 2)), new Variant.Vector2I(new Vector2I(1, 2)));
    }

    [Fact]
    public void ArraysCompareByContentAndType()
    {
        Variant plain = new Variant.Array([new Variant.Int(1), new Variant.Int(2)]);
        Variant same = new Variant.Array([new Variant.Int(1), new Variant.Int(2)]);
        Variant typed = new Variant.Array([new Variant.Int(1), new Variant.Int(2)], VariantType.Int);
        Variant reordered = new Variant.Array([new Variant.Int(2), new Variant.Int(1)]);

        Assert.Equal(plain, same);
        Assert.Equal(plain.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(plain, typed);
        Assert.NotEqual(plain, reordered);
        Assert.NotEqual(plain, new Variant.Array([new Variant.Int(1)]));
    }

    [Fact]
    public void DictionariesCompareByContentIgnoringOrder()
    {
        // Deviation D11: hash and equality agree even when the key order differs.
        Variant first = Dictionary(("a", new Variant.Int(1)), ("b", new Variant.Int(2)));
        Variant second = Dictionary(("b", new Variant.Int(2)), ("a", new Variant.Int(1)));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, Dictionary(("a", new Variant.Int(1)), ("b", new Variant.Int(3))));
        Assert.NotEqual(first, Dictionary(("a", new Variant.Int(1))));
    }

    [Fact]
    public void PackedArraysCompareByContent()
    {
        Assert.Equal<Variant>(
            new Variant.PackedStringArray(["a", "b"]),
            new Variant.PackedStringArray(["a", "b"]));
        Assert.NotEqual<Variant>(
            new Variant.PackedStringArray(["a", "b"]),
            new Variant.PackedStringArray(["b", "a"]));
        Assert.NotEqual<Variant>(
            new Variant.PackedInt32Array([1, 2]),
            new Variant.PackedInt64Array([1, 2]));
    }

    [Fact]
    public void ObjectAndResourceNodesCompareByContent()
    {
        Assert.Equal<Variant>(
            new Variant.Object("Node", [new KeyValuePair<string, Variant>("a", new Variant.Int(1))]),
            new Variant.Object("Node", [new KeyValuePair<string, Variant>("a", new Variant.Int(1))]));
        Assert.NotEqual<Variant>(
            new Variant.Object("Node", []),
            new Variant.Object("Node2", []));

        // Object and Resource share VariantType.Object, so equality has to look at the node class.
        Assert.NotEqual<Variant>(new Variant.Object("Resource", []), new Variant.Resource("Resource", null, "res://x"));
        Assert.Equal<Variant>(
            new Variant.Resource("Resource", "uid://a", null),
            new Variant.Resource("Resource", "uid://a", null));
    }

    [Fact]
    public void NilIsASingleton()
    {
        Assert.Same(Variant.Nil.Instance, Variant.Nil.Instance);
        Assert.Equal<Variant>(Variant.Nil.Instance, new Variant.Nil());
    }

    [Fact]
    public void ComparerMergesStringAndStringName()
    {
        Assert.True(VariantComparer.Instance.Equals(new Variant.Str("a"), new Variant.StringName("a")));
        Assert.Equal(
            VariantComparer.Instance.GetHashCode(new Variant.Str("a")),
            VariantComparer.Instance.GetHashCode(new Variant.StringName("a")));
    }

    [Fact]
    public void ComparerKeepsEverythingElseTypeSensitive()
    {
        Assert.False(VariantComparer.Instance.Equals(new Variant.Int(1), new Variant.Float(1.0)));
        Assert.False(VariantComparer.Instance.Equals(new Variant.Str("a"), new Variant.NodePath("a")));
        Assert.False(VariantComparer.Instance.Equals(new Variant.Str("a"), new Variant.Int(1)));
        Assert.True(VariantComparer.Instance.Equals(new Variant.NodePath("a"), new Variant.NodePath("a")));
    }

    [Fact]
    public void ComparerTreatsCompositeKeysByContent()
    {
        Variant left = new Variant.Array([new Variant.Int(1)]);
        Variant right = new Variant.Array([new Variant.Int(1)]);
        Assert.True(VariantComparer.Instance.Equals(left, right));
        Assert.Equal(VariantComparer.Instance.GetHashCode(left), VariantComparer.Instance.GetHashCode(right));
    }

    [Fact]
    public void DictionaryKeepsFirstPositionOnOverwrite()
    {
        VariantDictionary dictionary = new();
        dictionary.Set(new Variant.Str("a"), new Variant.Int(1));
        dictionary.Set(new Variant.Str("b"), new Variant.Int(2));
        dictionary.Set(new Variant.Str("a"), new Variant.Int(3));

        Assert.Equal(2, dictionary.Count);
        Assert.Equal(new Variant.Str("a"), dictionary.Keys[0]);
        Assert.Equal(new Variant.Str("b"), dictionary.Keys[1]);
        Assert.Equal(new Variant.Int(3), dictionary[new Variant.Str("a")]);
    }

    [Fact]
    public void DictionaryMergesStringAndStringNameKeys()
    {
        VariantDictionary dictionary = new();
        dictionary.Set(new Variant.Str("a"), new Variant.Int(1));
        dictionary.Set(new Variant.StringName("a"), new Variant.Int(2));

        Assert.Single(dictionary);
        Assert.Equal(new Variant.Int(2), dictionary[new Variant.Str("a")]);
        Assert.True(dictionary.TryGetValue(new Variant.StringName("a"), out Variant found));
        Assert.Equal(new Variant.Int(2), found);
    }

    [Fact]
    public void DictionaryAllowsNilAndCompositeKeys()
    {
        VariantDictionary dictionary = new();
        dictionary.Set(Variant.Nil.Instance, new Variant.Int(1));
        dictionary.Set(new Variant.Array([new Variant.Int(1)]), new Variant.Int(2));

        Assert.Equal(2, dictionary.Count);
        Assert.True(dictionary.ContainsKey(Variant.Nil.Instance));
        Assert.True(dictionary.TryGetValue(new Variant.Array([new Variant.Int(1)]), out Variant found));
        Assert.Equal(new Variant.Int(2), found);
    }

    [Fact]
    public void DictionaryMissingKeyThrowsAndReportsMiss()
    {
        VariantDictionary dictionary = new();
        Assert.False(dictionary.TryGetValue(new Variant.Str("nope"), out Variant missing));
        Assert.Same(Variant.Nil.Instance, missing);
        Assert.Throws<KeyNotFoundException>(() => dictionary[new Variant.Str("nope")]);
    }

    [Fact]
    public void ValuesRenderForDebugging()
    {
        Assert.Equal("1", new Variant.Int(1).ToString());
        Assert.Equal("1.5", new Variant.Float(1.5).ToString());
        Assert.Equal("inf", new Variant.Float(double.PositiveInfinity).ToString());
        Assert.Equal("-inf", new Variant.Float(double.NegativeInfinity).ToString());
        Assert.Equal("nan", new Variant.Float(double.NaN).ToString());
        Assert.Equal("&\"name\"", new Variant.StringName("name").ToString());
        Assert.Equal("[\"a\", 1]", new Variant.Array([new Variant.Str("a"), new Variant.Int(1)]).ToString());
        Assert.Equal("{\"a\": 1}", Dictionary(("a", new Variant.Int(1))).ToString());
        Assert.Equal("Vector2(3, 4)", new Variant.Vector2(new Vector2(3, 4)).ToString());
    }

    [Fact]
    public void GeometryStructsUseFloatAndIntComponents()
    {
        Assert.Equal(3f, new Vector2(3, 4).X);
        Assert.Equal(4, new Vector2I(3, 4).Y);
        Assert.Equal(1f, new Color(0, 0, 0, 1).A);
        Assert.Equal(1.5.ToString(CultureInfo.InvariantCulture), new Variant.Float(1.5).ToString());
    }

    private static Variant.Dictionary Dictionary(params (string Key, Variant Value)[] entries)
    {
        VariantDictionary dictionary = new();
        foreach ((string key, Variant value) in entries)
        {
            dictionary.Set(new Variant.Str(key), value);
        }

        return new Variant.Dictionary(dictionary);
    }
}
