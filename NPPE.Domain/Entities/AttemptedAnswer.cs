namespace NPPE.Domain.Entities;
public class AttemptedAnswer : BaseEntity
{
    public Guid ExamAttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid SelectedOptionId { get; set; }
    public bool IsCorrect { get; set; }
}
