namespace NPPE.Domain.Entities;
public class Question : BaseEntity
{
    public string Text { get; set; } = default!;
    public Guid ExamId { get; set; }
    public Exam Exam { get; set; } = default!;
    public ICollection<AnswerOption> Options { get; set; } = new List<AnswerOption>();
    public string ExplanationForCorrect { get; set; } = default!;
    public string ExplanationForIncorrect { get; set; } = default!;
}
