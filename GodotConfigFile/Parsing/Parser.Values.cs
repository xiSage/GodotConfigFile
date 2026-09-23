namespace GodotConfigFile;

/// <summary>
/// Value parsing, a port of <c>VariantParser::parse_value</c> and the <c>_parse_*</c> helpers.
/// </summary>
internal sealed partial class Parser
{
    private static readonly Dictionary<string, VariantType> TypeNames = new(StringComparer.Ordinal)
    {
        // Godot builds this table from Variant::get_type_name for every type except NIL, which is why "Nil"
        // is not accepted inside Array[...] / Dictionary[...].
        ["bool"] = VariantType.Bool,
        ["int"] = VariantType.Int,
        ["float"] = VariantType.Float,
        ["String"] = VariantType.String,
        ["Vector2"] = VariantType.Vector2,
        ["Vector2i"] = VariantType.Vector2I,
        ["Rect2"] = VariantType.Rect2,
        ["Rect2i"] = VariantType.Rect2I,
        ["Vector3"] = VariantType.Vector3,
        ["Vector3i"] = VariantType.Vector3I,
        ["Transform2D"] = VariantType.Transform2D,
        ["Vector4"] = VariantType.Vector4,
        ["Vector4i"] = VariantType.Vector4I,
        ["Plane"] = VariantType.Plane,
        ["Quaternion"] = VariantType.Quaternion,
        ["AABB"] = VariantType.Aabb,
        ["Basis"] = VariantType.Basis,
        ["Transform3D"] = VariantType.Transform3D,
        ["Projection"] = VariantType.Projection,
        ["Color"] = VariantType.Color,
        ["StringName"] = VariantType.StringName,
        ["NodePath"] = VariantType.NodePath,
        ["RID"] = VariantType.Rid,
        ["Object"] = VariantType.Object,
        ["Callable"] = VariantType.Callable,
        ["Signal"] = VariantType.Signal,
        ["Dictionary"] = VariantType.Dictionary,
        ["Array"] = VariantType.Array,
        ["PackedByteArray"] = VariantType.PackedByteArray,
        ["PackedInt32Array"] = VariantType.PackedInt32Array,
        ["PackedInt64Array"] = VariantType.PackedInt64Array,
        ["PackedFloat32Array"] = VariantType.PackedFloat32Array,
        ["PackedFloat64Array"] = VariantType.PackedFloat64Array,
        ["PackedStringArray"] = VariantType.PackedStringArray,
        ["PackedVector2Array"] = VariantType.PackedVector2Array,
        ["PackedVector3Array"] = VariantType.PackedVector3Array,
        ["PackedColorArray"] = VariantType.PackedColorArray,
        ["PackedVector4Array"] = VariantType.PackedVector4Array,
    };

    /// <summary>The same names, indexed by type, for the conversion messages Godot prints.</summary>
    private static readonly Dictionary<VariantType, string> TypeNamesByType =
        TypeNames.ToDictionary(pair => pair.Value, pair => pair.Key);

    /// <summary>Parses a value from an already-read token.</summary>
    /// <param name="token">The first token of the value.</param>
    /// <returns>The parsed value.</returns>
    internal Variant ParseValue(Token token)
    {
        switch (token.Type)
        {
            case TokenType.CurlyBracketOpen:
                EnterContainer(token.Span);
                try
                {
                    return new Variant.Dictionary(ParseDictionaryBody());
                }
                finally
                {
                    ExitContainer();
                }

            case TokenType.BracketOpen:
                EnterContainer(token.Span);
                try
                {
                    return new Variant.Array(ParseArrayBody());
                }
                finally
                {
                    ExitContainer();
                }

            case TokenType.Identifier:
                return ParseIdentifierValue(token);

            case TokenType.Number:
            case TokenType.String:
            case TokenType.StringName:
            case TokenType.Color:
                return token.Value;

            default:
                Fail(DiagnosticCode.ExpectedValue, $"Expected value, got '{token.Name}'", token.Span);
                return Variant.Nil.Instance;
        }
    }

