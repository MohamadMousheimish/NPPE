using NPPE.Application.DTOs.Questions;

namespace NPPE.Application.DTOs.Exams;
public record ExamWithQuestionsDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<QuestionDto> Questions { get; init; } = new();
}
