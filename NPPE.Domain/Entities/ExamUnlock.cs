using NPPE.Domain.Constants;

namespace NPPE.Domain.Entities;

/// <summary>
/// Grants a student access to one exam — created when they buy an exam pack. The exam
/// may be attempted up to <see cref="AttemptsAllowed"/> times; attempts used are counted
/// from the student's <see cref="ExamAttempt"/> rows for that exam.
/// </summary>
public class ExamUnlock : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = default!;

    public Guid ExamId { get; set; }
    public Exam Exam { get; set; } = default!;

    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
    public int AttemptsAllowed { get; set; } = PricingPlans.AttemptsPerExam;
}
