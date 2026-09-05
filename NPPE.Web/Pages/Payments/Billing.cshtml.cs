using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Repositories;

namespace NPPE.Web.Pages.Payments;

[Authorize(Roles = "Student")]
public class BillingModel : PageModel
{
    private readonly IExamUnlockRepository _examUnlocks;
    private readonly IExamAttemptRepository _examAttempts;

    public BillingModel(IExamUnlockRepository examUnlocks, IExamAttemptRepository examAttempts)
    {
        _examUnlocks = examUnlocks;
        _examAttempts = examAttempts;
    }

    public record UnlockedExam(string Title, DateTime UnlockedAt, int AttemptsRemaining, int AttemptsAllowed);

    public List<UnlockedExam> Exams { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? throw new InvalidOperationException("User ID not found.");

        var unlocks = await _examUnlocks.GetForUserAsync(userId);
        foreach (var u in unlocks.Where(u => u.Exam != null))
        {
            var used = await _examAttempts.CountAttemptsAsync(userId, u.ExamId);
            Exams.Add(new UnlockedExam(
                u.Exam.Title,
                u.UnlockedAt,
                Math.Max(0, u.AttemptsAllowed - used),
                u.AttemptsAllowed));
        }
    }
}
