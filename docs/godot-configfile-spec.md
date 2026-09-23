# Godot ConfigFile Syntax Specification (reference for cross-language reimplementation)

**Baseline**: `D:\Repos\godot`, commit `e2b42469291c6889f93ad83debb5d24f607d26e3` (`version.py` = 4.8.0 dev, `git describe` = `4.7-stable-2463-ge2b4246929`).

**Source files relied on**

| File | Role |
| --- | --- |
| `core/io/config_file.cpp` | ConfigFile's storage model, the `_parse` top-level loop, `save`/`encode_to_text` |
| `core/variant/variant_parser.cpp` | the lexer `get_token`, value parsing `parse_value`, top-level reading `parse_tag_assign_eof`, writing `VariantWriter::write` |
| `core/variant/variant_parser.h` | Token types, the `parse_tag_assign_eof(..., p_simple_tag)` signature |
| `core/string/ustring.cpp` | string escaping, `property_name_encode`, number formatting |
| `core/math/color.cpp` | `Color::html` (`#rgb/#rgba/#rrggbb/#rrggbbaa`) |
| `tests/core/io/test_config_file.cpp` | official behavior samples (including the precisely finalized save output) |

**Key conclusion (read this one first)**: the syntax of `ConfigFile` = the lexer of `VariantParser` + `parse_tag_assign_eof(..., p_simple_tag = true)`.
`project.godot` takes a **completely identical** path (`ProjectSettings::_load_settings_text`, `core/config/project_settings.cpp:986`),
so this specification is at the same time the text format specification of `project.godot` (the only difference being that ProjectSettings joins `section/key` into the full property name).

---

## 1. File model and encoding

1. The file is **text**; the parser reads it character by character into `char32_t`.
2. The two entry points differ in their byte/character viewpoint:
   - `load()` / `load_encrypted*()`: uses `StreamFile`, reads **byte-wise**, each byte treated as one `char32_t` (equivalent to a Latin-1 view),
     `is_utf8() == true`.
   - `parse(String)`: uses `StreamString`, parses directly on already-decoded code points, `is_utf8() == false`.
3. UTF-8 decoding happens only on **string literals** and **section names**:
   - String literals: after reading, if `is_utf8()`, `str.ascii(true)` → `append_utf8(...)` is executed (`variant_parser.cpp:400-405`), doing one "bytes → UTF-8 decoding" pass.
   - section names: in the simple branch of `_parse_tag`, a `CharString` accumulation + `append_utf8` is done on an `is_utf8()` stream (`variant_parser.cpp:1643-1665`).
   - **Key names are not decoded** (`what += c` raw code points, `variant_parser.cpp:1823`). ⇒ a non-ASCII key name written bare in the file turns into mojibake and must be quoted.
