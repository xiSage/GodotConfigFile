using System.Text;

namespace GodotConfigFileTests;

/// <summary>
/// The parser must never throw anything but the documented exception, whatever it is fed.
/// </summary>
public class FuzzTests
{
    private const string Alphabet = "[]{}():,=;\"&#\n\t0123456789abcdxyzABC-+._ \uFEFF";

    [Fact]
    public void RandomTextNeverEscapes()
    {
        Random random = new(20240101);

        for (int iteration = 0; iteration < 3000; iteration++)
        {
            ParseResult result = ConfigFile.ParseWithResult(RandomText(random));

            Assert.NotNull(result.Document);
            Assert.False(
                result.Success && result.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error),
                "A successful parse cannot carry an error diagnostic.");
        }
    }

    [Fact]
    public void RandomBytesNeverEscape()
    {
        Random random = new(20240202);
        byte[] bytes = new byte[96];

        for (int iteration = 0; iteration < 500; iteration++)
        {
            random.NextBytes(bytes);
            Assert.NotNull(ConfigFile.ParseUtf8WithResult(bytes).Document);
        }
    }

    [Fact]
    public void ExpectingAnExceptionInsteadOfAResultAlsoHolds()
    {
        Random random = new(20240303);

        for (int iteration = 0; iteration < 500; iteration++)
        {
            string text = RandomText(random);
            try
            {
                ConfigFileDocument document = ConfigFile.Parse(text);
                Assert.NotNull(document);
            }
            catch (ConfigFileParseException)
            {
                // The documented failure mode.
            }
        }
    }

    [Fact]
    public void DeeplyNestedInputDoesNotOverflowTheStack()
    {
        string text = "k=" + new string('[', 5000) + new string(']', 5000) + "\n";

        ParseResult result = ConfigFile.ParseWithResult(text);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == DiagnosticCode.DepthLimitExceeded);
    }

    [Fact]
    public void LongInputIsHandled()
    {
        StringBuilder builder = new();
        for (int i = 0; i < 5000; i++)
        {
            builder.Append("[section").Append(i).Append("]\nkey=").Append(i).Append('\n');
        }

        ConfigFileDocument document = ConfigFile.Parse(builder.ToString());

        Assert.Equal(5000, document.SectionNames.Count);
        Assert.Equal(new Variant.Int(4999), Fixture.Require(document, "section4999", "key"));
    }

    private static string RandomText(Random random)
    {
        int length = random.Next(0, 40);
        StringBuilder builder = new(length);
        for (int i = 0; i < length; i++)
        {
            builder.Append(Alphabet[random.Next(Alphabet.Length)]);
        }

        return builder.ToString();
    }
}
