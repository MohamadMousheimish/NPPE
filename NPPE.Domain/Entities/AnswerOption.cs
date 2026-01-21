namespace NPPE.Domain.Entities;
public class AnswerOption : BaseEntity
{
    public string Text { get; set; } = default!;
    public char Label { get; set; } // A, B, C, D
    public bool IsCorrect { get; set; }
    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = default!;
}
