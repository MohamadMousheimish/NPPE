namespace NPPE.Domain.Entities;
public class Exam : BaseEntity
{
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public bool IsActive { get; set; }

    /// <summary>When the exam was last deactivated (soft-deleted). Null while active.
    /// Used by the maintenance job to purge exams that have been inactive long enough.</summary>
    public DateTime? DeactivatedAt { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
