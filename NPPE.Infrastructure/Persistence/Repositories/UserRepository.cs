using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NPPE.Application.Repositories;
using NPPE.Domain.Constants;
using NPPE.Domain.Entities;

namespace NPPE.Infrastructure.Persistence.Repositories;
public class UserRepository : IUserRepository
{
    private readonly UserManager<AppUser> _userManager;

    public UserRepository(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<AppUser?> GetUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    public async Task<AppUser?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task CreateAsync(AppUser user, string password)
    {
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Self-registered users are students — without this role they'd be locked out
        // of the entire student area (pricing, exams, results, history) by the
        // role/fallback authorization policies.
        var roleResult = await _userManager.AddToRoleAsync(user, NppeRoles.Student);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }
    }

    public async Task UpdateAsync(AppUser user)
    {
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    public async Task<AppUser?> GetByStripeCustomerIdAsync(string stripeCustomerId)
    {
        return await _userManager.Users
            .FirstOrDefaultAsync(u => u.StripeCustomerId == stripeCustomerId);
    }

    public async Task<AppUser?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId)
    {
        return await _userManager.Users
            .FirstOrDefaultAsync(u => u.StripeSubscriptionId == stripeSubscriptionId);
    }
}
