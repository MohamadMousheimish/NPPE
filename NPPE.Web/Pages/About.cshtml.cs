using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NPPE.Web.Pages
{
    // Public marketing page — reachable before sign-in.
    [AllowAnonymous]
    public class AboutModel : PageModel
    {
        public void OnGet() { }
    }
}
