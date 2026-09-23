namespace GodotConfigFileTests;

/// <summary>Shared helpers for the test suite.</summary>
internal static class Fixture
{
    /// <summary>Resolves a file inside the corpus copied next to the test assembly.</summary>
    public static string CorpusPath(string relative) => Path.Combine(AppContext.BaseDirectory, "corpus", relative);

    /// <summary>Parses a corpus file.</summary>
    public static ConfigFileDocument ParseCorpus(string relative) => ConfigFile.Load(CorpusPath(relative));

    /// <summary>Reads a value that the test expects to exist.</summary>
    public static Variant Require(ConfigFileDocument document, string section, string key)
    {
        if (!document.TryGetVariant(section, key, out Variant value))
        {
            throw new InvalidOperationException($"Expected {section}/{key} to be present.");
        }

        return value;
    }

    /// <summary>Runs the lexer over the whole input and returns the tokens, excluding the final EOF.</summary>
    public static List<Token> Lex(string text, out IReadOnlyList<Diagnostic> diagnostics)
    {
        DiagnosticBag bag = new();
        Lexer lexer = new(text, bag);
        List<Token> tokens = [];

        while (true)
        {
            Token token = lexer.GetToken();
            if (token.Type == TokenType.Eof)
            {
                break;
            }

            tokens.Add(token);
        }

        diagnostics = bag.Items;
        return tokens;
    }

    /// <summary>Lexes input that is expected to tokenize without diagnostics.</summary>
    public static List<Token> Lex(string text) => Lex(text, out _);
}
