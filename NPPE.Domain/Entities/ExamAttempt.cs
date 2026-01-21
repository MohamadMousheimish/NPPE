namespace NPPE.Domain.Entities;
public class ExamAttempt : BaseEntity
{
    public string UserId { get; set; } = default!;
    public AppUser User { get; set; } = default!;
    public Guid ExamId { get; set; }
    public Exam Exam { get; set; } = default!;
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public DateTime TakenAt { get; set; }
    public ICollection<AttemptedAnswer> Answers { get; set; } = [];
}
