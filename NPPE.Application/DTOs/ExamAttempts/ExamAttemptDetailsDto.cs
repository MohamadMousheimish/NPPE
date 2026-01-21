namespace NPPE.Application.DTOs.ExamAttempts;
public record ExamAttemptDetailsDto
{
    public Guid Id { get; init; }
    public Guid ExamId { get; init; }
    public string StudentId { get; init; } = string.Empty;
    public string ExamTitle { get; init; } = string.Empty;
    public int Score { get; init; }
    public int TotalQuestions { get; init; }
    public List<QuestionResultDto> Questions { get; init; } = [];
}
