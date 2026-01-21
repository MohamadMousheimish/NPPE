namespace NPPE.Application.DTOs.ExamAttempts;
public record QuestionResultDto
{
    public string QuestionText { get; init; } = string.Empty;
    public char SelectedLabel { get; init; }
    public string SelectedText { get; init; } = string.Empty;
    public char CorrectLabel { get; init; }
    public string CorrectText { get; init; } = string.Empty;
    public bool IsCorrect { get; init; }
    public string FeedbackMessage { get; init; } = string.Empty;
}
