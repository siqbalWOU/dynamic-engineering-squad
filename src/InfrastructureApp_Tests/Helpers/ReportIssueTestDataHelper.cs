using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp_Tests.Helpers;

public static class ReportIssueTestDataHelper
{
    public static async Task<Users> EnsureTestUserAsync(
        ApplicationDbContext context,
        string userId,
        string? userName = null,
        string? email = null)
    {
        var existingUser = await context.Users.FirstOrDefaultAsync(user => user.Id == userId);
        if (existingUser != null)
        {
            return existingUser;
        }

        var resolvedUserName = string.IsNullOrWhiteSpace(userName) ? userId : userName;
        var resolvedEmail = string.IsNullOrWhiteSpace(email) ? $"{resolvedUserName}@test.local" : email;

        var user = new Users
        {
            Id = userId,
            UserName = resolvedUserName,
            NormalizedUserName = resolvedUserName.ToUpperInvariant(),
            Email = resolvedEmail,
            NormalizedEmail = resolvedEmail.ToUpperInvariant(),
            EmailConfirmed = true
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    public static async Task<InfrastructureApp.Models.ReportIssue> CreateTestReportAsync(
        ApplicationDbContext context,
        string description,
        string status,
        string userId,
        DateTime? createdAt = null,
        decimal? latitude = null,
        decimal? longitude = null,
        string? userName = null,
        string? email = null)
    {
        await EnsureTestUserAsync(context, userId, userName, email);

        var report = new InfrastructureApp.Models.ReportIssue
        {
            Description = description,
            Status = status,
            UserId = userId,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            Latitude = latitude,
            Longitude = longitude
        };

        context.ReportIssue.Add(report);
        await context.SaveChangesAsync();

        return report;
    }
}
