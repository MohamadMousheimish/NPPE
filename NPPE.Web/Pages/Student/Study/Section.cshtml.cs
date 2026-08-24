using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Domain.Entities;
using NPPE.Web.StudyContent;

namespace NPPE.Web.Pages.Student.Study
{
    [Authorize(Policy = "StudentOnly")]
    public class SectionModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;

        public SectionModel(UserManager<AppUser> userManager) => _userManager = userManager;

        public StudySection Section { get; private set; } = null!;
        public StudySection? Previous { get; private set; }
        public StudySection? Next { get; private set; }

        public async Task<IActionResult> OnGetAsync(int n = 1)
        {
            var section = StudyLibrary.Get(n);
            if (section == null)
                return RedirectToPage("Index");

            // Study Hub content is premium-only; send non-premium students to pricing.
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var user = userId == null ? null : await _userManager.FindByIdAsync(userId);
            if (user is null || !user.IsPremium)
                return RedirectToPage("/Payments/Pricing", new { returnUrl = $"/Student/Study/Section?n={n}" });

            Section = section;
            Previous = StudyLibrary.Get(n - 1);
            Next = StudyLibrary.Get(n + 1);
            return Page();
        }
    }
}
