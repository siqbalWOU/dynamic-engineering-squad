using InfrastructureApp.Models;
using InfrastructureApp.ViewModels.Account;
using Microsoft.AspNetCore.Identity;

namespace InfrastructureApp.Services;

public interface IUserService
{
    public Task<PaginatedList<Users>>
        GetUsersWithRolesAsync(int page, int pageSize);
    Task<ManageUserRolesViewModel> GetManageRolesViewModelAsync(string userId);
    Task<IdentityResult> UpdateUserRolesAsync(ManageUserRolesViewModel model, string adminId);
    Task<IdentityResult> DeleteUserAsync(string userId, string adminId);
    Task<IdentityResult> DeleteAccountAsync(string userId, string currentPassword);
}