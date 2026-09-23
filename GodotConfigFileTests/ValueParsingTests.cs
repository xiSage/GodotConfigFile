namespace GodotConfigFileTests;

/// <summary>
/// Value parsing: literals, constructors, containers and the error cases around them.
/// </summary>
public class ValueParsingTests
{
    [Fact]
    public void ReadsScalars()
    {
        Assert.Equal<Variant>(new Variant.Bool(true), ParseValue("true"));
        Assert.Equal<Variant>(new Variant.Bool(false), ParseValue("false"));
        Assert.Equal<Variant>(new Variant.Int(42), ParseValue("42"));
        Assert.Equal<Variant>(new Variant.Int(-7), ParseValue("-7"));
        Assert.Equal<Variant>(new Variant.Float(1.0), ParseValue("1.0"));
        Assert.Equal<Variant>(new Variant.Float(1.0), ParseValue("1."));
        Assert.Equal<Variant>(new Variant.Float(1000.0), ParseValue("1e3"));
        Assert.Equal<Variant>(new Variant.Float(double.PositiveInfinity), ParseValue("inf"));
        Assert.Equal<Variant>(new Variant.Float(double.NegativeInfinity), ParseValue("-inf"));
        Assert.Equal<Variant>(new Variant.Float(double.NegativeInfinity), ParseValue("inf_neg"));
        Assert.Equal<Variant>(new Variant.Float(double.NaN), ParseValue("nan"));
        Assert.Equal<Variant>(new Variant.Str("hi"), ParseValue("\"hi\""));
        Assert.Equal<Variant>(new Variant.StringName("n"), ParseValue("&\"n\""));
        Assert.Equal<Variant>(new Variant.NodePath("a/b"), ParseValue("NodePath(\"a/b\")"));
    }

    [Fact]
    public void IntAndFloatStayDistinctWhenParsed()
    {
        Assert.IsType<Variant.Int>(ParseValue("1"));
        Assert.IsType<Variant.Float>(ParseValue("1.0"));
    }

    [Fact]
    public void ReadsColours()
    {
        Assert.Equal<Variant>(new Variant.Color(new Color(1, 0, 0, 1)), ParseValue("#f00"));
        Assert.Equal<Variant>(new Variant.Color(new Color(0, 0.5f, 1, 1)), ParseValue("Color(0, 0.5, 1, 1)"));
        Assert.Equal<Variant>(new Variant.Color(new Color(0, 0, 1, 1)), ParseValue("Color(   0, 0,1, 1)"));
    }

    [Theory]
    [InlineData("Vector2(3, 4)", VariantType.Vector2)]
    [InlineData("Vector2i(3, 4)", VariantType.Vector2I)]
    [InlineData("Rect2(1, 2, 3, 4)", VariantType.Rect2)]
    [InlineData("Rect2i(1, 2, 3, 4)", VariantType.Rect2I)]
    [InlineData("Vector3(1, 2, 3)", VariantType.Vector3)]
    [InlineData("Vector3i(1, 2, 3)", VariantType.Vector3I)]
    [InlineData("Vector4(1, 2, 3, 4)", VariantType.Vector4)]
    [InlineData("Vector4i(1, 2, 3, 4)", VariantType.Vector4I)]
    [InlineData("Transform2D(1, 2, 3, 4, 5, 6)", VariantType.Transform2D)]
    [InlineData("Matrix32(1, 2, 3, 4, 5, 6)", VariantType.Transform2D)]
    [InlineData("Plane(1, 2, 3, 4)", VariantType.Plane)]
    [InlineData("Quaternion(1, 2, 3, 4)", VariantType.Quaternion)]
    [InlineData("Quat(1, 2, 3, 4)", VariantType.Quaternion)]
    [InlineData("AABB(1, 2, 3, 4, 5, 6)", VariantType.Aabb)]
    [InlineData("Rect3(1, 2, 3, 4, 5, 6)", VariantType.Aabb)]
    [InlineData("Basis(1, 2, 3, 4, 5, 6, 7, 8, 9)", VariantType.Basis)]
    [InlineData("Matrix3(1, 2, 3, 4, 5, 6, 7, 8, 9)", VariantType.Basis)]
    [InlineData("Transform3D(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)", VariantType.Transform3D)]
    [InlineData("Transform(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)", VariantType.Transform3D)]
    [InlineData("Projection(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16)", VariantType.Projection)]
    [InlineData("Color(1, 0.5, 0, 1)", VariantType.Color)]
    [InlineData("RID()", VariantType.Rid)]
    [InlineData("RID(7)", VariantType.Rid)]
    [InlineData("Signal()", VariantType.Signal)]
    [InlineData("Callable()", VariantType.Callable)]
    public void ReadsBuiltInConstructors(string text, VariantType expected)
    {
        Assert.Equal(expected, ParseValue(text).Kind);
    }

