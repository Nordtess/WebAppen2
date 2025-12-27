using System.Globalization;

namespace WebApp.Domain.Helpers;

/// <summary>
/// Hjälpmetoder för formatering och normalisering av namnsträngar.
/// </summary>
public static class NameNormalizer
{
    // Svensk kultur används för att TitleCase hanterar å/ä/ö korrekt.
    private static readonly CultureInfo SwedishCulture = CultureInfo.GetCultureInfo("sv-SE");

    public static string ToDisplayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // Trimma och bygg upp resultatet tecken för tecken så att mellanslag och bindestreck bevaras.
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

            // Normalisera ord: till små bokstäver och sedan TitleCase med svensk kultur.
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

        // Slå ihop och komprimera eventuella dubbla mellanslag.
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

        // Normaliseringsform för jämförelser/sökning: trim + versaler (kulturinvariant).
        return value.Trim().ToUpperInvariant();
    }
}
