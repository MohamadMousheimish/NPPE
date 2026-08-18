using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using NPPE.Application.Email;
using NPPE.Domain.Entities;
using NPPE.Web.Resources;
using System.ComponentModel.DataAnnotations;

namespace NPPE.Web.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ForgotPasswordModel(
            UserManager<AppUser> userManager,
            IEmailSender emailSender,
            IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _localizer = localizer;
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

            var subject = _localizer["Reset your password"];
            var body =
                $"<p>{_localizer["We received a request to reset your password."]}</p>" +
                $"<p><a href=\"{callbackUrl}\">{_localizer["Set a new password"]}</a></p>" +
                $"<p>{_localizer["If you didn't request this, you can safely ignore this email."]}</p>";
            await _emailSender.SendEmailAsync(user.Email!, subject, body);

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