    [Fact]
    public void ReadsConstructorComponentsInOrder()
    {
        Assert.Equal<Variant>(
            new Variant.Vector2(new Vector2(3, 4)),
            ParseValue("Vector2(\n\t3,\n\t4\n)"));
        Assert.Equal<Variant>(
            new Variant.Transform2D(new Transform2D(new Vector2(1, 2), new Vector2(3, 4), new Vector2(5, 6))),
            ParseValue("Transform2D(1, 2, 3, 4, 5, 6)"));
        Assert.Equal<Variant>(
            new Variant.Basis(new Basis(new Vector3(1, 2, 3), new Vector3(4, 5, 6), new Vector3(7, 8, 9))),
            ParseValue("Basis(1, 2, 3, 4, 5, 6, 7, 8, 9)"));
        Assert.Equal<Variant>(
            new Variant.Rect2I(new Rect2I(new Vector2I(1, 2), new Vector2I(3, 4))),
            ParseValue("Rect2i(1, 2, 3, 4)"));
        Assert.Equal<Variant>(new Variant.Rid(7), ParseValue("RID(7)"));
        Assert.Equal<Variant>(new Variant.Rid(0), ParseValue("RID()"));
    }

    [Fact]
    public void ConstructorsAcceptIntegerArgumentsAndSpecialFloats()
    {
        Assert.Equal<Variant>(new Variant.Vector2(new Vector2(1, 2)), ParseValue("Vector2(1, 2)"));
        Vector2 withSpecials = ((Variant.Vector2)ParseValue("Vector2(inf, nan)")).Value;
        Assert.True(float.IsPositiveInfinity(withSpecials.X));
        Assert.True(float.IsNaN(withSpecials.Y));
    }

    [Theory]
    [InlineData("Vector2(1)", "Expected 2 arguments for constructor")]
    [InlineData("Vector2()", "Expected 2 arguments for constructor")]
    [InlineData("Color(1, 2, 3)", "Expected 4 arguments for constructor")]
    [InlineData("Projection(1, 2, 3)", "Expected 16 arguments for constructor")]
    public void ConstructorArgumentCountIsChecked(string text, string message)
    {
        Diagnostic error = Error(text);
        Assert.Equal(DiagnosticCode.ConstructorArgumentCount, error.Code);
        Assert.Equal(message, error.Message);
    }

    [Theory]
    [InlineData("Vector2(1, 2,)")]
    [InlineData("Vector2(1, \"x\")")]
    [InlineData("Vector2(1, 2")]
    public void ConstructorsRejectBadArguments(string text)
    {
        Assert.Throws<ConfigFileParseException>(() => ParseValue(text));
    }

    [Fact]
    public void ReadsArrays()
    {
        Assert.Equal<Variant>(
            new Variant.Array([new Variant.Int(1), new Variant.Str("x"), new Variant.Bool(true)]),
            ParseValue("[1, \"x\", true]"));
        Assert.Equal<Variant>(new Variant.Array([]), ParseValue("[]"));
        Assert.Equal<Variant>(new Variant.Array([new Variant.Int(1)]), ParseValue("[1,]"));
        Assert.Equal<Variant>(
            new Variant.Array([
                new Variant.Array([new Variant.Int(1)]),
                new Variant.Dictionary(Dict(("a", new Variant.Int(2)))),
            ]),
            ParseValue("[[1], {\"a\": 2}]"));
        Assert.Equal<Variant>(
            new Variant.Array([new Variant.Int(1), new Variant.Int(2)]),
            ParseValue("[\n1,\n2\n]"));
    }

