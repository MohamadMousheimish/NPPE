using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using NPPE.Application.Email;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;
using NPPE.Web.Resources;
using NPPE.Web.Services;
using System.ComponentModel.DataAnnotations;

namespace NPPE.Web.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly RegistrationDomainPolicy _domainPolicy;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public RegisterModel(
            IUserRepository userRepository,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IEmailSender emailSender,
            RegistrationDomainPolicy domainPolicy,
            IStringLocalizer<SharedResource> localizer)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _domainPolicy = domainPolicy;
            _localizer = localizer;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // Reserve the company's own domain — the public can't self-register under our
            // brand; those accounts are provisioned by the admin instead.
            if (_domainPolicy.IsReserved(Input.Email))
            {
                ModelState.AddModelError("Input.Email",
                    _localizer["This email domain is reserved. Please use a personal email address."]);
                return Page();
            }

            var existingUser = await _userRepository.GetUserByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Input.Email", _localizer["Email is already registered."]);
                return Page();
            }

            var user = new AppUser
            {
                FirstName = Input.FirstName.Trim(),
                LastName = Input.LastName.Trim(),
                Email = Input.Email,
                UserName = Input.Email
            };

            try
            {
                await _userRepository.CreateAsync(user, Input.Password);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }

            // Email confirmation is required before sign-in — send the verification link
            // and land the user on a "check your email" page (no auto-login).
            await SendConfirmationLinkAsync(user);

            return RedirectToPage("/Account/RegisterConfirmation", new { email = user.Email });
        }

        private async Task SendConfirmationLinkAsync(AppUser user)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                null,
                new { userId = user.Id, code },
                protocol: Request.Scheme)!;

            await ConfirmationEmail.SendAsync(_emailSender, _localizer, user.Email!, callbackUrl);
        }

        public IActionResult OnPostGoogleSignup()
        {
            var redirectUrl = Url.Page("./ExternalLoginCallback", pageHandler: null, values: new { returnUrl = "/" });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return new ChallengeResult("Google", properties);
        }

        public class InputModel
        {
            [Required(ErrorMessage = "First name is required.")]
            [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Last name is required.")]
            [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid email address.")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required.")]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please confirm your password.")]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            [DataType(DataType.Password)]
            public string ConfirmPassword { get; set; } = string.Empty;
        }
    }
}