4. **No BOM handling**: the `load` path does not recognize `EF BB BF`; it is treated as 3 ordinary characters and glued onto the first key name (which invalidates the file's first section).
   For tolerance, implementers are advised to strip the BOM themselves (Godot itself does not).
5. Line endings: `\r` belongs to the "whitespace characters ≤ 32" and is discarded between key names/values (CRLF-safe); but **a `\r` inside a string literal is preserved verbatim**,
   so a multi-line string in a CRLF file carries `\r`.
6. A newline is used only for counting line numbers and for separation; it is not a statement terminator (see the gluing (key names run together across lines) rules in §5).

---

## 2. Top-level structure

```
file        ::= item*
item        ::= section | assignment
section     ::= '[' section-body ']'
assignment  ::= key '=' value
```

Top-level loop (`ConfigFile::_parse`, `config_file.cpp:284-302`) pseudocode:

```text
section = ""
loop:
    assign = ""  ; tag.name = "" ; tag.fields.clear()
    err = parse_tag_assign_eof(stream, lines, err_str, tag, assign, value, null, /*simple_tag=*/true)
    if err == ERR_FILE_EOF: return OK
    if err != OK:           return err          // ERR_PARSE_ERROR / ERR_FILE_CORRUPT
    if assign != "":        set_value(section, assign, value)
    else if tag.name != "": section = tag.name.replace("\\]", "]")
```

Points:

- `ERR_FILE_EOF` is a **normal termination** (empty file → `OK`), not an error.
- **On a parse failure the already-parsed part stays in the object** (it is not transactional).
- An assignment whose `assign` is an empty string is **silently discarded**: a line such as `=5` with no key name has no effect at all.
- When `tag.name` is empty after stripping (`[]`, `[   ]`), the **current section is not changed** (note: it does not switch back to the "no section" state).
- Once the "no section" state is left, there is no going back (no syntax can express "return to the root").

---

## 3. The lexer `get_token` (`variant_parser.cpp:162-520`)

Token set: `{ } [ ] ( ) : , . =`, identifier, string, string_name, number, color, EOF, ERROR.

Character handling (in `switch` order):

| Input | Result |
| --- | --- |
| `\n` | line number +1, continue (discarded as whitespace) |
| other `c <= 32` (space/`\t`/`\r`/`\v`/`\f`…) | discarded |
| `{` `}` `[` `]` `(` `)` | the corresponding bracket token |
| `:` `,` `.` `=` | the corresponding symbol token |
| `;` | **comment**: swallowed to the end of the line (including `\n`, line number +1); if EOF occurs midway → returns `TK_EOF` |
| `#` | **color literal**: eats consecutive hex digits, then `Color::html("#...")` (not a comment!) |
| `"` | string literal (including escapes) |
| `&` (and `@` when deprecated is not disabled) | must be immediately followed by `"`, otherwise it reports `Expected '"' after '&'`; parsed as a StringName |
| digit start (optional `-` followed by a digit) | number |
| ASCII letter or `_` (optional leading `-`, see below) | identifier: `[A-Za-z_][A-Za-z0-9_]*` |
| other | reports `Unexpected character` |

Details:

- **An identifier cannot start with a digit**; `-` is eaten into the token only when "followed by a digit" or "followed by a letter/underscore":
  - `-inf` → the identifier `-inf` (a legal value);
  - `-.5` / `- 5` / `+5` → an `Unexpected character` error (**unary `+` is not supported**).
- There is **no** `0x` hexadecimal, `0b` binary, or `1_000` underscore separator (those are GDScript syntax, not this).
  `a=0x10` parses as `a=0`, and then the remaining `x10` becomes part of the next key name.
- There is only a one-character pushback buffer (`p_stream->saved`), used to push a terminator back.

---

## 4. section header

`ConfigFile` passes `p_simple_tag = true` and takes the simple branch of `_parse_tag` (`variant_parser.cpp:1638-1691`),
so it **does not tokenize identifiers and does not parse fields** (the `key=value` field syntax used by `.tscn` for `[node name="X" type="Y"]` does not apply here —
`[node name="X"]` is taken as the whole section name `node name="X"`).

Rules:

1. Trigger condition: a `[` is encountered at the **start** of some read, and no key-name character has been accumulated before it (`what.length() == 0`, `variant_parser.cpp:1801`).
   ⇒ only whitespace/comments are allowed before the `[`; the `[` in `x[y]=1` belongs to the key name.
2. Afterwards it reads **character by character** up to the first "unescaped `]`", with `\` acting as the escape flag (the escape takes effect only on `]`):
   - `]` → end;
   - `\` → sets the escape flag, and the `\` itself **is also collected into the name**;
   - any other character (including a newline, `[`, `"`, `=`) → collected into the name verbatim.
3. The name is run through `strip_edges()` (trimming leading/trailing whitespace, including Unicode whitespace).
4. The stream type decides whether one UTF-8 decoding pass is done (see §1.3).
5. `ConfigFile` then finally does `name.replace("\\]", "]")` to restore (`config_file.cpp:300`).
6. `name` is empty → this section is ignored (the previous section is kept).
7. section names are case-sensitive; the same section may appear multiple times (keys merge into the same map).

The writing side (`config_file.cpp:200`) only does `name.replace("]", "\\]")`.

**Therefore**:
- `[a\]b]` ⇔ the name `a]b` (round-trip correct).
- `[a\b]` ⇔ the name `a\b` (the backslash is preserved verbatim, round-trip correct).
- ⚠️ When a name **ends with a single backslash** (`a\`), it is written as `[a\]`, and on read-back that `]` is eaten by the escape → it keeps swallowing up to the next `]`.
  This is a defect of the reference implementation; implementers are advised to additionally escape `\` inside the name as `\\` (but then the output is no longer byte-for-byte identical to Godot's).
- ⚠️ After `]`, the same line **must not** be followed by non-comment content (see §5.4).

---

## 5. Key names and assignment

### 5.1 Key name reading (`parse_tag_assign_eof`, `variant_parser.cpp:1768-1835`)

A character-by-character loop, **not** using the lexer:

```text
what = ""
loop:
    c = next char (or pushed-back char)
    if EOF:                 return ERR_FILE_EOF            // a dangling key name is silently ignored
    if c == ';':            swallow to end of line (line+1); continue     // comment, what is kept
    if c == '[' and what=="": push back '['; parse section header; return
    if c > 32:
        if c == '"':        push back '"'; take one string token; what = token value   // overwriting assignment!
        elif c != '=':      what += c
        else:               r_assign = what; take token; parse_value; return
    elif c == '\n':         line+1                             // what is kept
    // other whitespace: discarded