    private Variant ParseIdentifierValue(Token token)
    {
        string id = IdentifierText(token);

        switch (id)
        {
            case "true":
                return new Variant.Bool(true);
            case "false":
                return new Variant.Bool(false);
            case "null":
            case "nil": // "nil" is the legacy spelling of null, still accepted by Godot.
                return Variant.Nil.Instance;
            case "inf":
                return new Variant.Float(double.PositiveInfinity);
            case "-inf":
            case "inf_neg": // The lexer keeps a leading '-' with the identifier; "inf_neg" is the legacy spelling.
                return new Variant.Float(double.NegativeInfinity);
            case "nan":
                return new Variant.Float(double.NaN);

            case "Vector2":
                return Construct(token, 2, static a => new Variant.Vector2(new Vector2((float)a[0], (float)a[1])));
            case "Vector2i":
                return Construct(token, 2, static a => new Variant.Vector2I(new Vector2I(ToInt32(a[0]), ToInt32(a[1]))));
            case "Rect2":
                return Construct(token, 4, static a => new Variant.Rect2(new Rect2(new Vector2((float)a[0], (float)a[1]), new Vector2((float)a[2], (float)a[3]))));
            case "Rect2i":
                return Construct(token, 4, static a => new Variant.Rect2I(new Rect2I(new Vector2I(ToInt32(a[0]), ToInt32(a[1])), new Vector2I(ToInt32(a[2]), ToInt32(a[3])))));
            case "Vector3":
                return Construct(token, 3, static a => new Variant.Vector3(new Vector3((float)a[0], (float)a[1], (float)a[2])));
            case "Vector3i":
                return Construct(token, 3, static a => new Variant.Vector3I(new Vector3I(ToInt32(a[0]), ToInt32(a[1]), ToInt32(a[2]))));
            case "Transform2D":
            case "Matrix32":
                return Construct(token, 6, static a => new Variant.Transform2D(new Transform2D(
                    new Vector2((float)a[0], (float)a[1]),
                    new Vector2((float)a[2], (float)a[3]),
                    new Vector2((float)a[4], (float)a[5]))));
            case "Vector4":
                return Construct(token, 4, static a => new Variant.Vector4(new Vector4((float)a[0], (float)a[1], (float)a[2], (float)a[3])));
            case "Vector4i":
                return Construct(token, 4, static a => new Variant.Vector4I(new Vector4I(ToInt32(a[0]), ToInt32(a[1]), ToInt32(a[2]), ToInt32(a[3]))));
            case "Plane":
                return Construct(token, 4, static a => new Variant.Plane(new Plane(new Vector3((float)a[0], (float)a[1], (float)a[2]), (float)a[3])));
            case "Quaternion":
            case "Quat":
                return Construct(token, 4, static a => new Variant.Quaternion(new Quaternion((float)a[0], (float)a[1], (float)a[2], (float)a[3])));
            case "AABB":
            case "Rect3":
                return Construct(token, 6, static a => new Variant.Aabb(new Aabb(
                    new Vector3((float)a[0], (float)a[1], (float)a[2]),
                    new Vector3((float)a[3], (float)a[4], (float)a[5]))));
            case "Basis":
            case "Matrix3":
                return Construct(token, 9, static a => new Variant.Basis(new Basis(
                    new Vector3((float)a[0], (float)a[1], (float)a[2]),
                    new Vector3((float)a[3], (float)a[4], (float)a[5]),
                    new Vector3((float)a[6], (float)a[7], (float)a[8]))));
            case "Transform3D":
            case "Transform":
                return Construct(token, 12, static a => new Variant.Transform3D(new Transform3D(
                    new Basis(
                        new Vector3((float)a[0], (float)a[1], (float)a[2]),
                        new Vector3((float)a[3], (float)a[4], (float)a[5]),
                        new Vector3((float)a[6], (float)a[7], (float)a[8])),
                    new Vector3((float)a[9], (float)a[10], (float)a[11]))));
            case "Projection":
                return Construct(token, 16, static a => new Variant.Projection(new Projection(
                    new Vector4((float)a[0], (float)a[1], (float)a[2], (float)a[3]),
                    new Vector4((float)a[4], (float)a[5], (float)a[6], (float)a[7]),
                    new Vector4((float)a[8], (float)a[9], (float)a[10], (float)a[11]),
                    new Vector4((float)a[12], (float)a[13], (float)a[14], (float)a[15]))));
            case "Color":
                return Construct(token, 4, static a => new Variant.Color(new Color((float)a[0], (float)a[1], (float)a[2], (float)a[3])));

            case "NodePath":
                return ParseNodePath();
            case "RID":
                return ParseRid();
            case "Signal":
                return ParseEmptyConstructor<Variant.Signal>(Variant.Signal.Instance);
            case "Callable":
                return ParseEmptyConstructor<Variant.Callable>(Variant.Callable.Instance);

            case "Object":
                return ParseObjectLiteral();

            case "Resource":
            case "SubResource":
            case "ExtResource":
                return ParseResourceLiteral(id);

            case "Array":
                return ParseTypedArray(token.Span);
            case "Dictionary":
                return ParseTypedDictionary(token.Span);

            case "PackedByteArray":
            case "PoolByteArray":
            case "ByteArray":
                return ParsePackedByteArray();
            case "PackedInt32Array":
            case "PackedIntArray":
            case "PoolIntArray":
            case "IntArray":
                return new Variant.PackedInt32Array(ToSequence(ParseConstructArguments(), static v => ToInt32(v)));
            case "PackedInt64Array":
                return new Variant.PackedInt64Array(ToSequence(ParseConstructArguments(), static v => ToInt64(v)));
            case "PackedFloat32Array":
            case "PackedRealArray":
            case "PoolRealArray":
            case "FloatArray":
                return new Variant.PackedFloat32Array(ToSequence(ParseConstructArguments(), static v => (float)v));
            case "PackedFloat64Array":
                return new Variant.PackedFloat64Array(ToSequence(ParseConstructArguments(), static v => v));
            case "PackedStringArray":
            case "PoolStringArray":
            case "StringArray":
                return ParsePackedStringArray();
            case "PackedVector2Array":
            case "PoolVector2Array":
            case "Vector2Array":
                return new Variant.PackedVector2Array(GroupVectors2(ParseConstructArguments()));
            case "PackedVector3Array":
            case "PoolVector3Array":
            case "Vector3Array":
                return new Variant.PackedVector3Array(GroupVectors3(ParseConstructArguments()));
            case "PackedVector4Array":
            case "PoolVector4Array":
            case "Vector4Array":
                return new Variant.PackedVector4Array(GroupVectors4(ParseConstructArguments()));
            case "PackedColorArray":
            case "PoolColorArray":
            case "ColorArray":
                return new Variant.PackedColorArray(GroupColors(ParseConstructArguments()));

            default:
                Fail(DiagnosticCode.UnexpectedIdentifier, $"Unexpected identifier '{id}'", token.Span);
                return Variant.Nil.Instance;
        }
    }

