using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Repositories;
using NPPE.Web.StudyContent;

namespace NPPE.Web.Pages.Student.Study
{
    [Authorize(Policy = "StudentOnly")]
    public class SectionModel : PageModel
    {
        private readonly IExamUnlockRepository _examUnlocks;

        public SectionModel(IExamUnlockRepository examUnlocks) => _examUnlocks = examUnlocks;

        public StudySection Section { get; private set; } = null!;
        public StudySection? Previous { get; private set; }
        public StudySection? Next { get; private set; }

        public async Task<IActionResult> OnGetAsync(int n = 1)
        {
            var section = StudyLibrary.Get(n);
            if (section == null)
                return RedirectToPage("Index");

            // Full reading is premium — requires at least one exam-pack purchase.
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var hasAccess = userId != null && (await _examUnlocks.GetUnlockedExamIdsAsync(userId)).Count > 0;
            if (!hasAccess)
                return RedirectToPage("/Payments/Pricing", new { returnUrl = $"/Student/Study/Section?n={n}" });

            Section = section;
            Previous = StudyLibrary.Get(n - 1);
            Next = StudyLibrary.Get(n + 1);
            return Page();
        }
    }
}
