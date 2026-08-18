namespace NPPE.Application.Documents;

/// <summary>Result of parsing an uploaded exam document (best-effort; issues are flagged for review, not rejected).</summary>
public class ParsedExamResult
{
    /// <summary>False when the document doesn't look like an NPPE exam at all (no questions found).</summary>
    public bool Recognized { get; set; }

    /// <summary>Top-level error shown when <see cref="Recognized"/> is false.</summary>
    public string? Error { get; set; }

    public List<ParsedQuestion> Questions { get; set; } = new();
}

public class ParsedQuestion
{
    public string Text { get; set; } = string.Empty;
    public List<ParsedOption> Options { get; set; } = new();
    public string ExplanationForCorrect { get; set; } = string.Empty;
    public string ExplanationForIncorrect { get; set; } = string.Empty;

    /// <summary>Non-blocking notes about how this question was parsed (e.g. "options had no letters — assigned by order").</summary>
    public List<string> Notes { get; set; } = new();
}

public class ParsedOption
{
    public char Label { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
