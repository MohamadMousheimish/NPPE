using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Repositories;

namespace NPPE.Web.Pages.Student.Exams
{
    [Authorize(Policy = "StudentOnly")]
    public class IndexModel : PageModel
    {
        private readonly IExamUnlockRepository _examUnlocks;
        private readonly IExamAttemptRepository _examAttempts;

        public IndexModel(IExamUnlockRepository examUnlocks, IExamAttemptRepository examAttempts)
        {
            _examUnlocks = examUnlocks;
            _examAttempts = examAttempts;
        }

        public record UnlockedExam(Guid Id, string Title, string Description, DateTime CreatedAt, int AttemptsRemaining, int AttemptsAllowed);

        public List<UnlockedExam> Exams { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return;

            var unlocks = await _examUnlocks.GetForUserAsync(userId);
            foreach (var u in unlocks.Where(u => u.Exam != null && u.Exam.IsActive))
            {
                var used = await _examAttempts.CountAttemptsAsync(userId, u.ExamId);
                Exams.Add(new UnlockedExam(
                    u.ExamId,
                    u.Exam.Title,
                    u.Exam.Description,
                    u.Exam.CreatedAt,
                    Math.Max(0, u.AttemptsAllowed - used),
                    u.AttemptsAllowed));
            }
        }
    }
}
