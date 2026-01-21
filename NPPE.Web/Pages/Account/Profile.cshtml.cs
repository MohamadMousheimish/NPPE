using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NPPE.Web.Pages.Account
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public void OnGet()
        {
            FullName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Unknown";
            Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "Unknown";
            Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Student";
        }
    }
}
