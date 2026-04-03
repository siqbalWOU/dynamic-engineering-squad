using InfrastructureApp.Models;
using InfrastructureApp.ViewModels.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services;

public class UserService : IUserService
{
    private readonly UserManager<Users> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserService(UserManager<Users> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    } 

    public async Task<PaginatedList<Users>> GetUsersWithRolesAsync(int page, int pageSize)
    {
        var query = _userManager.Users.Select(u => new Users
        {
            Id = u.Id,
            UserName = u.UserName,
            Email = u.Email
        });

        var pagedUsers = await PaginatedList<Users>.CreateAsync(query, page, pageSize);
        
        foreach (var user in pagedUsers)
        {
            var identityUser = await _userManager.FindByIdAsync(user.Id);
            user.Roles = (await _userManager.GetRolesAsync(identityUser)).ToList();
        }

        return pagedUsers;
    }
    
    public async Task<ManageUserRolesViewModel> GetManageRolesViewModelAsync(string Id)
    {
        var user = await _userManager.FindByIdAsync(Id);
        if (user == null) return null;

        var allRoles = await _roleManager.Roles.ToListAsync();
        var model = new ManageUserRolesViewModel 
        { 
            UserId = user.Id, 
            UserName = user.UserName 
        };

        foreach (var role in allRoles)
        {
            model.Roles.Add(new RoleSelection
            {
                RoleName = role.Name,
                IsSelected = await _userManager.IsInRoleAsync(user, role.Name)
            });
        }
        return model;
    }

    public async Task<IdentityResult> UpdateUserRolesAsync(ManageUserRolesViewModel model, string adminId)
    {
        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        if (user.Id == adminId)
        {
            var adminRoleChecked = model.Roles
                .Any(r => r.RoleName == "Admin" && r.IsSelected);

            if (!adminRoleChecked)
            {
                return IdentityResult.Failed(new IdentityError 
                { 
                    Description = "You cannot remove the Admin role from your own account to prevent lockout." 
                });
            }
        }
        
        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

        if (!removeResult.Succeeded)
        {
            return removeResult;
        }
        
        var selectedRoles = model.Roles.Where(x => x.IsSelected).Select(y => y.RoleName);
        return await _userManager.AddToRolesAsync(user, selectedRoles);
    }
}