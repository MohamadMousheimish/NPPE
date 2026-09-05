using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Repositories;
using NPPE.Web.StudyContent;

namespace NPPE.Web.Pages.Student.Study
{
    [Authorize(Policy = "StudentOnly")]
    public class IndexModel : PageModel
    {
        private readonly IExamUnlockRepository _examUnlocks;

        public IndexModel(IExamUnlockRepository examUnlocks) => _examUnlocks = examUnlocks;

        // Full study materials unlock with any exam-pack purchase; the section cards
        // themselves stay visible to everyone as the free preview.
        public bool HasAccess { get; private set; }
        public IReadOnlyList<StudySection> Sections => StudyLibrary.Sections;

        public async Task OnGetAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
                HasAccess = (await _examUnlocks.GetUnlockedExamIdsAsync(userId)).Count > 0;
        }
    }
}
