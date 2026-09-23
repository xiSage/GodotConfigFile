namespace GodotConfigFile;

/// <summary>
/// The <c>#rgb</c> / <c>#rgba</c> / <c>#rrggbb</c> / <c>#rrggbbaa</c> colour literal, following <c>Color::html</c>.
/// </summary>
internal static class ColorHtml
{
    /// <summary>The colour Godot returns for a literal it cannot read.</summary>
    public static readonly Color OpaqueBlack = new(0f, 0f, 0f, 1f);

    /// <summary>Parses a colour literal.</summary>
    /// <param name="text">The literal, including the leading <c>#</c>.</param>
    /// <param name="color">The parsed colour, or <see cref="OpaqueBlack"/> when the length is not one of the four accepted forms.</param>
    /// <returns><see langword="true"/> when the literal was well formed.</returns>
    public static bool TryParse(string text, out Color color)
    {
        // The lexer has already guaranteed that everything after '#' is a hexadecimal digit.
        ReadOnlySpan<char> digits = text.AsSpan(1);
        switch (digits.Length)
        {
            case 3:
                color = new Color(
                    (float)(HexValue(digits[0]) / 15.0),
                    (float)(HexValue(digits[1]) / 15.0),
                    (float)(HexValue(digits[2]) / 15.0),
                    1f);
                return true;
            case 4:
                color = new Color(
                    (float)(HexValue(digits[0]) / 15.0),
                    (float)(HexValue(digits[1]) / 15.0),
                    (float)(HexValue(digits[2]) / 15.0),
                    (float)(HexValue(digits[3]) / 15.0));
                return true;
            case 6:
                color = new Color(
                    (float)(((HexValue(digits[0]) << 4) | HexValue(digits[1])) / 255.0),
                    (float)(((HexValue(digits[2]) << 4) | HexValue(digits[3])) / 255.0),
                    (float)(((HexValue(digits[4]) << 4) | HexValue(digits[5])) / 255.0),
                    1f);
                return true;
            case 8:
                color = new Color(
                    (float)(((HexValue(digits[0]) << 4) | HexValue(digits[1])) / 255.0),
                    (float)(((HexValue(digits[2]) << 4) | HexValue(digits[3])) / 255.0),
                    (float)(((HexValue(digits[4]) << 4) | HexValue(digits[5])) / 255.0),
                    (float)(((HexValue(digits[6]) << 4) | HexValue(digits[7])) / 255.0));
                return true;
            default:
                color = OpaqueBlack;
                return false;
        }
    }

    private static int HexValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        _ => c - 'A' + 10,
    };
}