    // ── Containers ───────────────────────────────────────────────────────────────────────────────

    private VariantDictionary ParseDictionaryBody()
    {
        VariantDictionary entries = new();
        bool atKey = true;
        Variant key = Variant.Nil.Instance;
        bool needComma = false;

        while (true)
        {
            if (_lexer.IsEof)
            {
                Fail(DiagnosticCode.UnexpectedEof, "Unexpected EOF while parsing dictionary", CurrentSpan());
            }

            Token token;
            if (atKey)
            {
                token = _lexer.GetToken();
                if (token.Type == TokenType.CurlyBracketClose)
                {
                    return entries;
                }

                if (needComma)
                {
                    if (token.Type != TokenType.Comma)
                    {
                        Fail(DiagnosticCode.ExpectedToken, "Expected '}' or ','", token.Span);
                    }

                    needComma = false;
                    continue;
                }

                key = ParseValue(token);

                token = _lexer.GetToken();
                if (token.Type != TokenType.Colon)
                {
                    Fail(DiagnosticCode.ExpectedColon, "Expected ':'", token.Span);
                }

                atKey = false;
            }
            else
            {
                token = _lexer.GetToken();
                entries.Set(key, ParseValue(token));
                needComma = true;
                atKey = true;
            }
        }
    }

    private List<Variant> ParseArrayBody()
    {
        List<Variant> items = [];
        bool needComma = false;

        while (true)
        {
            if (_lexer.IsEof)
            {
                Fail(DiagnosticCode.UnexpectedEof, "Unexpected EOF while parsing array", CurrentSpan());
            }

            Token token = _lexer.GetToken();
            if (token.Type == TokenType.BracketClose)
            {
                return items;
            }

            if (needComma)
            {
                if (token.Type != TokenType.Comma)
                {
                    Fail(DiagnosticCode.ExpectedComma, "Expected ','", token.Span);
                }

                needComma = false;
                continue;
            }

            items.Add(ParseValue(token));
            needComma = true;
        }
    }

    // ── Special constructors ─────────────────────────────────────────────────────────────────────

    private Variant.NodePath ParseNodePath()
    {
        Token token = _lexer.GetToken();
        if (token.Type != TokenType.ParenthesisOpen)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected '('", token.Span);
        }

