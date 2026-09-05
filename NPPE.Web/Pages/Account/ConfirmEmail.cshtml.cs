using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Domain.Entities;

namespace NPPE.Web.Pages.Account
{
    [AllowAnonymous]
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;

        public ConfirmEmailModel(UserManager<AppUser> userManager) => _userManager = userManager;

        /// <summary>True once the token has been validated and the email marked confirmed.</summary>
        public bool Succeeded { get; private set; }

        /// <summary>The confirmed address, so the confirmation page can offer a resend link on failure.</summary>
        public string? Email { get; private set; }

        public async Task<IActionResult> OnGetAsync(string? userId, string? code)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
                return RedirectToPage("/Account/Login");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Page(); // Succeeded stays false — show the failure state.

            Email = user.Email;
            var result = await _userManager.ConfirmEmailAsync(user, code);
            Succeeded = result.Succeeded;
            return Page();
        }
    }
}
