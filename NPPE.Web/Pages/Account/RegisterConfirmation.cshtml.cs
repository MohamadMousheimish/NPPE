using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using NPPE.Application.Email;
using NPPE.Domain.Entities;
using NPPE.Web.Resources;
using NPPE.Web.Services;

namespace NPPE.Web.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public RegisterConfirmationModel(
            UserManager<AppUser> userManager,
            IEmailSender emailSender,
            IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _localizer = localizer;
        }

        [BindProperty(SupportsGet = true)]
        public string? Email { get; set; }

        public string? StatusMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostResendAsync()
        {
            // Always show the same neutral message so this can't be used to probe which
            // addresses are registered (or already confirmed).
            StatusMessage = _localizer["If your email needs confirming, we've sent a fresh link."].Value;

            if (!string.IsNullOrWhiteSpace(Email))
            {
                var user = await _userManager.FindByEmailAsync(Email);
                if (user != null && !await _userManager.IsEmailConfirmedAsync(user))
                {
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        null,
                        new { userId = user.Id, code },
                        protocol: Request.Scheme)!;

                    await ConfirmationEmail.SendAsync(_emailSender, _localizer, user.Email!, callbackUrl);
                }
            }

            return Page();
        }
    }
}
