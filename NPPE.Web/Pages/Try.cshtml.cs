using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Web.StudyContent;

namespace NPPE.Web.Pages;

/// <summary>
/// Public 3-question free trial. No account, no payment. The same three questions every
/// time (SampleQuestions.All), so repeat visits can't harvest new content. Once a visitor
/// finishes, a cookie is set and any return visit bounces to the landing page.
/// </summary>
[AllowAnonymous]
public class TryModel : PageModel
{
    public const string DoneCookie = "nppe_sample_done";

    public IReadOnlyList<SampleQuestions.Question> Questions => SampleQuestions.All;

    public IActionResult OnGet()
    {
        // Already completed once → no going back to the questions.
        if (Request.Cookies.ContainsKey(DoneCookie))
            return RedirectToPage("/Index");
        return Page();
    }
}
