namespace NPPE.Domain.Entities;
public class Question : BaseEntity
{
    public string Text { get; set; } = default!;
    public Guid ExamId { get; set; }
    public Exam Exam { get; set; } = default!;
    public ICollection<AnswerOption> Options { get; set; } = new List<AnswerOption>();
    public string ExplanationForCorrect { get; set; } = default!;
    public string ExplanationForIncorrect { get; set; } = default!;

    // French translations (nullable). When empty, the UI falls back to the English above.
    public string? TextFr { get; set; }
    public string? ExplanationForCorrectFr { get; set; }
    public string? ExplanationForIncorrectFr { get; set; }

    // Soft-delete flag. Deleted questions are excluded from exams and admin
    // listings but kept in the database so historical attempt results still
    // resolve the question they referenced.
    public bool IsActive { get; set; } = true;
}