        token = _lexer.GetToken();
        if (token.Type != TokenType.String)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected string as argument for NodePath()", token.Span);
        }

        Variant.NodePath result = new(IdentifierText(token));

        token = _lexer.GetToken();
        if (token.Type != TokenType.ParenthesisClose)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected ')'", token.Span);
        }

        return result;
    }

    private Variant.Rid ParseRid()
    {
        Token token = _lexer.GetToken();
        if (token.Type != TokenType.ParenthesisOpen)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected '('", token.Span);
        }

        token = _lexer.GetToken();
        if (token.Type == TokenType.ParenthesisClose)
        {
            return new Variant.Rid(0);
        }

        if (token.Type != TokenType.Number)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected number as argument or ')'", token.Span);
        }

        ulong id = ToUInt64(NumberValue(token));

        token = _lexer.GetToken();
        if (token.Type != TokenType.ParenthesisClose)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected ')'", token.Span);
        }

        return new Variant.Rid(id);
    }

    private TNode ParseEmptyConstructor<TNode>(TNode instance)
        where TNode : Variant
    {
        Token token = _lexer.GetToken();
        if (token.Type != TokenType.ParenthesisOpen)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected '('", token.Span);
        }

        token = _lexer.GetToken();
        if (token.Type != TokenType.ParenthesisClose)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected ')'", token.Span);
        }

        return instance;
    }

    private Variant.Object ParseObjectLiteral()
    {
        Token token = _lexer.GetToken();
        if (token.Type != TokenType.ParenthesisOpen)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected '('", token.Span);
        }

        token = _lexer.GetToken();
        if (token.Type != TokenType.Identifier)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected identifier with type of object", token.Span);
        }

        string className = IdentifierText(token);

        token = _lexer.GetToken();
        if (token.Type != TokenType.Comma)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected ',' after object type", token.Span);
        }

        EnterContainer(token.Span);
        try
        {
            List<KeyValuePair<string, Variant>> properties = [];
            bool atKey = true;
            string key = string.Empty;
            bool needComma = false;

            while (true)
            {
                if (_lexer.IsEof)
                {
                    Fail(DiagnosticCode.UnexpectedEof, "Unexpected EOF while parsing Object()", CurrentSpan());
                }

                if (atKey)
                {
                    token = _lexer.GetToken();
                    if (token.Type == TokenType.ParenthesisClose)
                    {
                        return new Variant.Object(className, properties);
                    }

                    if (needComma)
                    {
                        if (token.Type != TokenType.Comma)
                        {
                            Fail(DiagnosticCode.ExpectedToken, "Expected '}' or ','", token.Span);
                        }

                        needComma = false;
                        continue;
                    }

                    if (token.Type != TokenType.String)
                    {
                        Fail(DiagnosticCode.ExpectedToken, "Expected property name as string", token.Span);
                    }

                    key = IdentifierText(token);

                    token = _lexer.GetToken();
                    if (token.Type != TokenType.Colon)
                    {
                        Fail(DiagnosticCode.ExpectedColon, "Expected ':'", token.Span);
                    }

                    atKey = false;
                }
                else
                {
                    token = _lexer.GetToken();
                    properties.Add(new KeyValuePair<string, Variant>(key, ParseValue(token)));
                    needComma = true;
                    atKey = true;
                }
            }
        }
        finally
        {
            ExitContainer();
        }
    }

    private Variant.Resource ParseResourceLiteral(string keyword)
    {
        Token token = _lexer.GetToken();
        if (token.Type != TokenType.ParenthesisOpen)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected '('", token.Span);
        }

        token = _lexer.GetToken();
        if (token.Type != TokenType.String)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected string as argument for Resource()", token.Span);
        }

        string first = IdentifierText(token);
        string? uid = null;
        string? path = null;
        if (first.StartsWith("uid://", StringComparison.Ordinal))
        {
            uid = first;
        }
        else
        {
            path = first;
        }

        token = _lexer.GetToken();
        if (token.Type == TokenType.Comma)
        {
            token = _lexer.GetToken();
            if (token.Type != TokenType.String)
            {
                Fail(DiagnosticCode.ExpectedToken, "Expected string in Resource reference", token.Span);
            }

            string extra = IdentifierText(token);
            if (extra.StartsWith("uid://", StringComparison.Ordinal))
            {
                if (uid is not null)
                {
                    Fail(DiagnosticCode.InvalidResourceReference, "Two uid:// paths in one Resource reference", token.Span);
                }

                uid = extra;
            }
            else
            {
                if (path is not null)
                {
                    Fail(DiagnosticCode.InvalidResourceReference, "Two non-uid paths in one Resource reference", token.Span);
                }

                path = extra;
            }

            token = _lexer.GetToken();
        }

        if (token.Type != TokenType.ParenthesisClose)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected ')'", token.Span);
        }

        return new Variant.Resource(keyword, uid, path);
    }

    // ── Typed containers ─────────────────────────────────────────────────────────────────────────

    private Variant.Array ParseTypedArray(SourceSpan span)
    {
        Token token = _lexer.GetToken();
        if (token.Type != TokenType.BracketOpen)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected '['", token.Span);
        }

        token = _lexer.GetToken();
        if (token.Type != TokenType.Identifier)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected type identifier", token.Span);
        }

        VariantType? elementType = ResolveTypeName(IdentifierText(token), token, out Token next);

        if (next.Type != TokenType.BracketClose)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected ']'", next.Span);
        }

        token = _lexer.GetToken();
        if (token.Type != TokenType.ParenthesisOpen)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected '('", token.Span);
        }

        token = _lexer.GetToken();
        if (token.Type != TokenType.BracketOpen)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected '['", token.Span);
        }

        List<Variant> items = ParseArrayBody();

        token = _lexer.GetToken();
        if (token.Type != TokenType.ParenthesisClose)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected ')'", token.Span);
        }

        return new Variant.Array(ConvertElements(items, elementType, span), elementType);
    }

    private Variant.Dictionary ParseTypedDictionary(SourceSpan span)
    {
        Token token = _lexer.GetToken();
        if (token.Type != TokenType.BracketOpen)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected '['", token.Span);
        }

        token = _lexer.GetToken();
        if (token.Type != TokenType.Identifier)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected type identifier for key", token.Span);
        }

        VariantType? keyType = ResolveTypeName(IdentifierText(token), token, out Token next);

        if (next.Type != TokenType.Comma)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected ',' after key type", next.Span);
        }

        token = _lexer.GetToken();
        if (token.Type != TokenType.Identifier)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected type identifier for value", token.Span);
        }

        VariantType? valueType = ResolveTypeName(IdentifierText(token), token, out next);

        if (next.Type != TokenType.BracketClose)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected ']'", next.Span);
        }

        token = _lexer.GetToken();
        if (token.Type != TokenType.ParenthesisOpen)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected '('", token.Span);
        }

        token = _lexer.GetToken();
        if (token.Type != TokenType.CurlyBracketOpen)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected '{'", token.Span);
        }

        VariantDictionary entries = ParseDictionaryBody();

        token = _lexer.GetToken();
        if (token.Type != TokenType.ParenthesisClose)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected ')'", token.Span);
        }

        return new Variant.Dictionary(ConvertEntries(entries, keyType, valueType, span), keyType, valueType);
    }

    /// <summary>
    /// Resolves a type name inside <c>Array[...]</c> or <c>Dictionary[...]</c>.
    /// </summary>
    /// <remarks>
    /// <paramref name="next"/> is the token that follows the type name: either <c>]</c>/<c>,</c>, or the token
    /// after a consumed <c>Resource("...")</c> argument list. Godot decides this by catching a parse failure and
    /// comparing the error string; this library reads it directly, and does not keep the class name, so containers
    /// typed by class become <see cref="VariantType.Object"/> (deviation D8).
    /// </remarks>
    private VariantType? ResolveTypeName(string name, Token token, out Token next)
    {
        if (TypeNames.TryGetValue(name, out VariantType builtin))
        {
            next = _lexer.GetToken();
            return builtin;
        }

        if (name is "Resource" or "SubResource" or "ExtResource")
        {
            next = _lexer.GetToken();
            if (next.Type == TokenType.ParenthesisOpen)
            {
                ParseResourceReferenceArguments();
                next = _lexer.GetToken();
            }

            return VariantType.Object;
        }

        _diagnostics.Warning(
            DiagnosticCode.UnknownTypeName,
            $"Unknown type name '{name}'; the container is treated as untyped",
            token.Span);
        next = _lexer.GetToken();
        return null;
    }

    /// <summary>Consumes the arguments of a resource reference when it appears in a type position.</summary>
    private void ParseResourceReferenceArguments()
    {
        Token token = _lexer.GetToken();
        if (token.Type != TokenType.String)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected string as argument for Resource()", token.Span);
        }

        token = _lexer.GetToken();
        if (token.Type == TokenType.Comma)
        {
            token = _lexer.GetToken();
            if (token.Type != TokenType.String)
            {
                Fail(DiagnosticCode.ExpectedToken, "Expected string in Resource reference", token.Span);
            }

            token = _lexer.GetToken();
        }

        if (token.Type != TokenType.ParenthesisClose)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected ')'", token.Span);
        }
    }

    // ── Packed arrays ────────────────────────────────────────────────────────────────────────────

    private Variant ParsePackedByteArray()
    {
        Token token = _lexer.GetToken();
        if (token.Type != TokenType.ParenthesisOpen)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected '(' in constructor", token.Span);
        }

        token = _lexer.GetToken();

        if (token.Type == TokenType.String)
        {
            string encoded = IdentifierText(token);
            byte[] decoded = new byte[(encoded.Length / 4 * 3) + 3];
            if (!Convert.TryFromBase64String(encoded, decoded, out int written))
            {
                Fail(DiagnosticCode.InvalidBase64, "Invalid base64-encoded string", token.Span);
                return Variant.Nil.Instance;
            }

            token = _lexer.GetToken();
            if (token.Type != TokenType.ParenthesisClose)
            {
                Fail(DiagnosticCode.ExpectedToken, "Expected ')' in constructor", token.Span);
            }

            return new Variant.PackedByteArray(decoded[..written]);
        }

        if (token.Type == TokenType.ParenthesisClose)
        {
            return new Variant.PackedByteArray([]);
        }

        if (token.Type != TokenType.Number && token.Type != TokenType.Identifier)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected base64 string, or list of numbers in constructor", token.Span);
        }

        List<byte> bytes = [];
        while (true)
        {
            bytes.Add(ToByteValue(token));

            token = _lexer.GetToken();
            if (token.Type == TokenType.Comma)
            {
                // Keep reading elements.
            }
            else if (token.Type == TokenType.ParenthesisClose)
            {
                break;
            }
            else
            {
                Fail(DiagnosticCode.ExpectedToken, "Expected ',' or ')' in constructor", token.Span);
            }

            token = _lexer.GetToken();
        }

        return new Variant.PackedByteArray(bytes);
    }

    private Variant.PackedStringArray ParsePackedStringArray()
    {
        Token token = _lexer.GetToken();
        if (token.Type != TokenType.ParenthesisOpen)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected '('", token.Span);
        }

        List<string> items = [];
        bool first = true;

        while (true)
        {
            if (!first)
            {
                token = _lexer.GetToken();
                if (token.Type == TokenType.Comma)
                {
                    // A trailing comma is accepted here, unlike in every other packed array.
                }
                else if (token.Type == TokenType.ParenthesisClose)
                {
                    break;
                }
                else
                {
                    Fail(DiagnosticCode.ExpectedToken, "Expected ',' or ')'", token.Span);
                }
            }

            token = _lexer.GetToken();
            if (token.Type == TokenType.ParenthesisClose)
            {
                break;
            }

            if (token.Type != TokenType.String)
            {
                Fail(DiagnosticCode.ExpectedToken, "Expected string", token.Span);
            }

            first = false;
            items.Add(IdentifierText(token));
        }

        return new Variant.PackedStringArray(items);
    }

    // ── Typed containers ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies a declared element type to the elements of an <c>Array[T](...)</c> literal.
    /// </summary>
    /// <remarks>
    /// Godot converts every element and, when one of them cannot be converted, prints an error and leaves the
    /// converted array empty (<c>Array::assign</c>). That outcome is reproduced here; the failure is also reported
    /// as a warning rather than only being printed.
    /// </remarks>
    private List<Variant> ConvertElements(List<Variant> items, VariantType? elementType, SourceSpan span)
    {
        if (elementType is not VariantType target)
        {
            return items;
        }

        Variant[] converted = new Variant[items.Count];
        for (int i = 0; i < converted.Length; i++)
        {
            if (!TryConvertElement(items[i], target, out Variant value))
            {
                _diagnostics.Warning(
                    DiagnosticCode.IncompatibleElement,
                    $"Unable to convert array index {i} from '{GodotTypeName(items[i].Kind)}' to '{GodotTypeName(target)}'.",
                    span);
                return [];
            }

            converted[i] = value;
        }

        return [.. converted];
    }

    /// <summary>Applies the declared key and value types to a <c>Dictionary[K, V](...)</c> literal.</summary>
    /// <remarks>
    /// Godot's <c>Dictionary::assign</c> behaves like <c>Array::assign</c>: when a conversion fails, the result is
    /// an empty dictionary and an error is printed.
    /// </remarks>
    private VariantDictionary ConvertEntries(VariantDictionary entries, VariantType? keyType, VariantType? valueType, SourceSpan span)
    {
        if (keyType is null && valueType is null)
        {
            return entries;
        }

        VariantDictionary converted = new();

        foreach (KeyValuePair<Variant, Variant> entry in entries)
        {
            Variant key = entry.Key;
            Variant value = entry.Value;

            if (keyType is VariantType targetKey && !TryConvertElement(key, targetKey, out key))
            {
                _diagnostics.Warning(
                    DiagnosticCode.IncompatibleElement,
                    $"Unable to convert key from '{GodotTypeName(entry.Key.Kind)}' to '{GodotTypeName(targetKey)}'.",
                    span);
                return new VariantDictionary();
            }

            if (valueType is VariantType targetValue && !TryConvertElement(value, targetValue, out value))
            {
                _diagnostics.Warning(
                    DiagnosticCode.IncompatibleElement,
                    $"Unable to convert value at key '{Describe(entry.Key)}' from '{GodotTypeName(entry.Value.Kind)}' to '{GodotTypeName(targetValue)}'.",
                    span);
                return new VariantDictionary();
            }

            converted.Set(key, value);
        }

        return converted;
    }

    /// <summary>Converts one element to the type its container declared for it.</summary>
    /// <remarks>
    /// Only the conversions that show up in real files are implemented: an element that already has the declared
    /// kind, the numeric pair Int/Float, and the string family String/StringName. Godot's strict conversion table
    /// is wider (Color from Vector3, for instance); a pair outside this set counts as a conversion failure.
    /// </remarks>
    private static bool TryConvertElement(Variant value, VariantType target, out Variant converted)
    {
        if (value.Kind == target)
        {
            converted = value;
            return true;
        }

        switch ((Value: value, Target: target))
        {
            case (Variant.Int number, VariantType.Float):
                converted = new Variant.Float(number.Value);
                return true;
            case (Variant.Float number, VariantType.Int):
                converted = new Variant.Int(ToInt64(number.Value));
                return true;
            case (Variant.Str text, VariantType.StringName):
                converted = new Variant.StringName(text.Value);
                return true;
            case (Variant.StringName text, VariantType.String):
                converted = new Variant.Str(text.Value);
                return true;
            default:
                converted = Variant.Nil.Instance;
                return false;
        }
    }

    private static string GodotTypeName(VariantType type) =>
        type == VariantType.Nil ? "Nil" : TypeNamesByType[type];

    /// <summary>
    /// Formats a value the way Godot's <c>%s</c> does, for the messages this library copies from the engine:
    /// a string-like value contributes its text rather than a quoted rendering.
    /// </summary>
    private static string Describe(Variant value) => value switch
    {
        Variant.Str text => text.Value,
        Variant.StringName text => text.Value,
        Variant.NodePath text => text.Value,
        _ => value.ToString(),
    };

    // ── Argument helpers ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a flat constructor argument list, the equivalent of Godot's <c>_parse_construct&lt;T&gt;</c>.
    /// </summary>
    private List<double> ParseConstructArguments()
    {
        Token token = _lexer.GetToken();
        if (token.Type != TokenType.ParenthesisOpen)
        {
            Fail(DiagnosticCode.ExpectedToken, "Expected '(' in constructor", token.Span);
        }

        List<double> arguments = [];
        bool first = true;

        while (true)
        {
            if (!first)
            {
                token = _lexer.GetToken();
                if (token.Type == TokenType.Comma)
                {
                    // Keep reading elements.
                }
                else if (token.Type == TokenType.ParenthesisClose)
                {
                    break;
                }
                else
                {
                    Fail(DiagnosticCode.ExpectedToken, "Expected ',' or ')' in constructor", token.Span);
                }
            }

            token = _lexer.GetToken();
            if (first && token.Type == TokenType.ParenthesisClose)
            {
                break;
            }

            arguments.Add(RequireNumber(token, "Expected float in constructor"));
            first = false;
        }

        return arguments;
    }

    private Variant Construct(Token token, int count, Func<IReadOnlyList<double>, Variant> build)
    {
        List<double> arguments = ParseConstructArguments();
        if (arguments.Count != count)
        {
            Fail(DiagnosticCode.ConstructorArgumentCount, $"Expected {count} arguments for constructor", token.Span);
        }

        return build(arguments);
    }

    private double RequireNumber(Token token, string message)
    {
        if (token.Type == TokenType.Number)
        {
            return NumberValue(token);
        }

        if (token.Type == TokenType.Identifier && TryStorFix(IdentifierText(token), out double special))
        {
            return special;
        }

        Fail(DiagnosticCode.ConstructorArgumentType, message, token.Span);
        return 0;
    }

    /// <summary>Godot's <c>stor_fix</c>: the only identifiers a numeric argument may be.</summary>
    private static bool TryStorFix(string id, out double value)
    {
        switch (id)
        {
            case "inf":
                value = double.PositiveInfinity;
                return true;
            case "-inf":
            case "inf_neg":
                value = double.NegativeInfinity;
                return true;
            case "nan":
                value = double.NaN;
                return true;
            default:
                value = -1;
                return false;
        }
    }

    private void EnterContainer(SourceSpan span)
    {
        _depth++;
        if (_depth > _options.MaxDepth)
        {
            Fail(DiagnosticCode.DepthLimitExceeded, $"Exceeded the maximum nesting depth of {_options.MaxDepth}", span);
        }
    }

    private void ExitContainer() => _depth--;

    private static T[] ToSequence<T>(List<double> arguments, Func<double, T> convert)
    {
        T[] result = new T[arguments.Count];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = convert(arguments[i]);
        }

        return result;
    }

    private static Vector2[] GroupVectors2(List<double> arguments)
    {
        Vector2[] result = new Vector2[arguments.Count / 2];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = new Vector2((float)arguments[i * 2], (float)arguments[(i * 2) + 1]);
        }

        return result;
    }

    private static Vector3[] GroupVectors3(List<double> arguments)
    {
        Vector3[] result = new Vector3[arguments.Count / 3];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = new Vector3((float)arguments[i * 3], (float)arguments[(i * 3) + 1], (float)arguments[(i * 3) + 2]);
        }

        return result;
    }

    private static Vector4[] GroupVectors4(List<double> arguments)
    {
        Vector4[] result = new Vector4[arguments.Count / 4];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = new Vector4(
                (float)arguments[i * 4],
                (float)arguments[(i * 4) + 1],
                (float)arguments[(i * 4) + 2],
                (float)arguments[(i * 4) + 3]);
        }

        return result;
    }

    private static Color[] GroupColors(List<double> arguments)
    {
        Color[] result = new Color[arguments.Count / 4];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = new Color(
                (float)arguments[i * 4],
                (float)arguments[(i * 4) + 1],
                (float)arguments[(i * 4) + 2],
                (float)arguments[(i * 4) + 3]);
        }

        return result;
    }

    private static int ToInt32(double value)
    {
        if (double.IsNaN(value))
        {
            return 0;
        }

        if (value >= int.MaxValue)
        {
            return int.MaxValue;
        }

        return value <= int.MinValue ? int.MinValue : (int)value;
    }

    private static long ToInt64(double value)
    {
        if (double.IsNaN(value))
        {
            return 0;
        }

        if (value >= long.MaxValue)
        {
            return long.MaxValue;
        }

        return value <= long.MinValue ? long.MinValue : (long)value;
    }

    private static byte ToByte(double value)
    {
        if (double.IsNaN(value) || value <= 0)
        {
            return 0;
        }

        return value >= byte.MaxValue ? byte.MaxValue : (byte)value;
    }

    /// <summary>
    /// Converts one element of a <c>PackedByteArray(...)</c> numeric list.
    /// </summary>
    /// <remarks>
    /// Godot's Variant-to-uint8 conversion keeps the low eight bits of an integer (so <c>-1</c> is 255) and
    /// truncates a float. A float outside 0..255 is undefined behaviour there; here it saturates.
    /// </remarks>
    private byte ToByteValue(Token token)
    {
        if (token.Type == TokenType.Number)
        {
            return token.Value switch
            {
                Variant.Int i => unchecked((byte)i.Value),
                Variant.Float f => ToByte(f.Value),
                _ => 0,
            };
        }

        if (token.Type == TokenType.Identifier && TryStorFix(IdentifierText(token), out double special))
        {
            return ToByte(special);
        }

        Fail(DiagnosticCode.ConstructorArgumentType, "Expected number in constructor", token.Span);
        return 0;
    }

    private static ulong ToUInt64(double value)
    {
        if (double.IsNaN(value) || value <= 0)
        {
            return 0;
        }

        return value >= ulong.MaxValue ? ulong.MaxValue : (ulong)value;
    }
}
