namespace NPPE.Domain.Entities;
public class Question : BaseEntity
{
    public string Text { get; set; } = default!;
    public Guid ExamId { get; set; }
    public Exam Exam { get; set; } = default!;
    public ICollection<AnswerOption> Options { get; set; } = new List<AnswerOption>();
    public string ExplanationForCorrect { get; set; } = default!;
    public string ExplanationForIncorrect { get; set; } = default!;

    // Soft-delete flag. Deleted questions are excluded from exams and admin
    // listings but kept in the database so historical attempt results still
    // resolve the question they referenced.
    public bool IsActive { get; set; } = true;
}
