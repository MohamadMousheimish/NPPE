using NPPE.Domain.Entities;

namespace NPPE.Application.Common;

/// <summary>
/// Keeps catch-all answer options ("All of the above", "None of the above",
/// "None of these", …) at the bottom of the displayed option list, where they
/// belong, and hands out clean sequential display letters (A, B, C, D) in that
/// order. Display-only: grading is by option Id + IsCorrect, never by position,
/// and the stored <see cref="AnswerOption.Label"/> is left untouched (French
/// translations are matched to options by that stored label), so nothing here
/// can change which answer is correct or misalign a translation.
/// </summary>
public static class AnswerOptionOrder
{
    /// <summary>Options in display order: normal options first (by stored label), catch-alls last.</summary>
    public static List<AnswerOption> ForDisplay(IEnumerable<AnswerOption> options) =>
        options.OrderBy(o => MetaRank(o.Text)).ThenBy(o => o.Label).ToList();

    /// <summary>
    /// The sequential display letter (A, B, C, …) an option gets after display ordering,
    /// so the letters a student saw while taking the exam match the results/history screens.
    /// Falls back to the option's own label if it isn't part of the list (e.g. a deleted option).
    /// </summary>
    public static char DisplayLabel(IEnumerable<AnswerOption> options, AnswerOption target)
    {
        if (target is null) return '?';
        var ordered = ForDisplay(options);
        var idx = ordered.FindIndex(o => o.Id == target.Id);
        return idx >= 0 ? (char)('A' + idx) : target.Label;
    }

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
