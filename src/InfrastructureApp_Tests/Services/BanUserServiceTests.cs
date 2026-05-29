using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using InfrastructureApp.Data;
using NSubstitute;

namespace InfrastructureApp_Tests.Services;

[TestFixture]
public class BanUserServiceTests
{
    private SqliteConnection _connection = null!;
    private ApplicationDbContext _context = null!;
    private UserManager<Users> _userManager = null!;
    private RoleManager<IdentityRole> _roleManager = null!;
    private UserService _userService = null!;

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options => 
            options.UseSqlite(_connection));

        services.AddIdentity<Users, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddLogging(builder => builder.AddConsole());

        var provider = services.BuildServiceProvider();
        _context = provider.GetRequiredService<ApplicationDbContext>();
        _userManager = provider.GetRequiredService<UserManager<Users>>();
        _roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();

        await _context.Database.EnsureCreatedAsync();

        _userService = new UserService(_userManager, _roleManager, _context, Substitute.For<IAuditLogService>());
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_context != null) await _context.DisposeAsync();
        if (_connection != null) await _connection.DisposeAsync();
        _userManager.Dispose();
        _roleManager.Dispose();
    }

    [Test]
    public async Task BanUserAsync_SuccessfullyBansUser_AndUpdatesSecurityStamp()
    {
        var admin = new Users { UserName = "admin", Email = "admin@test.com" };
        await _userManager.CreateAsync(admin);

        var user = new Users { UserName = "malicious", Email = "bad@test.com" };
        await _userManager.CreateAsync(user);
        var originalSecurityStamp = user.SecurityStamp;

        var result = await _userService.BanUserAsync(user.Id, admin.Id, "Rule breaking");

        var updatedUser = await _userManager.FindByIdAsync(user.Id);
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(updatedUser!.IsBanned, Is.True);
            Assert.That(updatedUser.BanReason, Is.EqualTo("Rule breaking"));
            Assert.That(updatedUser.SecurityStamp, Is.Not.EqualTo(originalSecurityStamp));
        });
    }

    [Test]
    public async Task BanUserAsync_LogsModerationAction()
    {
        var admin = new Users { UserName = "admin", Email = "admin@test.com" };
        await _userManager.CreateAsync(admin);

        var user = new Users { UserName = "malicious", Email = "bad@test.com" };
        await _userManager.CreateAsync(user);

        await _userService.BanUserAsync(user.Id, admin.Id, "Rule breaking");

        var log = await _context.ModerationActionLogs.FirstOrDefaultAsync(l => l.Action == "Banned" && l.ModeratorId == admin.Id);
        Assert.Multiple(() =>
        {
            Assert.That(log, Is.Not.Null);
            Assert.That(log!.TargetContentSnapshot, Does.Contain("malicious"));
        });
    }

    [Test]
    public async Task UnbanUserAsync_SuccessfullyUnbansUser()
    {
        var admin = new Users { UserName = "admin", Email = "admin@test.com" };
        await _userManager.CreateAsync(admin);

        var user = new Users { UserName = "redeemed", Email = "good@test.com", IsBanned = true, BanReason = "Mistake" };
        await _userManager.CreateAsync(user);

        var result = await _userService.UnbanUserAsync(user.Id, admin.Id);

        var updatedUser = await _userManager.FindByIdAsync(user.Id);
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(updatedUser!.IsBanned, Is.False);
            Assert.That(updatedUser.BanReason, Is.Null);
        });
    }

    [Test]
    public async Task UnbanUserAsync_LogsModerationAction()
    {
        var admin = new Users { UserName = "admin", Email = "admin@test.com" };
        await _userManager.CreateAsync(admin);

        var user = new Users { UserName = "redeemed", Email = "good@test.com", IsBanned = true };
        await _userManager.CreateAsync(user);

        await _userService.UnbanUserAsync(user.Id, admin.Id);

        var log = await _context.ModerationActionLogs.FirstOrDefaultAsync(l => l.Action == "Unbanned" && l.ModeratorId == admin.Id);
        Assert.That(log, Is.Not.Null);
    }
}
