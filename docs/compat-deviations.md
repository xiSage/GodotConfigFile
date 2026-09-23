# Deliberate deviations from Godot

This library reproduces Godot's `ConfigFile` grammar faithfully: the rules of the format itself — `;` comments,
`#` colour literals, key names that swallow whitespace and cross newlines, `null` erasing a key, the asymmetries
around trailing commas, the old constructor aliases, NUL handling, the way `1x` splits into `1` and `x` — are all
copied, quirks included. The format is the contract.

What follows is the short list of places where this implementation deliberately does **not** do what Godot does.
Every entry has a test in `GodotConfigFileTests/CompatDeviationTests.cs` (and, for D4 and D10, additional coverage
in the lexer and API tests).

Baseline: `D:\Repos\godot` at commit `e2b42469291c6889f93ad83debb5d24f607d26e3`. Details and file/line references
for each behaviour are in [`godot-configfile-spec.md`](godot-configfile-spec.md).

---

## D1 — A byte order mark is stripped

**Godot:** does not strip a BOM. The three bytes `EF BB BF` are read as three ordinary characters and fuse onto the
first key name, which usually destroys the file's first section.

**Here:** a leading U+FEFF is removed and reported as an `Info` diagnostic (`DiagnosticCode.BomStripped`). Set
`ParseOptions.StripBom = false` to get Godot's behaviour.

*Test:* `D1_ByteOrderMarkIsStripped`, plus `ByteOrderMarksCanBeKeptAndThenGlueOntoTheKey` for the opt-out.

## D2 — Key names are decoded as UTF-8

**Godot:** the `load()` path reads bytes, one byte per code point, and decodes only string literals and section
names. A non-ASCII key written without quotes therefore comes back as mojibake, while the same key written as a
quoted string is fine. Godot's own `parse(String)` entry point does not have this problem — the two entry points
disagree.

**Here:** the input is decoded once, so `静音=1` and `"静音"=1` produce the same key. This matches Godot's
`parse(String)` path.

*Test:* `D2_UnquotedNonAsciiKeyIsDecoded`.

## D3 — `\u` and `\U` escapes are decoded once

**Godot:** after reading a string literal, the `load()` path re-interprets what it built as bytes and decodes it as
UTF-8 again. An escape that produced a code point above 0x7F is mangled by that second pass: `\u00e9` becomes a
replacement character and `\u4e2d` becomes a space. Again, `parse(String)` does not do this.

**Here:** the escape is decoded once, so `\u00e9` is `é`. This matches Godot's `parse(String)` path.

*Test:* `D3_UnicodeEscapesAreNotDoubleDecoded`.

## D4 — The input is decoded once, up front

**Godot:** the `load()` path keeps a byte-per-code-point view, so a file that is not valid UTF-8 is still parsed,
with the damaged bytes reinterpreted according to the stream's `is_utf8()` behaviour in each separate place
(string literals, section names, key names, each differently).

**Here:** `ParseUtf8` decodes the whole input once with replacement fallback. Bytes that are not valid UTF-8 become
U+FFFD and produce a `Warning` (`DiagnosticCode.InvalidUtf8Byte`); parsing continues. There is no Latin-1 fallback.
`ParseUtf8` and `Parse` therefore agree, which is the point of D2–D4 together.

*Test:* `D4_InputIsDecodedOnce`, plus `InvalidUtf8BytesBecomeReplacementCharactersWithAWarning`.

## D5 — Nesting depth is limited

**Godot:** `VariantParser` has no depth parameter for arrays and dictionaries; `MAX_RECURSION` (100) applies to the
*writer* only. A deeply nested input overflows the stack.

**Here:** `ParseOptions.MaxDepth` defaults to 100 and exceeding it is an `Error`
(`DiagnosticCode.DepthLimitExceeded`), so parsing stops instead of crashing.

*Test:* `D5_NestingBeyondTheDepthLimitIsAnError`, plus `DeeplyNestedInputDoesNotOverflowTheStack`.

## D6 — Integer overflow saturates to the correct bound

**Godot:** `String::to_int` saturates and prints an error, but its overflow check only triggers after the 19th digit,
so a 19-digit literal that does not fit silently yields `INT64_MIN`. (Godot's own clamp test does not catch this: it
reuses a stream whose `eof` flag is sticky, so the second parse never runs.)

**Here:** the digits are accumulated with an exact bound, so the result is `long.MaxValue` for a positive overflow
and `long.MinValue` for a negative one, with a `Warning` (`DiagnosticCode.IntegerOverflow`). Parsing continues, as
it does in Godot.

*Test:* `D6_IntegerOverflowSaturatesCorrectly`, plus `IntegerOverflowSaturatesAndWarns` and
`NineteenDigitOverflowIsFixedToTheRightBound`.

## D7 — `Object(...)` and resource references are opaque

**Godot:** `Object(...)` is instantiated through `ClassDB`, so an unknown class fails the whole parse. `Resource`,
`SubResource` and `ExtResource` are handed to `ResourceLoader::load()` — `ConfigFile` passes no resource parser — so
a reference to a file that is not present also fails the whole parse. That is why `ExtResource("1_abc")` is unusable
in a plain `ConfigFile`.

**Here:** both become opaque nodes. `Object(...)` is a `Variant.Object` carrying the class name and the property
assignments in source order; resource references are a `Variant.Resource` carrying the keyword and whichever of the
`uid://` reference and the path were written. Nothing is instantiated and nothing is loaded, so these literals never
fail. The grammar around them — the mandatory comma after the class name, string-only property names, the two-path
conflict errors — is still enforced exactly as Godot enforces it.

