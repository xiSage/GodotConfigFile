using System.Globalization;

namespace GodotConfigFile;

/// <summary>
/// Turns the lexer's number text into values, reproducing Godot's tolerance for malformed numbers.
/// </summary>
/// <remarks>
/// Godot parses floats with its own <c>built_in_strtod</c>, which accepts <c>1e</c>, <c>1e+</c> and <c>1.</c> and
/// clamps the exponent to 511. The clamp is not reproduced because the arithmetic result is the same: exponents
/// that large already overflow to infinity (or underflow to zero) in IEEE-754, which is exactly what
/// <see cref="double.Parse(string)"/> returns. Integers saturate at the 64-bit bounds; Godot's own check misses the
/// 19-digit case and yields <see cref="long.MinValue"/> there, which this library fixes (deviation D6).
/// </remarks>
internal static class NumberParser
{
    /// <summary>Parses an integer literal, saturating and warning on overflow.</summary>
    /// <param name="text">The token text, optionally starting with <c>-</c>.</param>
    /// <param name="diagnostics">Where to report overflow.</param>
    /// <param name="span">Where the literal is.</param>
    /// <returns>The parsed value.</returns>
    public static long ParseInt(string text, DiagnosticBag diagnostics, SourceSpan span)
    {
        bool negative = text.StartsWith('-');
        ReadOnlySpan<char> digits = negative ? text.AsSpan(1) : text.AsSpan();

        // 9223372036854775808 is the magnitude of long.MinValue, so it is reachable only when negative.
        ulong limit = negative ? 9223372036854775808UL : long.MaxValue;
        ulong accumulator = 0;
        bool overflow = false;

        foreach (char c in digits)
        {
            ulong digit = (ulong)(c - '0');
            if (accumulator > (limit - digit) / 10)
            {
                overflow = true;
                break;
            }

            accumulator = (accumulator * 10) + digit;
        }

        if (overflow)
        {
            diagnostics.Warning(DiagnosticCode.IntegerOverflow, "Integer overflow", span);
            return negative ? long.MinValue : long.MaxValue;
        }

        if (!negative)
        {
            return (long)accumulator;
        }

        // The magnitude of long.MinValue is not representable as a positive long, so it needs its own branch.
        return accumulator == 9223372036854775808UL ? long.MinValue : -(long)accumulator;
    }

    /// <summary>Parses a float literal, tolerating Godot's incomplete exponent forms.</summary>
    /// <param name="text">The token text, optionally starting with <c>-</c>.</param>
    /// <returns>The parsed value.</returns>
    public static double ParseFloat(string text)
    {
        string normalized = text;

        // "1e" and "1e+" mean 1.0 to Godot: the exponent introduces nothing, so the sign is dropped with it.
        if (normalized.EndsWith('e') || normalized.EndsWith('E'))
        {
            normalized = normalized[..^1];
        }
        else if (normalized.Length > 1 &&
            (normalized.EndsWith('+') || normalized.EndsWith('-')) &&
            (normalized[^2] == 'e' || normalized[^2] == 'E'))
        {
            normalized = normalized[..^2];
        }

        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
        {
            return value;
        }

        // "1." is the only shape the lexer can still produce here.
        if (normalized.EndsWith('.') &&
            double.TryParse(normalized[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return value;
        }

        throw new InvalidOperationException($"The lexer produced an unparsable number token: '{text}'.");
    }
}
