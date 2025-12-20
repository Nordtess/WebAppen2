using System.Globalization;

namespace WebApp.Domain.Helpers;

public static class NameNormalizer
{
    // Splits on space and hyphen and applies Title Case per segment.
    // Keeps separators as-is (single space or '-').
    public static string ToDisplayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // Normalize whitespace
        value = value.Trim();

        // Build by iterating chars to preserve separators
        var result = new List<string>();
        var current = new List<char>();

        void flushWord()
        {
            if (current.Count == 0) return;
            var word = new string(current.ToArray());
            current.Clear();

            // TitleCase in Swedish culture
            var culture = CultureInfo.GetCultureInfo("sv-SE");
            word = word.ToLower(culture);
            word = culture.TextInfo.ToTitleCase(word);
            result.Add(word);
        }

        foreach (var ch in value)
        {
            if (ch is '-' or ' ')
            {
                flushWord();
                result.Add(ch.ToString());
            }
            else
            {
                current.Add(ch);
            }
        }

        flushWord();

        // Collapse multiple spaces (optional)
        var joined = string.Concat(result);
        while (joined.Contains("  ", StringComparison.Ordinal))
        {
            joined = joined.Replace("  ", " ", StringComparison.Ordinal);
        }

        return joined;
    }

    public static string ToNormalized(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().ToUpperInvariant();
    }
}
