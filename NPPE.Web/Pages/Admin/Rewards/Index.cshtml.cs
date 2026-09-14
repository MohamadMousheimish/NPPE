using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Rewards.ModerateReward;
using NPPE.Application.DTOs.Rewards;
using NPPE.Application.Queries.Rewards.GetAllRewards;

namespace NPPE.Web.Pages.Admin.Rewards;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly IMediator _mediator;
    public IndexModel(IMediator mediator) => _mediator = mediator;

    public List<AdminRewardDto> Items { get; private set; } = new();

    private string AdminId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

    public async Task OnGetAsync() => Items = await _mediator.Send(new GetAllRewardsQuery());

    public async Task<IActionResult> OnPostApproveAsync(Guid id)
    {
        var ok = await _mediator.Send(new ApproveCompletionRewardCommand(id, AdminId));
        TempData["SuccessMessage"] = ok
            ? "Refund issued to the student's card."
            : "Could not issue the refund — check the claim is pending and the payment is refundable.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid id, string? reason)
    {
        await _mediator.Send(new RejectCompletionRewardCommand(id, AdminId, reason));
        TempData["SuccessMessage"] = "Claim rejected.";
        return RedirectToPage();
    }
}
