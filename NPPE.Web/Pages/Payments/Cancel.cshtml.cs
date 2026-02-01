using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NPPE.Web.Pages.Payments;

[Authorize(Roles = "Student")]
public class CancelModel : PageModel
{
    public void OnGet() { }
}