```

Conclusions:

1. Key name = all characters `> 32` from the start of the call to the first `=` (with whitespace removed).
2. **Whitespace inside a key name is deleted**: `my key=1` → key name `mykey`.
3. **A newline does not break the statement**: `foo\nbar=1` → key name `foobar`.
4. **A `;` comment does not break it either**: `a;注释\nb=1` → key name `ab`.
5. Apart from `=`, `;`, and characters `≤32`, every character is legal in a key name (including `[`, `]`, `#`, `{`, `}`, `(`,`)`, `:`, `,`, `.`, `"` (not at the start), etc.).
   - An `a/b` key name such as `physics/2d/default_gravity=980` is the norm (project.godot expresses hierarchy with it).
6. **Quoted key names**: `"..."` uses the lexer's string parsing (with all escapes), and it **overwrites** `what` (it does not append):
   - `"my key"=1` → key name `my key`;
   - `x"ab"=1` → key name `ab` (`x` is discarded);
   - `"a"b=1` → key name `ab` (accumulation resumes after the quotes).
7. **`#` is not a comment**: the line `# comment` becomes part of a key name; for `#ff0000=1` the key name is `#ff0000`.
   ⇒ only `;` is a comment character (in the file `#` is meaningful only in a **value position**, where it means a color).
8. Key names are case-sensitive; for duplicate keys in a value **the later one overrides the earlier one**.
9. An assignment with no key name (`=5`) is discarded (§2).
10. A key name left dangling at the end of the file (such as `foo` on the last line) → `ERR_FILE_EOF` → **not an error, silently ignored**;
    but `foo=` on the last line (has `=` with no value) → fetching a token yields EOF → `Expected value, got 'EOF'` → `ERR_PARSE_ERROR`.

### 5.2 Separation

`key = value`, `key=value`, and `\t key=value` are all legal. After one assignment ends, the next assignment may continue on the same line:
`a=1 b=2` and `a="x" b=true` are both legal (the value itself carries its own terminator).

### 5.3 The "residual characters" rule after a value on a line (important)

Value parsing pushes back only the **terminator**, so a non-comment character immediately following the value becomes the **start of the next key name**:

```ini
a=1 remainder
b=2
```
⇒ key `a` = 1, key `remainderb` = 2 (`remainder` is glued to the `b` of the next line).

```ini
[section] extra
key=1
```
⇒ section name `section`, then key `extrakey` = 1.

⇒ practical rule: **each assignment / each section header occupies its own line, and only a `;` comment may follow on that line**.

### 5.4 Comments

- Only `;` (a line comment, up to `\n` or EOF).
- Position: the top level (inside the key-name reading loop), and **anywhere a token is fetched with the lexer** (between array, dictionary, and constructor arguments).
- `#` is **not** a comment.
- A comment at EOF ends parsing directly (returns `ERR_FILE_EOF` → `OK`).

---

## 6. The complete syntax of values

```
value      ::= dict | array | identifier-value | number | string | string-name | color
dict       ::= '{' ( pair ( ',' pair )* ','? )? '}'          // trailing comma allowed
pair       ::= value ':' value                               // the key may be any value
array      ::= '[' ( value ( ',' value )* ','? )? ']'        // trailing comma allowed
```

### 6.1 The literal kinds

