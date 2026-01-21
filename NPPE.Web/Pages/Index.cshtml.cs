using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using NPPE.Web.Resources;

namespace NPPE.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public IndexModel(ILogger<IndexModel> logger, IStringLocalizer<SharedResource> localizer)
    {
        _logger = logger;
        _localizer = localizer;
    }

    public IActionResult OnGet()
    {
        if (User.Identity != null  && !User.Identity.IsAuthenticated)
        {
            // Redirect to login for anonymous users
            return RedirectToPage("/Account/Login");
        }

        // Usage of localizer in model:
        //ModelState.AddModelError(string.Empty, _localizer["Invalid login attempt."]);

        // Later: redirect to Student Dashboard or Admin Dashboard based on role
        return Page(); // For now, just render Index if authenticated
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync();
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        return RedirectToPage("/Account/Login");
    }
}
