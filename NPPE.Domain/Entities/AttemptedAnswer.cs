namespace NPPE.Domain.Entities;

// An immutable record of one answer a student gave during an exam attempt.
//
// QuestionId and SelectedOptionId are intentionally stored as plain Guids
// (snapshots) rather than enforced foreign keys. A student's answer history must
// remain accurate for the exam exactly as it was at the time it was taken, even
// after an admin later edits, removes, or soft-deletes questions/options. A hard
// FK would either cascade-delete this history or block admins from ever changing
// an option that has been answered. IsCorrect is likewise captured at submit time.
public class AttemptedAnswer : BaseEntity
{
    public Guid ExamAttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid SelectedOptionId { get; set; }
    public bool IsCorrect { get; set; }
}