| Form | Example | Result | Note |
| --- | --- | --- | --- |
| integer | `42` `-7` | int (int64) | overflow saturates as int64 and reports an error |
| float | `1.5` `-0.25` `1e3` `1.` `0.5e-2` | float (double) | a `.` or `e/E` makes it a float |
| string | `"hi\n"` | String | UTF-8; escapes in §7 |
| StringName | `&"name"` (old style `@"name"`, when deprecated is not disabled) | StringName | `&` must be immediately followed by `"` |
| color | `#ff0000` `#f00` `#ff0000ff` `#f00a` | Color | 3/4/6/8 hex digits; any other length → error and fall back to `Color()` (black) |
| boolean/null | `true` `false` `null` `nil` | bool / NIL | `nil` is the same as `null`; NIL **deletes the key** (§8.3) |
| special floats | `inf` `-inf` `inf_neg` `nan` | ±INF / NaN | `-inf` is an identifier (the `-` is eaten into the token) |
| array | `[1, 2, "x", Vector2(1,2)]` | Array | elements may be any value, may be nested |
| dictionary | `{"a": 1, 2: [3]}` | Dictionary | keys may be any value (including numbers/arrays) |
| constructor | see §6.2 | built-in types | the argument count must match exactly |
| packed array | see §6.3 | Packed*Array | |
| typed container | see §6.4 | Array[T] / Dictionary[K,V] | |
| object/resource | see §6.5 | Object/Resource/… | generally not needed in ConfigFile |

An identifier not listed → `Unexpected identifier 'xxx'` → `ERR_PARSE_ERROR`.

Type restrictions on value tokens (the closing branch of `parse_value`): only `{ [ identifier number string string_name color` is accepted;
encountering `( ) : , . = } ]` or EOF reports `Expected value, got '...'`.

### 6.2 Built-in constructors (`real_t` arguments = floats, `int32_t`/`int64_t` = integers)

| Name | Argument count | Aliases (backward compatible) |
| --- | --- | --- |
| `Vector2` | 2 | |
| `Vector2i` | 2 | |
| `Rect2` | 4 | |
| `Rect2i` | 4 | |
| `Vector3` | 3 | |
| `Vector3i` | 3 | |
| `Vector4` | 4 | |
| `Vector4i` | 4 | |
| `Transform2D` | 6 | `Matrix32` |
| `Plane` | 4 | |
| `Quaternion` | 4 | `Quat` |
| `AABB` | 6 | `Rect3` |
| `Basis` | 9 | `Matrix3` |
| `Transform3D` | 12 | `Transform` |
| `Projection` | 16 | |
| `Color` | 4 | |
| `NodePath` | 1 **string** | |
| `RID` | 0 (`RID()`) or 1 number | |
| `Signal` | 0 (`Signal()`) | deserializes to empty |
| `Callable` | 0 (`Callable()`) | deserializes to empty |
| `Object` | see §6.5 | |

Constructor argument rules (`_parse_construct`, `variant_parser.cpp:552-599`):

- syntax `Name( arg (, arg)* )`; arguments must be number tokens or the identifiers `inf` / `-inf` / `inf_neg` / `nan`;
- **an empty argument list is not allowed** (except for the dedicated branches `RID()` / `Signal()` / `Callable()`);
- **a trailing comma is not allowed** (`Vector2(1, 2,)` → `Expected float in constructor`);
- an argument count mismatch → `Expected N arguments for constructor`;
- arguments may be integer tokens (they are converted to floats).

### 6.3 Packed arrays (flat argument lists)

| Name | Argument type | Old alias |
| --- | --- | --- |
| `PackedByteArray` | `("base64")` **or** a list of numbers | `PoolByteArray`, `ByteArray` |
| `PackedInt32Array` | list of integers | `PackedIntArray`, `PoolIntArray`, `IntArray` |
| `PackedInt64Array` | list of integers | |
| `PackedFloat32Array` | list of floats | `PackedRealArray`, `PoolRealArray`, `FloatArray` |
| `PackedFloat64Array` | list of floats | |
| `PackedStringArray` | list of strings | `PoolStringArray`, `StringArray` |
| `PackedVector2Array` | list of floats (groups of 2) | `PoolVector2Array`, `Vector2Array` |
| `PackedVector3Array` | list of floats (groups of 3) | `PoolVector3Array`, `Vector3Array` |
| `PackedVector4Array` | list of floats (groups of 4) | `PoolVector4Array`, `Vector4Array` |
| `PackedColorArray` | list of floats (groups of 4) | `PoolColorArray`, `ColorArray` |

- The empty `PackedInt32Array()` is allowed; a **trailing comma** is allowed only in `PackedStringArray`,
  whereas a trailing comma in `PackedByteArray` and in the `_parse_construct` family (int/float/vector/color grouping) reports
  `Expected number/float in constructor` (because their loops, after eating a comma, require an element to follow immediately).