*Test:* `D7_ObjectsAndResourcesAreOpaqueAndNeverFail`.

## D8 — Typed containers use one rule for both sides

**Godot:** for `Array[T]`, an unrecognised type name leaves the array untyped, but for `Dictionary[K, V]` a single
recognised side is enough to call `set_typed`, leaving the other side as `NIL`. The only way that asymmetry is
observable is `Dictionary::is_typed()`, which returns `true` in both cases.

**Here:** `ElementType`, `KeyType` and `ValueType` are plain `VariantType?` values where `null` means "not
specified", and the asymmetry is not reproduced. An unrecognised type name produces a `Warning`
(`DiagnosticCode.UnknownTypeName`) and leaves the container untyped — including class names such as `Array[Node]`,
which cannot be resolved without `ClassDB`, so the type hint is reported as unknown rather than guessed. The three
resource keywords are the exception: `Array[Resource]` and its siblings are typed as `VariantType.Object`, which is
what Godot resolves them to.

**Elements are converted, and a failure empties the container.** Godot's `Array::assign` and `Dictionary::assign`
convert each element to the declared type and, when one cannot be converted, print an error and leave the container
empty. Both halves are reproduced: the conversion covers an element that already has the declared kind, the numeric
pair `Int`/`Float`, and the string family `String`/`StringName`; anything else empties the container, and the failure
is reported as a `Warning` (`DiagnosticCode.IncompatibleElement`) instead of only being printed. Pairs that Godot's
wider strict-conversion table accepts — a `Color` from a `Vector3`, say — count as failures here.

*Tests:* `D8_TypedContainerAsymmetryIsNotCopied`, `UnknownTypeNamesMakeTheContainerUntyped`,
`TypedContainersConvertTheirElements`, `UnconvertibleElementsEmptyTheContainerAndWarn`,
`UnconvertibleDictionaryEntriesEmptyTheDictionary`.

## D9 — Line numbers count the real text

**Godot:** the line counter is not advanced by newlines inside a section header, so error messages after such a
header point at the wrong line.

**Here:** every newline counts, so `SourcePosition.Line` always matches the text as written.

*Test:* `D9_LinesFollowTheRealText`.

## D10 — A malformed colour literal is reported

**Godot:** a `#` literal whose length is not 3, 4, 6 or 8 produces `Color()` (opaque black) and prints an error to
the log, but the parse continues and the caller is not told.

**Here:** the value is the same opaque black and the parse continues, but a `Warning`
(`DiagnosticCode.MalformedHexColor`) is added to the diagnostics. This is a reporting difference, not a behavioural
one.

*Test:* `D10_MalformedColourIsBlackWithAWarning`.

## D11 — Composite keys hash and compare consistently

**Godot:** `Dictionary::recursive_hash` hashes entries in insertion order while `recursive_equal` compares them
without order. Two dictionaries with the same content but a different key order are therefore *equal* yet hash
differently, so a dictionary used as a key can end up as two separate entries.

**Here:** hashing is order-independent, so hash and equality agree and the two dictionaries are one key.

*Test:* `D11_CompositeKeysHashAndCompareConsistently`.

## D12 — NaN equals NaN

**Godot:** has two equality rules. The key path (`hash_compare` with `semantic = true`) treats NaN as equal to NaN,
which is what makes a NaN key findable; the value path used by `Dictionary::operator==` (`semantic = false`) treats
it as unequal, so two dictionaries holding NaN values compare unequal even though their keys are findable.

**Here:** `Variant` equality follows the key path — NaN equals NaN, everywhere, including inside geometry
components. That keeps `Equals` and `GetHashCode` consistent, which is required for `VariantDictionary`. The value
path's NaN rule is not reproduced.

*Test:* `D12_NaNEqualsNaN`, plus `NaNEqualsNaN` in the value-model tests.

---

## Not a deviation: the API rule about strings and numbers

`GetValue<T>` never converts through strings: a `Variant.Str` holding `"5"` is not converted to `5`, and
`TryGetValue<int>` returns `false` for it. This is a rule of this API rather than a difference from Godot, whose
`Variant` conversion operators would happily do it. It is called out here because it is the one place where a caller
might expect a conversion that will not happen.

*Test:* `StringIsNeverImplicitlyConvertedToNumber`.

## Appendix: behaviour Godot leaves unspecified

Three corners are undefined in Godot and defined here, which is not a deviation so much as a decision:

- **`PackedByteArray` elements.** Godot's integer conversion keeps the low eight bits (so `-1` becomes 255) and its
  float conversion truncates; a float outside 0..255 is undefined behaviour. Here integers keep the low eight bits
  exactly as Godot does, and an out-of-range or non-finite float saturates to 0..255.
- **Out-of-range constructor arguments.** `Vector2i(1e30)` and friends saturate to the integer bounds rather than
  being undefined, as does `RID(1e30)`.
- **Whitespace inside base64.** Godot decodes through mbedtls, which accepts line breaks and trailing spaces but
  rejects a space *inside* the data; the .NET decoder used here ignores whitespace anywhere. Invalid data is a fatal
  `InvalidBase64` either way, and the length rule is the same (`"AQI"`, three characters, is invalid for Godot
  because the digits plus padding must be a multiple of four).