    [Fact]
    public void ReadsDictionaries()
    {
        Assert.Equal<Variant>(new Variant.Dictionary(Dict(("a", new Variant.Int(1)))), ParseValue("{\"a\": 1}"));
        Assert.Equal<Variant>(new Variant.Dictionary(new VariantDictionary()), ParseValue("{}"));
        Assert.Equal<Variant>(new Variant.Dictionary(Dict(("a", new Variant.Int(1)))), ParseValue("{\"a\": 1,}"));

        // Keys may be any value, including numbers and nulls.
        Assert.Equal<Variant>(new Variant.Dictionary(Dict((1, new Variant.Str("x")))), ParseValue("{1: \"x\"}"));
        Assert.Equal<Variant>(new Variant.Dictionary(Dict((Variant.Nil.Instance, new Variant.Int(1)))), ParseValue("{null: 1}"));
    }

    [Fact]
    public void DictionariesMergeStringAndStringNameKeys()
    {
        Variant.Dictionary dictionary = (Variant.Dictionary)ParseValue("{\"a\": 1, &\"a\": 2}");

        Assert.Single(dictionary.Items);
        Assert.Equal(new Variant.Int(2), dictionary.Items[new Variant.Str("a")]);
    }

    [Fact]
    public void BareIdentifierKeysAreRejected()
    {
        Diagnostic error = Error("{a: 1}");
        Assert.Equal(DiagnosticCode.UnexpectedIdentifier, error.Code);
        Assert.Equal("Unexpected identifier 'a'", error.Message);
    }

    [Fact]
    public void ArrayWithoutCommasIsRejected()
    {
        Diagnostic error = Error("[1 2]");
        Assert.Equal(DiagnosticCode.ExpectedComma, error.Code);
        Assert.Equal("Expected ','", error.Message);
    }

    [Fact]
    public void DictionaryWithoutColonIsRejected()
    {
        Diagnostic error = Error("{\"a\" 1}");
        Assert.Equal(DiagnosticCode.ExpectedColon, error.Code);
        Assert.Equal("Expected ':'", error.Message);
    }

    [Fact]
    public void UnclosedContainersReportEndOfInput()
    {
        // Without a trailing newline the last value's scan runs past the end of input, and the loop's EOF check
        // fires before another token is read.
        Diagnostic array = ErrorRaw("[1, 2");
        Assert.Equal(DiagnosticCode.UnexpectedEof, array.Code);
        Assert.Equal("Unexpected EOF while parsing array", array.Message);

        Diagnostic dictionary = ErrorRaw("{\"a\": 1");
        Assert.Equal(DiagnosticCode.UnexpectedEof, dictionary.Code);
        Assert.Equal("Unexpected EOF while parsing dictionary", dictionary.Message);
    }

    [Fact]
    public void UnclosedContainersWithATrailingNewlineReportTheSeparator()
    {
        // With a newline after the last element the EOF is only discovered while looking for a separator, so
        // Godot reports a missing separator instead. This is a real difference in behaviour, not in wording.
        Assert.Equal("Expected ','", Error("[1, 2").Message);
        Assert.Equal("Expected '}' or ','", Error("{\"a\": 1").Message);
    }

