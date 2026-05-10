using InfrastructureApp.Models;
using InfrastructureApp.ViewModels.Account;
using Microsoft.AspNetCore.Identity;

namespace InfrastructureApp.Services;

public interface IUserService
{
    public Task<PaginatedList<Users>>
        GetUsersWithRolesAsync(int page, int pageSize, string? searchTerm = null);
    Task<ManageUserRolesViewModel> GetManageRolesViewModelAsync(string userId);
    Task<IdentityResult> UpdateUserRolesAsync(ManageUserRolesViewModel model, string adminId);
    Task<IdentityResult> DeleteUserAsync(string userId, string adminId);
    Task<IdentityResult> DeleteAccountAsync(string userId, string currentPassword);
    Task<IdentityResult> BanUserAsync(string userId, string adminId, string reason);
    Task<IdentityResult> UnbanUserAsync(string userId, string adminId);
}