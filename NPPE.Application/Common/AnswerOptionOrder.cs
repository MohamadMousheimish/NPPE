namespace NPPE.Application.Common;

/// <summary>
/// Keeps catch-all answer options ("All of the above", "None of the above",
/// "None of these", …) at the bottom of the displayed option list, where they
/// belong. Display-only: grading is by option Id + IsCorrect, never by position,
/// so reordering the display can never change which answer is correct.
/// </summary>
public static class AnswerOptionOrder
{
    /// <summary>0 for a normal option, 1 for a meta / catch-all option (sorts last).</summary>
    public static int MetaRank(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        // Normalise: trim surrounding whitespace and trailing punctuation, lowercase.
        var t = text.Trim().TrimEnd('.', '!', ' ').ToLowerInvariant();

        if (t.StartsWith("all of the above", StringComparison.Ordinal)
            || t.StartsWith("all the above", StringComparison.Ordinal)
            || t.StartsWith("none of the above", StringComparison.Ordinal)
            || t.StartsWith("none of these", StringComparison.Ordinal))
            return 1;

        return 0;
    }
}
