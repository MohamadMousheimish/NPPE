namespace NPPE.Application.DTOs.ExamAttempts;
public record QuestionResultDto
{
    public string QuestionText { get; init; } = string.Empty;
    public char SelectedLabel { get; init; }
    public string SelectedText { get; init; } = string.Empty;
    public char CorrectLabel { get; init; }
    public string CorrectText { get; init; } = string.Empty;
    public bool IsCorrect { get; init; }

    // The admin-authored explanation for the answer given (localized wrapper is
    // composed in the view so this stays free of presentation concerns).
    public string Explanation { get; init; } = string.Empty;
}