    [Fact]
    public void ReadsPackedArrays()
    {
        Assert.Equal<Variant>(new Variant.PackedInt32Array([1, 2]), ParseValue("PackedInt32Array(1, 2)"));
        Assert.Equal<Variant>(new Variant.PackedInt32Array([]), ParseValue("PackedInt32Array()"));
        Assert.Equal<Variant>(new Variant.PackedInt64Array([1, 2]), ParseValue("PackedInt64Array(1, 2)"));
        Assert.Equal<Variant>(new Variant.PackedFloat32Array([1.5f]), ParseValue("PackedFloat32Array(1.5)"));
        Assert.Equal<Variant>(new Variant.PackedFloat64Array([1.5]), ParseValue("PackedFloat64Array(1.5)"));
        Assert.Equal<Variant>(new Variant.PackedStringArray(["a", "b"]), ParseValue("PackedStringArray(\"a\", \"b\")"));
        Assert.Equal<Variant>(new Variant.PackedStringArray([]), ParseValue("PackedStringArray()"));
        Assert.Equal<Variant>(new Variant.PackedByteArray([1, 2, 3]), ParseValue("PackedByteArray(\"AQID\")"));
        Assert.Equal<Variant>(new Variant.PackedByteArray([1, 2, 3]), ParseValue("PackedByteArray(1, 2, 3)"));
        Assert.Equal<Variant>(new Variant.PackedByteArray([]), ParseValue("PackedByteArray()"));
        Assert.Equal<Variant>(
            new Variant.PackedVector2Array([new Vector2(1, 2), new Vector2(3, 4)]),
            ParseValue("PackedVector2Array(1, 2, 3, 4)"));
        Assert.Equal<Variant>(new Variant.PackedVector3Array([new Vector3(1, 2, 3)]), ParseValue("PackedVector3Array(1, 2, 3)"));
        Assert.Equal<Variant>(new Variant.PackedVector4Array([new Vector4(1, 2, 3, 4)]), ParseValue("PackedVector4Array(1, 2, 3, 4)"));
        Assert.Equal<Variant>(new Variant.PackedColorArray([new Color(1, 0, 0, 1)]), ParseValue("PackedColorArray(1, 0, 0, 1)"));
    }

    [Theory]
    [InlineData("PoolIntArray(1)", VariantType.PackedInt32Array)]
    [InlineData("IntArray(1)", VariantType.PackedInt32Array)]
    [InlineData("PackedIntArray(1)", VariantType.PackedInt32Array)]
    [InlineData("ByteArray(\"AQID\")", VariantType.PackedByteArray)]
    [InlineData("PoolByteArray(1)", VariantType.PackedByteArray)]
    [InlineData("FloatArray(1.5)", VariantType.PackedFloat32Array)]
    [InlineData("PackedRealArray(1.5)", VariantType.PackedFloat32Array)]
    [InlineData("PoolRealArray(1.5)", VariantType.PackedFloat32Array)]
    [InlineData("PoolStringArray(\"a\")", VariantType.PackedStringArray)]
    [InlineData("StringArray(\"a\")", VariantType.PackedStringArray)]
    [InlineData("Vector2Array(1, 2)", VariantType.PackedVector2Array)]
    [InlineData("PoolVector2Array(1, 2)", VariantType.PackedVector2Array)]
    [InlineData("Vector3Array(1, 2, 3)", VariantType.PackedVector3Array)]
    [InlineData("Vector4Array(1, 2, 3, 4)", VariantType.PackedVector4Array)]
    [InlineData("ColorArray(1, 0, 0, 1)", VariantType.PackedColorArray)]
    [InlineData("PoolColorArray(1, 0, 0, 1)", VariantType.PackedColorArray)]
    public void ReadsPackedArrayAliases(string text, VariantType expected)
    {
        Assert.Equal(expected, ParseValue(text).Kind);
    }

    [Fact]
    public void TrailingCommaIsOnlyAcceptedWhereGodotAcceptsIt()
    {
        Assert.Equal<Variant>(new Variant.PackedStringArray(["a", "b"]), ParseValue("PackedStringArray(\"a\", \"b\",)"));
        Assert.Equal<Variant>(new Variant.Array([new Variant.Int(1)]), ParseValue("[1,]"));
        Assert.Equal<Variant>(new Variant.Dictionary(Dict(("a", new Variant.Int(1)))), ParseValue("{\"a\": 1,}"));

        Assert.Throws<ConfigFileParseException>(() => ParseValue("PackedInt32Array(1, 2,)"));
        Assert.Throws<ConfigFileParseException>(() => ParseValue("Vector2(1, 2,)"));
        Assert.Throws<ConfigFileParseException>(() => ParseValue("PackedVector2Array(1, 2,)"));
    }

