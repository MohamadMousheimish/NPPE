namespace NPPE.Domain.Entities;

/// <summary>
/// A student review/testimonial. Submitted by students (linked via <see cref="UserId"/>)
/// or seeded directly by an admin (UserId null). Only <see cref="IsApproved"/> rows are
/// shown publicly on the landing page.
/// </summary>
public class Feedback : BaseEntity
{
    public string? UserId { get; set; }        // null for admin-seeded testimonials
    public AppUser? User { get; set; }

    public string AuthorName { get; set; } = default!;   // shown publicly, e.g. "Ada L."
    public string? AuthorTitle { get; set; }             // optional, e.g. "EIT · Ontario"
    public int Rating { get; set; }                       // 1..5
    public string Comment { get; set; } = default!;
    public bool IsApproved { get; set; }
}
