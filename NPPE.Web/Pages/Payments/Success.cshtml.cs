using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NPPE.Web.Pages.Payments
{
    [Authorize(Roles = "Student")]
    public class SuccessModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public void OnGet()
        {
            // Optionally show success message
        }
    }
}