    [Fact]
    public void PackedArrayElementTypesAreChecked()
    {
        Diagnostic error = Error("PackedStringArray(1)");
        Assert.Equal(DiagnosticCode.ExpectedToken, error.Code);
        Assert.Equal("Expected string", error.Message);

        Assert.Equal("Expected float in constructor", Error("PackedInt32Array(\"a\")").Message);
    }

    [Fact]
    public void PackedByteArrayConvertsElementsLikeGodot()
    {
        // Integers keep their low eight bits; floats truncate.
        Assert.Equal<Variant>(new Variant.PackedByteArray([0, 255, 1]), ParseValue("PackedByteArray(256, -1, 1.9)"));
    }

    [Fact]
    public void InvalidBase64IsFatal()
    {
        Diagnostic error = Error("PackedByteArray(\"!!!\")");
        Assert.Equal(DiagnosticCode.InvalidBase64, error.Code);
        Assert.Equal("Invalid base64-encoded string", error.Message);
    }

    [Fact]
    public void PackedGroupedArraysDropTrailingElements()
    {
        // Godot divides by the group size without checking for a remainder.
        Variant.PackedVector2Array array = (Variant.PackedVector2Array)ParseValue("PackedVector2Array(1, 2, 3)");
        Assert.Single(array.Items);
        Assert.Equal(new Vector2(1, 2), array.Items[0]);
    }

    [Fact]
    public void ReadsTypedContainers()
    {
        Variant.Array typedArray = (Variant.Array)ParseValue("Array[int]([1, 2])");
        Assert.Equal(VariantType.Int, typedArray.ElementType);
        Assert.Equal(2, typedArray.Items.Count);

        Variant.Array pathArray = (Variant.Array)ParseValue("Array[NodePath]([])");
        Assert.Equal(VariantType.NodePath, pathArray.ElementType);

        Variant.Dictionary typedDictionary = (Variant.Dictionary)ParseValue("Dictionary[String, int]({\"a\": 1})");
        Assert.Equal(VariantType.String, typedDictionary.KeyType);
        Assert.Equal(VariantType.Int, typedDictionary.ValueType);
        Assert.Equal(new Variant.Int(1), typedDictionary.Items[new Variant.Str("a")]);
    }

    [Fact]
    public void UnknownTypeNamesMakeTheContainerUntyped()
    {
        // Deviation D8: Godot's asymmetry (one known side still marks the dictionary typed) is not copied.
        Variant.Array array = (Variant.Array)ParseValue("Array[Whatever]([])");
        Assert.Null(array.ElementType);

        Variant.Dictionary dictionary = (Variant.Dictionary)ParseValue("Dictionary[Whatever, int]({})");
        Assert.Null(dictionary.KeyType);
        Assert.Equal(VariantType.Int, dictionary.ValueType);
    }

    [Fact]
    public void TypedContainersRequireTheirTypeNames()
    {
        Assert.Throws<ConfigFileParseException>(() => ParseValue("Array([1])"));
        Assert.Throws<ConfigFileParseException>(() => ParseValue("Dictionary({\"a\": 1})"));
        Assert.Throws<ConfigFileParseException>(() => ParseValue("Array[int]({})"));
    }

    [Fact]
    public void ReadsObjectLiteralsAsOpaqueNodes()
    {
        Variant.Object value = (Variant.Object)ParseValue("Object(InputEventKey, \"a\": 1, \"b\": null)");

        Assert.Equal("InputEventKey", value.ClassName);
        Assert.Equal(2, value.Properties.Count);
        Assert.Equal("a", value.Properties[0].Key);
        Assert.Equal(new Variant.Int(1), value.Properties[0].Value);
        Assert.Equal("b", value.Properties[1].Key);
        Assert.Equal(Variant.Nil.Instance, value.Properties[1].Value);
    }

