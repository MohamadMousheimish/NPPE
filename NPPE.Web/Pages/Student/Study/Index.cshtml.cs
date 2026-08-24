using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Domain.Entities;
using NPPE.Web.StudyContent;

namespace NPPE.Web.Pages.Student.Study
{
    [Authorize(Policy = "StudentOnly")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;

        public IndexModel(UserManager<AppUser> userManager) => _userManager = userManager;

        public bool IsPremium { get; private set; }
        public IReadOnlyList<StudySection> Sections => StudyLibrary.Sections;

        public async Task OnGetAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                var user = await _userManager.FindByIdAsync(userId);
                IsPremium = user?.IsPremium ?? false;
            }
        }
    }
}
