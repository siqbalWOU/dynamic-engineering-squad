using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using InfrastructureApp.Data;
using NSubstitute;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Services;

[TestFixture]
public class UserSearchTests
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

        services.AddLogging();

        var provider = services.BuildServiceProvider();
        _context = provider.GetRequiredService<ApplicationDbContext>();
        _userManager = provider.GetRequiredService<UserManager<Users>>();
        _roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();

        await _context.Database.EnsureCreatedAsync();

        _userService = new UserService(_userManager, _roleManager, _context, Substitute.For<IAuditLogService>());

        // Seed users
        await _userManager.CreateAsync(new Users { UserName = "alice", Email = "alice@test.com" });
        await _userManager.CreateAsync(new Users { UserName = "bob", Email = "bob@test.com" });
        await _userManager.CreateAsync(new Users { UserName = "charlie", Email = "charlie@test.com" });
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_context != null) await _context.DisposeAsync();
        if (_connection != null) await _connection.DisposeAsync();
        _userManager?.Dispose();
        _roleManager?.Dispose();
    }

    [Test]
    public async Task GetUsersWithRolesAsync_WithSearchTerm_ReturnsFilteredUsers()
    {
        // Act
        var result = await _userService.GetUsersWithRolesAsync(page: 1, pageSize: 10, searchTerm: "ali");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].UserName, Is.EqualTo("alice"));
        });
    }

    [Test]
    public async Task GetUsersWithRolesAsync_WithSearchTerm_IsCaseInsensitive()
    {
        // Act
        var result = await _userService.GetUsersWithRolesAsync(page: 1, pageSize: 10, searchTerm: "ALICE");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].UserName, Is.EqualTo("alice"));
        });
    }

    [Test]
    public async Task GetUsersWithRolesAsync_WithSearchTerm_ReturnsEmptyIfNoMatch()
    {
        // Act
        var result = await _userService.GetUsersWithRolesAsync(page: 1, pageSize: 10, searchTerm: "nonexistent");

        // Assert
        Assert.That(result, Is.Empty);
    }
}