- For the grouping kinds (Vector2/3/4, Color), **extra trailing elements are silently discarded** (count ÷ N rounded down), and **whether the count is a multiple of N is not checked**.
- The grouping form of `PackedByteArray` also accepts `inf/nan` (it shares `stor_fix`); values are truncated as uint8.
- A wrong element type (such as `PackedStringArray(1)`) → `Expected string`.

### 6.4 Typed containers

```
typed-array ::= 'Array[' type-ident ']' '(' array ')'
typed-dict  ::= 'Dictionary[' type-ident ',' type-ident ']' '(' dict ')'
```

- `type-ident` is a **variant type name**: `bool int float String Vector2 Vector2i Rect2 Rect2i Transform2D Vector3 Vector3i
  Vector4 Vector4i Plane AABB Quaternion Basis Transform3D Projection Color RID Object Callable Signal StringName
  NodePath Dictionary Array PackedByteArray PackedInt32Array PackedInt64Array PackedFloat32Array PackedFloat64Array
  PackedStringArray PackedVector2Array PackedVector3Array PackedColorArray PackedVector4Array`
  (note: the name `Nil` of `Variant::NIL` is **not** in the list; the type enumeration starts at index 1, `variant_parser.cpp:1132`)
- It may also be `Resource` / `SubResource` / `ExtResource` (in which case it parses into a resource reference, using the script's base class name as the class name),
  or any `ClassDB` class name (such as `Node`).
- **An unknown identifier does not report an error**: `Array[Whatever]([])` is treated as an **untyped** array (`set_typed` is not called).
- In `Dictionary[K, V]` both the key and the value type identifiers are required.

### 6.5 Objects and resources

- `Object(ClassName, "prop": value, "prop2": value)`: instantiates from ClassDB and `set()`s the properties;
  keys must be strings; the `}`/`,` semantics are the same as for dictionaries (trailing comma allowed); the writing side serializes only
  properties with `PROPERTY_USAGE_STORAGE` or `SCRIPT_VARIABLE`, and the indented form is in `tests/core/io/test_config_file.cpp:177-200`.
- `Resource("res://path.tres")`, `Resource("uid://xxx", "res://path.tres")`.
- ⚠️ The `_parse` of ConfigFile **does not pass a `ResourceParser`** (`config_file.cpp:289`, the 7th argument is `nullptr`),
  so the three identifiers `Resource/SubResource/ExtResource` all take the same branch: **treating the argument as a path and calling `ResourceLoader::load()`**.
  In ConfigFile, `ExtResource("1_abc")` makes the whole parse fail because the file cannot be found.
  If a third-party implementation does not intend to load resources, it is advisable to keep `Resource(...)` as an "opaque node" and declare it unsupported at load time (or allow turning it off).

---

## 7. Strings and escapes (`variant_parser.cpp:277-415`)

- The delimiter is `"`; **a newline does not terminate**, a bare newline may appear inside a string (the line number still increments).
- The `\` escape table:

| Escape | Result |
| --- | --- |
| `\b` `\t` `\n` `\f` `\r` | 0x08 / 0x09 / 0x0A / 0x0C / 0x0D |
| `\uXXXX` | a 4-digit hexadecimal code point (UTF-16 surrogate pair joining is supported) |
| `\UXXXXXX` | a 6-digit hexadecimal code point |
| `\<other>` | that character itself (`\"`→`"`, `\\`→`\`, `\'`→`'`, `\a`→`a`, `\v`→`v`, `\p`→`p`) |

- Errors: EOF → `Unterminated string`; a non-hex digit in `\u` → `Malformed hex constant in string`;
  a lone surrogate → `Invalid UTF-16 sequence in string, unpaired lead/trail surrogate`.
- ⚠️ **Windows path trap**: the `\t` in `"C:\temp"` is a tab and `\p` becomes `p`; a path must be written `"C:\\temp"`.
- ⚠️ **`\u` is "broken" on the `load()` path** (code-derived, not measured): after the string is read, an `is_utf8()` stream goes through
  `str.ascii(true)` → `append_utf8`, so the **code point produced by the escape is treated as raw bytes and UTF-8-decoded one more time**:
  - `\u0041` (ASCII) is fine;
  - `\u00e9` → byte `0xE9` → invalid UTF-8 → the **U+FFFD replacement character**;
  - `\u4e2d` → `ascii()` reports "Invalid unicode codepoint" → becomes a **space**.
  The same text is nevertheless correct via `parse(String)` (`is_utf8() == false`, this re-decoding is skipped).
  ⇒ for cross-implementation interoperability, characters containing non-ASCII should be written as **raw UTF-8 bytes**, not `\u`/`\U`.
- The writing side (`VariantWriter`) uses two escape functions:
  - String values → `c_escape_multiline()`: **escapes only `\` and `"`**, a bare newline is preserved verbatim (multi-line strings);
  - StringName / NodePath / PackedStringArray elements → `c_escape()`: additionally escapes `\a \b \f \n \r \t \v \'`.
  - ⚠️ Asymmetric: `\a` reads back as `a`, `\v` as `v`, `\'` as `'`. So a StringName containing 0x07/0x0B loses characters on a
    round-trip (a small bug in Godot itself).

---

## 8. Numbers

```
number ::= '-'? digits ( '.' digits* )? ( [eE] [+-]? digits* )?
```

- Decision: a `.` or `e/E` in the token → go through `double` (the built-in `strtod`); otherwise int64.
- Truncated shapes such as `1.`, `1e`, `1e+` are allowed (`strtod` extracts as much as it can); implementers may handle them as "lenient" or as "error".
- Integer overflow: saturates to `INT64_MAX` / `INT64_MIN` and outputs an error.
- `0x`, `0b`, `_` separators, and a leading `+` are **not supported**.
- Writing (`rtos_fix` + `String::num_scientific`, i.e. grisu2 shortest round-trip, `%g` style):
  - fixed-point range: roughly `1e-4 <= |v| < 1e15`, otherwise exponential form with an exponent of **at least two digits** (`1e+16`, `1e-05`);
  - `-0.0` → `0`; `inf` → `inf`; `nan` → `nan`;
  - with `p_compat = true` (which is what ConfigFile uses) `-inf` → `inf_neg`;
  - **suspected bug**: after `-inf` a `.0` is still appended (because the test excludes only `inf`/`-inf`/`nan` and `inf_neg` is not in that list)
    → in practice `inf_neg.0` is written and **a round-trip is impossible**. Whether to copy this behavior is up to implementers (writing `-inf` is recommended).
  - After a float is written, if it contains no `.`/`e`/`E`, `.0` is appended to guarantee that reading it back yields a float (the float `1` is written `1.0`, `1e14` is written `100000000000000.0`).
  - When `(double)(float)v == v`, it is output at float32 precision (to avoid a 32-bit float writing garbage into the mantissa).
- Writing integers: decimal `itos`.
- **int and float are two different variant types**; `1` and `1.0` are not equivalent (the type read back differs).

---

## 9. Writing (serialization) rules

`ConfigFile::save()` and `encode_to_text()` have identical logic with exactly one difference: `save()` escapes `]` in a section name as `\]`,
while `encode_to_text()` **does not escape** (`config_file.cpp:133` vs `:200`).

```text
for i, (section, keys) in sections:          // insertion order
    if i > 0: write "\n"
    if section != "": write "[" + section.replace("]", "\\]") + "]\n\n"
    for (key, value) in keys:                // insertion order
        write key.property_name_encode() + "=" + VariantWriter.write(value, pretty=true, compat=true) + "\n"
```

- One blank line between sections; one blank line after a section header; one line per key (a value may span several lines); the file ends with `\n`.
- Keys of the "no section" part go first (no section header is written):
  `values.insert(section, ..., /*p_front=*/section.is_empty())` (`config_file.cpp:51`),
  Godot's `HashMap` is **insertion-ordered** (it has a linked list, `core/templates/hash_map.h:232-243`).
- Key names `property_name_encode()` (`ustring.cpp:5067`): characters containing `=`, `"`, `;`, `[`, `]`,
  or `< 33`, `> 126` → quoted as a whole and passed through `c_escape_multiline()`; otherwise verbatim.
  (`a=b` → `"a=b"`, `静音` → `"静音"`, consistent with the official test.)
- Value writing forms:
  - array: `[a, b, c]` on one line (`, ` between elements), empty `[]`;
  - dictionary: multi-line
    ```
    {
    "k": v,
    "k2": v2
    }
    ```
    empty `{}`; **keys are first sorted in "string-kind" order** (same-type String/StringName by code-point lexicographic order, `str_compare`; mixed types fall back to `Variant::operator<`, `variant_parser.cpp:2181`);
  - `Object(...)` is multi-line (`p_pretty_print` affects only this one place: a space prefix on property lines, `,\n` at line ends);
  - packed arrays are one line, separated by `", "`; with `p_compat=true` a `PackedByteArray` is written as a decimal number list, otherwise as a base64 string;
  - when the recursion depth > `MAX_RECURSION` (100, `core/typedefs.h:260`), an array is written `[]` and a dictionary `{}` (and an error is reported).

---

## 10. ConfigFile data model semantics (`config_file.cpp`)

- Structure: `section -> { key -> Variant }`, two levels of `HashMap`, **preserving insertion order**.
- `set_value(section, key, NIL)` = **delete** that key; if the section becomes empty as a result, **the section is deleted along with it** (`config_file.cpp:38-56`).
  - Because parsing also goes through this path for `null`/`nil` (the `set_value` of `NIL`), **a `null` value cannot be stored in a ConfigFile**:
    `k=null` equals deleting `k`; if that was the section's only key, **the whole section disappears**.
  - If the target section does not exist, `set_value(..., NIL)` does nothing.
- Duplicate section headers: merged into the same section (not newly created, not reset).
- Duplicate keys: the later write overrides the earlier one.
- Both section names and key names are **case-sensitive** (the official test specifically verifies `antialiasing` ≠ `antiAliasing`).
- `get_value(section, key)`: missing and no default given → prints an error and returns NIL (`config_file.cpp:58-66`);
  `erase_section*` on a nonexistent target prints an error and has no effect; `get_section_keys` on a nonexistent section prints an error and returns an empty array.
- Parsing does not clear existing content (`parse`/`load` "merge into" rather than "replace").
- Encrypted variant: **the same text format**, AES-256 encrypted (`FileAccessEncrypted`), with no difference at the syntax level.

---

## 11. Error codes and messages (for aligning behavior)

| Scenario | Return | Message (`r_err_str`) |
| --- | --- | --- |
| normal / empty file / dangling key name on the last line | `OK` | — |
| unknown identifier value | `ERR_PARSE_ERROR` | `Unexpected identifier 'x'` |
| an illegal token in a value position | `ERR_PARSE_ERROR` | `Expected value, got '<tk>'` |
| illegal character | `ERR_PARSE_ERROR` | `Unexpected character` |
| unterminated string | `ERR_PARSE_ERROR` | `Unterminated string` |
| constructor argument count/type | `ERR_PARSE_ERROR` | `Expected N arguments for constructor` / `Expected float in constructor` |
| array/dictionary/object hits EOF | `ERR_FILE_CORRUPT` | `Unexpected EOF while parsing array/dictionary/Object()` |
| array missing a comma | `ERR_PARSE_ERROR` | `Expected ','` |
| dictionary missing a colon | `ERR_PARSE_ERROR` | `Expected ':'` |
| section header unterminated | `ERR_PARSE_ERROR` | `Unexpected EOF while parsing simple tag` |
| no `"` after `&` | `ERR_PARSE_ERROR` | `Expected '"' after '&'` |

The error context is printed by ConfigFile: `ConfigFile parse error at <path>:<lines>: <err_str>.` (`config_file.cpp:293`).
Note that `lines` increments only when "skipping whitespace/newlines/comments/newlines inside a string"; **a newline inside a section header is not counted** (the simple tag branch does not touch `r_line`).

⚠️ The parser has **no nesting depth limit** (array/dictionary recursion is not constrained by `MAX_RECURSION`), and a malicious deeply nested input causes a stack overflow ——
when reimplementing, it is advisable to add a limit of your own (the limit on Godot's writing side is 100).

---

## 12. Compatibility trap list (the things to watch most when reimplementing)

1. **Only `;` is a comment**, `#` is not; `#` in a value position is a color.
2. **A newline is not a terminator**: key names glue across lines, across `;` comments, and across residual characters after a section header. The key name of `a=1 x\nb=2` is `xb`.
3. **A key name eats whitespace**: the key of `my key=1` is `mykey`.
4. **A non-ASCII key name must be quoted**, otherwise `load()` yields mojibake (key names are not UTF-8 decoded; section names and strings are).
5. **`null` is not a value but a deletion operation**, and it may delete a whole section.
6. **`\u`/`\U` code points above 0x7F are broken on the `load()` path**; use only UTF-8 literals or real escapes (such as `\n`).
7. **Escapes such as `\t` eat Windows paths**, so `\\` is required.
8. **Trailing comma**: allowed in arrays, dictionaries, `Object(...)`, and `PackedStringArray`;
   not allowed in the built-in constructors and the other packed arrays (the `_parse_construct` / `PackedByteArray` paths).
9. **A misspelled type identifier does not report an error** (`Array[Foo]` silently becomes an untyped array).
10. **Extra elements of a grouping packed array are silently truncated**.
11. **`-inf` is written as `inf_neg.0`** (no round-trip possible), while `inf_neg` itself is an accepted input.
12. **`PackedByteArray` has two input forms** (a number list / a base64 string), but the default output is a number list.
13. **BOM** is not handled; `0x`/`0b`/`1_000` are not numbers.
14. A section name ending with `\` breaks the round-trip (see §4).
15. `encode_to_text()` and `save()` differ in their handling of `]` in a section name.

---

## 13. Reimplementation checklist

**Suggested module split**

1. **Byte layer**: read the byte stream; decide between "decode UTF-8 to code points up front as a whole" and "replicate Godot's byte view".
   If the goal is 100% replication of the quirks → replicate the byte view; if the goal is "looks the same and is more correct" → decode UTF-8 once and accept the differences of §12.4/§12.6.
2. **Lexer**: implement §3 (including the one-character pushback, `;` comments, `#` colors, `&"`, and the number state machine).
3. **Top-level reader**: implement the `parse_tag_assign_eof` equivalent of §5.1 + the simple tag reading of §4.
4. **Value parser**: dispatch per §6; validate the constructor argument count against the table (error messages may align with §11).
5. **Data model**: an ordered map (both sections and keys must preserve order) + `NIL = delete` semantics.
6. **Writer**: per §9 (and along the way decide whether to copy `inf_neg.0`).
7. **Numerics**: int64 + IEEE754 double; parsing via a strtod equivalent;
   writing via shortest-round-trip (Ryu/Grisu/`printf %.17g` then trimming), an exponent of at least two digits, the fixed-point range `1e-4 ~ 1e15`, and a `.0` suffix.
8. **Safety**: add a depth limit for nesting; add a length limit for the input (Godot itself has neither).

**Cases that must pass (from the official implementation)**

```ini
; 混合格式，官方测试原样，必须全部解析成功
[player]

name = "Unnamed Player"
tagline="Waiting
for
Godot"

color =Color(   0, 0.5,1, 1) ; Inline comment
position= Vector2(
	3,
	4
)

[graphics]

antialiasing = true

; Testing comments and case-sensitivity...
antiAliasing = false
```
→ `player/name == "Unnamed Player"`, `player/tagline == "Waiting\nfor\nGodot"`,
`player/color == Color(0,0.5,1,1)`, `player/position == Vector2(3,4)`,
`graphics/antialiasing == true`, `graphics/antiAliasing == false`.

Round-trip (the exact expectation of `save`, official test `test_config_file.cpp:141-159`):

```ini
[player]

name="Unnamed Player"
tagline="Waiting
for
Godot"
color=Color(0, 0.5, 1, 1)
position=Vector2(3, 4)

[graphics]

antialiasing=true
antiAliasing=false

[quoted]

"静音"=42
"a=b"=7
```

Other boundaries that must be tested: empty file, `null` deleting a key and a section, duplicate keys/duplicate sections, `a=1 b=2`, `foo\nbar=1` gluing,
`#` not being a comment, `[]` not changing the section, trailing commas (allowed in arrays/dictionaries/`PackedStringArray`, not allowed in constructors), `PackedByteArray("AQID")`,
`Array[int]([1, 2])`, `Dictionary[String, int]({"a": 1})`, `&"name"`, `#f00`,
the type distinction between `1.0` and `1`, `-inf`/`nan`.

---

## 14. Reference: a real `project.godot` fragment

```ini
; Engine configuration file.
; It's best edited using the editor UI and not directly,
; since the parameters that go here are not all obvious.
;
; Format:
;   [section] ; section goes between []
;   param=value ; assign values to parameters

config_version=5

[application]

config/name="CompatibilityTest"
run/flush_stdout_on_print=true

[input]

ui_accept={
"deadzone": 0.5,
"events": [Object(InputEventKey,
"resource_local_to_scene": false,
"resource_name": "",
"device": 16,
"window_id": 0,
"alt_pressed": false,
"shift_pressed": false,
"ctrl_pressed": false,
"meta_pressed": false,
"pressed": false,
"keycode": 32,
"physical_keycode": 0,
"key_label": 0,
"unicode": 0,
"location": 0,
"echo": false,
"script": null
)]
}
```

(Note the `)]` after `Object(...)`, property lines separated by `,\n`, no comma on the last line, and the top-level `config_version=5` with no section.)
