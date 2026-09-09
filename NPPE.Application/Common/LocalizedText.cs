using System.Globalization;

namespace NPPE.Application.Common;

/// <summary>
/// Picks the French text for DB-stored content (exam questions/options/explanations)
/// when the current request culture is French and a translation exists; otherwise
/// falls back to the English original.
/// </summary>
public static class LocalizedText
{
    public static string Pick(string english, string? french) =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("fr", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(french)
            ? french!
            : english;
}
