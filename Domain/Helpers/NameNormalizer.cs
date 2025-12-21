using System.Globalization;

namespace WebApp.Domain.Helpers;

/// <summary>
/// Hjälpmetoder för att formatera och normalisera namnsträngar på ett konsekvent sätt.
/// </summary>
public static class NameNormalizer
{
    private static readonly CultureInfo SwedishCulture = CultureInfo.GetCultureInfo("sv-SE");

    public static string ToDisplayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // Trimma och bygg sedan upp strängen tecken för tecken för att behålla separatorer (mellanslag och bindestreck).
        value = value.Trim();

        var parts = new List<string>();
        var currentWord = new List<char>();

        void FlushWord()
        {
            if (currentWord.Count == 0)
            {
                return;
            }

            var word = new string(currentWord.ToArray());
            currentWord.Clear();

            // TitleCase med svensk kultur (påverkar t.ex. å/ä/ö).
            word = word.ToLower(SwedishCulture);
            word = SwedishCulture.TextInfo.ToTitleCase(word);

            parts.Add(word);
        }

        foreach (var ch in value)
        {
            if (ch is '-' or ' ')
            {
                FlushWord();
                parts.Add(ch.ToString());
                continue;
            }

            currentWord.Add(ch);
        }

        FlushWord();

        // Slå ihop resultatet och komprimera eventuella dubbla mellanslag.
        var joined = string.Concat(parts);
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

        // Normaliserad form används för jämförelser/sökning: trim + versaler med invariant kultur.
        return value.Trim().ToUpperInvariant();
    }
}
