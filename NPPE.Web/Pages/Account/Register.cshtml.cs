using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace NPPE.Web.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly IUserRepository _userRepository;
        private readonly SignInManager<AppUser> _signInManager;

        public RegisterModel(IUserRepository userRepository, SignInManager<AppUser> signInManager)
        {
            _userRepository = userRepository;
            _signInManager = signInManager;
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

            var existingUser = await _userRepository.GetUserByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Input.Email", "Email is already registered.");
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
                // Auto-login can be added later; for now, redirect to login
                return RedirectToPage("/Account/Login");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
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
