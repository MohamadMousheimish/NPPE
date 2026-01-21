namespace NPPE.Application.DTOs.ExamAttempts;
public record ExamAttemptSummaryDto
{
    public Guid Id { get; init; }
    public string ExamTitle { get; init; } = string.Empty;
    public int Score { get; init; }
    public int TotalQuestions { get; init; }
    public DateTime TakenAt { get; init; }
    public double Percentage => TotalQuestions > 0 ? Math.Round((double)Score / TotalQuestions * 100, 1) : 0;
}
