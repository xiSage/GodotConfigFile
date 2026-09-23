# AGENTS.md

Working rules for agents in this repository. **Read this file before changing code.**

## Pre-commit formatting gate (mandatory)

Before committing any C# change, run, in order:

1. `dotnet format --severity info`
2. `dotnet format --severity info --verify-no-changes`
3. If step 2 reports anything, **fix it by hand** according to its output, then repeat steps 1 and 2 until `--verify-no-changes` is clean.

Run these from the repository root; the workspace (`GodotConfigFile.slnx`) resolves correctly there. As of the last check the current code is already clean at info severity.

## Build and test

From the repository root:

```bash
dotnet build
dotnet test
```

## Language

Every committed artifact is written in **English**: documentation, source comments, XML doc comments, test method names, commit messages, and CI descriptions.

Two exceptions:

- **Test data and corpus fixtures keep their original bytes**, including any non-ASCII characters. That covers the
  corpus files themselves and the non-ASCII literals a test writes into its input to exercise them (the official
  `静音` key, for example). They are the content under test, not documentation.
- Conversation with the maintainer stays in Chinese; conversation is not a committed artifact.

Existing commit history is left as-is; history is not rewritten to satisfy this rule.

## Additional rules during the v1.0.0 rewrite

- The library must build with **zero warnings** (`TreatWarningsAsErrors`), target `net10.0`, have no third-party runtime dependencies, and stay trim / AOT-clean (no reflection).
- **Parsing only, no serialization**: do not add write-out APIs such as `Save` / `EncodeToText`.
- **Format rules replicate Godot verbatim.** Fixing a Godot bug is a *deliberate deviation*: it must be recorded in `docs/compat-deviations.md`, with at least one test per deviation.
- Tests use the ported official `tests/core/io/test_config_file.cpp` suite and the checklist in `docs/godot-configfile-spec.md` §13 as the oracle. `docs/godot-configfile-spec.md` is the factual baseline: its **facts must not be changed**. It was translated from Chinese to English once, for the language rule above, and that is the only edit it gets.
