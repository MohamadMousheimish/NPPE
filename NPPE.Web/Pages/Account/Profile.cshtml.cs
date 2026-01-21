using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Web.Pages.Account
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IExamAttemptRepository _examAttemptRepository;

        public ProfileModel(UserManager<AppUser> userManager, IExamAttemptRepository examAttemptRepository)
        {
            _userManager = userManager;
            _examAttemptRepository = examAttemptRepository;
        }

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string MemberSince { get; set; } = string.Empty;
        public int ExamsCompleted { get; set; }
        public int AverageScore { get; set; }

        public async Task OnGetAsync()
        {
            FullName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Unknown";
            Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "Unknown";
            Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Student";

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                MemberSince = user.CreatedAt.ToString("MMMM yyyy");

                var attempts = await _examAttemptRepository.GetAttemptsByUserIdAsync(user.Id);
                var completedAttempts = attempts.ToList();

                ExamsCompleted = completedAttempts.Count;
                AverageScore = completedAttempts.Count > 0
                    ? (int)completedAttempts.Average(a => a.Score)
                    : 0;
            }
            else
            {
                MemberSince = "Unknown";
                ExamsCompleted = 0;
                AverageScore = 0;
            }
        }
    }
}
