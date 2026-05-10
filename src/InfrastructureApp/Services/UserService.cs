using InfrastructureApp.Models;
using InfrastructureApp.ViewModels.Account;
using InfrastructureApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services;

public class UserService : IUserService
{
    private readonly UserManager<Users> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public UserService(UserManager<Users> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IAuditLogService auditLogService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _auditLogService = auditLogService;
    } 

    public async Task<PaginatedList<Users>> GetUsersWithRolesAsync(int page, int pageSize, string? searchTerm = null)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim().ToLower();
            query = query.Where(u => u.UserName != null && u.UserName.ToLower().Contains(searchTerm));
        }

        query = query.OrderBy(u => u.UserName);

        var pagedUsers = await PaginatedList<Users>.CreateAsync(query, page, pageSize);
        
        foreach (var user in pagedUsers)
        {
            user.Roles = (await _userManager.GetRolesAsync(user)).ToList();
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
        
        var selectedRoles = model.Roles.Where(x => x.IsSelected).Select(y => y.RoleName).ToArray();
        var addResult = await _userManager.AddToRolesAsync(user, selectedRoles);

        if (addResult.Succeeded)
        {
            var rolesLabel = selectedRoles.Length == 0 ? "No roles" : string.Join(", ", selectedRoles);
            await _auditLogService.LogAsync(
                $"User role changed. TargetUserId={user.Id}; TargetUserName={user.UserName ?? "(unknown)"}; Roles={rolesLabel}.",
                adminId);
        }

        return addResult;
    }

    public async Task<IdentityResult> DeleteUserAsync(string userId, string adminId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        if (user.Id == adminId)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Description = "You cannot delete your own account."
            });
        }

        await _userManager.UpdateSecurityStampAsync(user);
        return await _userManager.DeleteAsync(user);
    }

    public async Task<IdentityResult> DeleteAccountAsync(string userId, string currentPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        var passwordValid = await _userManager.CheckPasswordAsync(user, currentPassword);
        if (!passwordValid)
        {
            return IdentityResult.Failed(new IdentityError { Description = "Incorrect password." });
        }

        await _userManager.UpdateSecurityStampAsync(user);
        return await _userManager.DeleteAsync(user);
    }

    public async Task<IdentityResult> BanUserAsync(string userId, string adminId, string reason)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        if (user.Id == adminId)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Description = "You cannot ban your own account."
            });
        }

        user.IsBanned = true;
        user.BanReason = reason;

        // Immediately terminate any of that user's active sessions.
        await _userManager.UpdateSecurityStampAsync(user);
        
        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            _context.ModerationActionLogs.Add(new ModerationActionLog
            {
                ModeratorId = adminId,
                Action = "Banned",
                TargetContentSnapshot = $"User {user.UserName} banned. Reason: {reason}",
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return result;
    }

    public async Task<IdentityResult> UnbanUserAsync(string userId, string adminId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        user.IsBanned = false;
        user.BanReason = null;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            _context.ModerationActionLogs.Add(new ModerationActionLog
            {
                ModeratorId = adminId,
                Action = "Unbanned",
                TargetContentSnapshot = $"User {user.UserName} unbanned.",
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return result;
    }
}
