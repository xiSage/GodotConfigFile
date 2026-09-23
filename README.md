[![NuGet Version](https://img.shields.io/nuget/v/xiSage.GodotConfigFile)](https://www.nuget.org/packages/xiSage.GodotConfigFile)

# GodotConfigFile

A read-only parser for Godot's `ConfigFile` text format — the format of `project.godot`, `override.cfg`,
`export_presets.cfg` and the editor caches under `.godot/`.

- **Faithful to Godot's grammar**, including the parts that surprise people. See
  [`docs/godot-configfile-spec.md`](docs/godot-configfile-spec.md), which was derived from the engine source and
  is the factual baseline for this implementation.
- **Parse-only.** There is no `Save` and no `EncodeToText`; the model is immutable.
- **`net10.0`, zero runtime dependencies**, no reflection anywhere, so it is trim- and AOT-clean.
- **Every deliberate difference from Godot is documented and tested** — see
  [`docs/compat-deviations.md`](docs/compat-deviations.md).

## Install

```bash
dotnet add package xiSage.GodotConfigFile
```

## Quick start

```csharp
using GodotConfigFile;

ConfigFileDocument document = ConfigFile.Parse("""
    [application]

    config/name="My Game"
    config/features=PackedStringArray("4.7", "GL Compatibility")

    [rendering]

    renderer/rendering_method="gl_compatibility"
    """);

string name = document.GetValue("application", "config/name", "Untitled");
string[] features = document.GetValue<string[]>("application", "config/features", []);

if (document.TryGetVariant("rendering", "renderer/rendering_method", out Variant method) &&
    method is Variant.Str text)
{
    Console.WriteLine($"{name} / {string.Join(", ", features)} / {text.Value}");
}
```

Reading a file is the same, with the bytes handled for you:

```csharp
ConfigFileDocument settings = ConfigFile.Load("project.godot");
```

## The model

| Type | What it is |
| --- | --- |
| `ConfigFileDocument` | An immutable, insertion-ordered map of sections. Top-level keys live in the section named `ConfigFile.RootSection`, which is the empty string. |
| `ConfigSection` | An insertion-ordered map of key names to values. Key names are case-sensitive. |
| `Variant` | A parsed value: an abstract base class with one nested sealed class per kind (`Variant.Int`, `Variant.Str`, `Variant.Array`, …), plus a `Kind` property for quick dispatch. |
| `VariantDictionary` | A dictionary value: insertion-ordered, arbitrary keys, Godot's key semantics. |
| `VariantComparer` | The comparer that gives `VariantDictionary` its Godot key semantics. Useful if you build such a map yourself. |

Values are matched with pattern matching:

```csharp
Variant value = document.GetSection("input")!["ui_accept"]!;

if (value is Variant.Dictionary dictionary &&
    dictionary.Items.TryGetValue(new Variant.Str("deadzone"), out Variant deadzone) &&
    deadzone is Variant.Float number)
{
    Console.WriteLine(number.Value);
}
```

### Equality

`Variant` equality is type-sensitive and structural: `1` and `1.0` are different values, containers compare by
content, and NaN equals NaN. It matches Godot's *key comparison*, not the numeric promotion GDScript performs for
`==` — see deviation D12.

`VariantComparer` is slightly wider, because that is what Godot's dictionary key table does: a `Variant.Str` and a
`Variant.StringName` with the same text are the same key, so `{"a": 1, &"a": 2}` is one entry. Everything else stays
type-sensitive: `1` and `1.0` are two keys, and a `NodePath` is never merged with a string.

## Reading values

`GetValue<T>` and `TryGetValue<T>` convert to a closed set of types. Conversion is explicit, and **values are never
converted through strings**: a string holding `"5"` is not the number 5, so a mistyped value fails the lookup
instead of silently succeeding.

| Target | Accepted from |
| --- | --- |
| `bool` | `Variant.Bool` |
| `int`, `long` | `Variant.Int`, range-checked against the target |
| `double`, `float` | `Variant.Float`, `Variant.Int` |
| `string` | `Variant.Str`, `Variant.StringName`, `Variant.NodePath` |
| `Color`, `Vector2`, `Vector2I`, `Rect2`, `Rect2I`, `Vector3`, `Vector3I`, `Vector4`, `Vector4I`, `Transform2D`, `Plane`, `Quaternion`, `Aabb`, `Basis`, `Transform3D`, `Projection` | the matching node |
| `Variant`, `VariantDictionary` | the value itself |
| `Variant[]` | `Variant.Array` |
| `byte[]` | `Variant.PackedByteArray` |
| `string[]`, `int[]`, `long[]`, `float[]`, `double[]`, `bool[]`, `Color[]` | the matching packed array, or a `Variant.Array` whose elements all convert |

Anything else — an unsupported type, a kind mismatch, or an out-of-range number — returns the caller's default from
`GetValue<T>` and `false` from `TryGetValue<T>`.

## Merging

`Merge` returns a new document with another laid over it, which is how Godot's "parse merges into the existing
`ConfigFile`" behaviour is expressed without mutable state:

```csharp
ConfigFileDocument effective = ConfigFile.Load("project.godot")
    .Merge(ConfigFile.Load("override.cfg", new ParseOptions { PreserveNilKeys = true }));
```

A key already present keeps its original position; new keys are appended. A `null` in the overlay **deletes** the
key (and the section, when it becomes empty), which is why the overlay has to be parsed with
`PreserveNilKeys` — without it, a top-level `key=null` erases the key in that document and leaves no trace of the
deletion.

## Diagnostics

Errors throw `ConfigFileParseException`, which carries a `Diagnostic` (a stable `DiagnosticCode`, Godot's own
message wording, and a `SourceSpan`), the full list of diagnostics, and `PartialDocument` — what had been read
before the error, because a failed parse is not transactional, exactly as in Godot.

`ConfigFile.ParseWithResult` never throws: it returns a `ParseResult` with the document, the diagnostics and a
`Success` flag. `ParseOptions.ThrowOnError = false` makes the convenience entry points behave the same way.

Non-fatal diagnostics matter too: a stripped byte order mark, invalid UTF-8, an integer overflow, an unknown type
name, a malformed colour literal or a duplicated key are reported as `Info`/`Warning` while the parse continues.

## Entry points

| Method | Input |
| --- | --- |
| `ConfigFile.Parse(string, ParseOptions?)` | text |
| `ConfigFile.Parse(ReadOnlySpan<char>, ParseOptions?)` | text |
| `ConfigFile.ParseUtf8(ReadOnlySpan<byte>, ParseOptions?)` | UTF-8 bytes |
| `ConfigFile.Load(string, ParseOptions?)` | a file, read as bytes |
| `ConfigFile.ParseWithResult` / `ParseUtf8WithResult` | the same, without throwing |

`ParseUtf8` decodes the whole input once; bytes that are not valid UTF-8 become U+FFFD and produce a warning instead
of failing the parse. A leading byte order mark is stripped and reported (deviation D1) — Godot leaves it in place and
it then fuses with the first key name.

`ParseOptions` also carries `MaxDepth` (default 100, matching Godot's writer; Godot's parser has no limit at all and
can overflow the stack — deviation D5).

## Format notes worth knowing

These are Godot's rules, reproduced as-is:

- **Only `;` starts a comment.** `#` is a colour literal, so `# comment` becomes part of the next key name and
  `#ff0000=1` defines the key `#ff0000`.
- **Newlines do not end statements.** A key name swallows whitespace, runs across newlines and across `;` comments,
  and picks up whatever trails the previous value: `a=1 remainder` followed by `b=2` defines the keys `a` and
  `remainderb`. Keep one assignment per line.
- **A `[` only starts a section header when nothing has been accumulated yet**, so `x[y]=1` has the key `x[y]`.
- **`null` is not a value at the top level**: `key=null` erases the key, and erases the section when it becomes empty.
  Inside arrays and dictionaries a null is kept.
- **`1` and `1.0` are different types**, and `1e`, `1e+` and `1.` are all valid floats.
- **Trailing commas are only allowed where Godot allows them**: arrays, dictionaries, `Object(...)` and
  `PackedStringArray(...)` accept one; built-in constructors and the other packed arrays do not.
- **`\u`/`\U` escapes beyond ASCII are decoded once**, unlike Godot's `load()` path, which decodes them twice —
  see deviation D3.

## Not implemented

Writing is out of scope, by design: there is no serializer, and `Variant.ToString()` is a debugging aid that is not
guaranteed to parse back. Resources are never loaded: `Resource(...)`, `SubResource(...)`, `ExtResource(...)` and
`Object(...)` become opaque `Variant.Resource` / `Variant.Object` nodes (deviation D7), so a file that references a
resource that does not exist still parses.

## Documentation

| Document | Contents |
| --- | --- |
| [`docs/godot-configfile-spec.md`](docs/godot-configfile-spec.md) | The full grammar, derived from the Godot source, including every quirk and the file/line it comes from. |
| [`docs/compat-deviations.md`](docs/compat-deviations.md) | Every deliberate difference from Godot, with the test that pins it down. |

## License

MIT — see [LICENSE.txt](LICENSE.txt).
