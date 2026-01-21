using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;
using NPPE.Web.Resources;

namespace NPPE.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly UserManager<AppUser> _userManager;
    private readonly IExamAttemptRepository _examAttemptRepository;
    private readonly IExamRepository _examRepository;

    public IndexModel(
        ILogger<IndexModel> logger,
        IStringLocalizer<SharedResource> localizer,
        UserManager<AppUser> userManager,
        IExamAttemptRepository examAttemptRepository,
        IExamRepository examRepository)
    {
        _logger = logger;
        _localizer = localizer;
        _userManager = userManager;
        _examAttemptRepository = examAttemptRepository;
        _examRepository = examRepository;
    }

    public string UserName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public int ExamsCompleted { get; set; }
    public int AverageScore { get; set; }
    public int TotalExamsAvailable { get; set; }
    public List<RecentExamAttempt> RecentAttempts { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (User.Identity != null && !User.Identity.IsAuthenticated)
        {
            return RedirectToPage("/Account/Login");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            UserName = $"{user.FirstName} {user.LastName}".Trim();
            FirstName = user.FirstName;
            IsAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var attempts = await _examAttemptRepository.GetAttemptsByUserIdAsync(user.Id);
            var completedAttempts = attempts.ToList();

            ExamsCompleted = completedAttempts.Count;
            AverageScore = completedAttempts.Count > 0
                ? (int)completedAttempts.Average(a => a.Score)
                : 0;

            RecentAttempts = completedAttempts
                .OrderByDescending(a => a.TakenAt)
                .Take(5)
                .Select(a => new RecentExamAttempt
                {
                    ExamTitle = a.Exam?.Title ?? "Unknown Exam",
                    Score = a.Score,
                    CompletedAt = a.TakenAt,
                    AttemptId = a.Id
                })
                .ToList();
        }

        var allExams = await _examRepository.GetAllAsync();
        TotalExamsAvailable = allExams.Count();

        return Page();
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync();
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        return RedirectToPage("/Account/Login");
    }

    public class RecentExamAttempt
    {
        public string ExamTitle { get; set; } = string.Empty;
        public int Score { get; set; }
        public DateTime CompletedAt { get; set; }
        public Guid AttemptId { get; set; }
    }
}