    [Fact]
    public void ObjectLiteralsOfUnknownClassesStillParse()
    {
        // Deviation D7: Godot would fail here, because it instantiates through ClassDB.
        Variant.Object value = (Variant.Object)ParseValue("Object(NoSuchClass, \"a\": 1)");
        Assert.Equal("NoSuchClass", value.ClassName);
    }

    [Theory]
    [InlineData("Object()")]
    [InlineData("Object(InputEventKey)")]
    [InlineData("Object(\"InputEventKey\", \"a\": 1)")]
    [InlineData("Object(InputEventKey, a: 1)")]
    [InlineData("Object(InputEventKey, \"a\" 1)")]
    public void ObjectLiteralGrammarIsEnforced(string text)
    {
        Assert.Throws<ConfigFileParseException>(() => ParseValue(text));
    }

    [Fact]
    public void ObjectLiteralAllowsEmptyPropertyList()
    {
        Variant.Object value = (Variant.Object)ParseValue("Object(InputEventKey,)");
        Assert.Empty(value.Properties);
    }

    [Fact]
    public void ReadsResourceReferencesAsOpaqueNodes()
    {
        Variant.Resource path = (Variant.Resource)ParseValue("Resource(\"res://x.tres\")");
        Assert.Equal("Resource", path.Keyword);
        Assert.Null(path.Uid);
        Assert.Equal("res://x.tres", path.Path);

        Variant.Resource both = (Variant.Resource)ParseValue("Resource(\"uid://abc\", \"res://y.tres\")");
        Assert.Equal("uid://abc", both.Uid);
        Assert.Equal("res://y.tres", both.Path);

        Variant.Resource ext = (Variant.Resource)ParseValue("ExtResource(\"1_abc\")");
        Assert.Equal("ExtResource", ext.Keyword);
        Assert.Equal("1_abc", ext.Path);
    }

    [Fact]
    public void ConflictingResourcePathsAreRejected()
    {
        Diagnostic error = Error("Resource(\"uid://a\", \"uid://b\")");
        Assert.Equal(DiagnosticCode.InvalidResourceReference, error.Code);
        Assert.Equal("Two uid:// paths in one Resource reference", error.Message);

        Assert.Throws<ConfigFileParseException>(() => ParseValue("Resource(\"res://a\", \"res://b\")"));
    }

    [Fact]
    public void UnknownIdentifiersAreRejected()
    {
        Diagnostic error = Error("foo");
        Assert.Equal(DiagnosticCode.UnexpectedIdentifier, error.Code);
        Assert.Equal("Unexpected identifier 'foo'", error.Message);
    }

    [Theory]
    [InlineData("", "Expected value, got 'EOF'")]
    [InlineData(".5", "Expected value, got ''.''")]
    [InlineData(")", "Expected value, got '')''")]
    [InlineData("]", "Expected value, got '']''")]
    [InlineData(":", "Expected value, got '':''")]
    public void MissingValuesAreReportedWithGodotWording(string text, string message)
    {
        Diagnostic error = Error(text);
        Assert.Equal(DiagnosticCode.ExpectedValue, error.Code);
        Assert.Equal(message, error.Message);
    }

    [Fact]
    public void NestingDeeperThanTheLimitIsAnError()
    {
        // Deviation D5: Godot's parser has no depth limit and can overflow the stack.
        string allowed = new string('[', 100) + new string(']', 100);
        Assert.Equal(VariantType.Array, ParseValue(allowed).Kind);

        string tooDeep = new string('[', 101) + new string(']', 101);
        Assert.Equal(DiagnosticCode.DepthLimitExceeded, Error(tooDeep).Code);
    }

    [Fact]
    public void DepthLimitIsConfigurable()
    {
        ParseOptions options = new() { MaxDepth = 2 };
        ConfigFileParseException error = Assert.Throws<ConfigFileParseException>(
            () => ConfigFile.Parse("k=[[[1]]]\n", options));
        Assert.Equal(DiagnosticCode.DepthLimitExceeded, error.Diagnostic.Code);
    }

