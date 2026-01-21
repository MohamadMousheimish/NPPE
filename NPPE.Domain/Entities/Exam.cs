namespace NPPE.Domain.Entities;
public class Exam : BaseEntity
{
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public bool IsActive { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
