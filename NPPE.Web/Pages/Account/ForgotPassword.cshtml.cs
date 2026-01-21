using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace NPPE.Web.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;

        public ForgotPasswordModel(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Don't reveal that email doesn't exist (security best practice)
                // Just show success message either way
                TempData["StatusMessage"] = "If your email is registered, you will receive a password reset link.";
                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                null,
                new { userId = user.Id, code = code },
                protocol: Request.Scheme);

            // TODO: In production, send real email
            // For now, log to console or show in dev
            Console.WriteLine($"Password reset link: {callbackUrl}");

            TempData["StatusMessage"] = "If your email is registered, you will receive a password reset link.";
            return RedirectToPage("./ForgotPasswordConfirmation");
        }

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid email address.")]
            public string Email { get; set; } = string.Empty;
        }
    }
}
