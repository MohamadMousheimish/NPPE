using NPPE.Domain.Entities;

namespace NPPE.Application.Repositories;
public interface IUserRepository
{
    Task<AppUser?> GetUserByIdAsync(string userId);
    Task<AppUser?> GetUserByEmailAsync(string email);
    Task CreateAsync(AppUser user, string password);
    Task UpdateAsync(AppUser user);
}