    [Fact]
    public void TypedContainersConvertTheirElements()
    {
        // Godot's Array::assign / Dictionary::assign convert what they can.
        Variant.Array floats = (Variant.Array)ParseValue("Array[float]([1, 2.5])");
        Assert.Equal(VariantType.Float, floats.ElementType);
        Assert.Equal(2, floats.Items.Count);
        Assert.Equal(new Variant.Float(1), floats.Items[0]);
        Assert.Equal(new Variant.Float(2.5), floats.Items[1]);

        Variant.Array ints = (Variant.Array)ParseValue("Array[int]([1.9])");
        Assert.Equal(new Variant.Int(1), ints.Items[0]);

        Variant.Array names = (Variant.Array)ParseValue("Array[StringName]([\"a\"])");
        Assert.Equal(new Variant.StringName("a"), names.Items[0]);

        Variant.Dictionary dictionary = (Variant.Dictionary)ParseValue("Dictionary[String, float]({\"a\": 1})");
        Assert.Equal(new Variant.Float(1), dictionary.Items[new Variant.Str("a")]);
    }

    [Fact]
    public void UnconvertibleElementsEmptyTheContainerAndWarn()
    {
        // Godot prints an error and leaves the converted array empty; the outcome is the same here, and the
        // failure also reaches the caller as a diagnostic.
        ParseResult result = ConfigFile.ParseWithResult("k=Array[int]([1, \"x\"])\n");

        Assert.True(result.Success);
        Variant.Array array = (Variant.Array)Fixture.Require(result.Document, "", "k");
        Assert.Equal(VariantType.Int, array.ElementType);
        Assert.Empty(array.Items);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.IncompatibleElement, diagnostic.Code);
        Assert.Equal("Unable to convert array index 1 from 'String' to 'int'.", diagnostic.Message);
    }

    [Fact]
    public void UnconvertibleDictionaryEntriesEmptyTheDictionary()
    {
        ParseResult result = ConfigFile.ParseWithResult("k=Dictionary[String, int]({\"a\": \"b\"})\n");

        Variant.Dictionary dictionary = (Variant.Dictionary)Fixture.Require(result.Document, "", "k");
        Assert.Empty(dictionary.Items);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.IncompatibleElement, diagnostic.Code);
        Assert.Equal("Unable to convert value at key 'a' from 'String' to 'int'.", diagnostic.Message);
    }

    [Fact]
    public void ObjectLiteralsAcceptTrailingCommasAndRequireTheirCloser()
    {
        Variant.Object value = (Variant.Object)ParseValue("Object(InputEventKey, \"a\": 1,)");
        Assert.Single(value.Properties);

        Diagnostic error = ErrorRaw("Object(InputEventKey, \"a\": 1");
        Assert.Equal(DiagnosticCode.UnexpectedEof, error.Code);
        Assert.Equal("Unexpected EOF while parsing Object()", error.Message);
    }

    private static Variant ParseValue(string value)
    {
        // Every real file ends with a newline, and some scans (colours) need a character after the token.
        ConfigFileDocument document = ConfigFile.Parse($"k={value}\n");
        return Fixture.Require(document, ConfigFile.RootSection, "k");
    }

    private static Variant ParseValueRaw(string value)
    {
        ConfigFileDocument document = ConfigFile.Parse($"k={value}");
        return Fixture.Require(document, ConfigFile.RootSection, "k");
    }

    private static Diagnostic Error(string value) =>
        Assert.Throws<ConfigFileParseException>(() => ParseValue(value)).Diagnostic;

    private static Diagnostic ErrorRaw(string value) =>
        Assert.Throws<ConfigFileParseException>(() => ParseValueRaw(value)).Diagnostic;

    private static VariantDictionary Dict(params (object Key, Variant Value)[] entries)
    {
        VariantDictionary dictionary = new();
        foreach ((object key, Variant value) in entries)
        {
            dictionary.Set(
                key switch
                {
                    string text => new Variant.Str(text),
                    int number => new Variant.Int(number),
                    Variant variant => variant,
                    _ => throw new InvalidOperationException("Unsupported key in test data."),
                },
                value);
        }

        return dictionary;
    }
}
