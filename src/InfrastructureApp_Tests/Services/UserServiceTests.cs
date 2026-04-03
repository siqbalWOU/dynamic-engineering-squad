using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels.Account;

namespace InfrastructureApp_Tests.Services;

[TestFixture]
public class UserServiceTests
{
    private SqliteConnection _connection = null!;
    private TestIdentityDbContext _context = null!;
    private UserManager<Users> _userManager = null!;
    private RoleManager<IdentityRole> _roleManager = null!;
    private UserService _userService = null!;

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var services = new ServiceCollection();

        services.AddDbContext<TestIdentityDbContext>(options => 
            options.UseSqlite(_connection));

        services.AddIdentity<Users, IdentityRole>()
            .AddEntityFrameworkStores<TestIdentityDbContext>()
            .AddDefaultTokenProviders();

        services.AddLogging(builder => builder.AddConsole());

        var provider = services.BuildServiceProvider();
        _context = provider.GetRequiredService<TestIdentityDbContext>();
        _userManager = provider.GetRequiredService<UserManager<Users>>();
        _roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();

        await _context.Database.EnsureCreatedAsync();

        _userService = new UserService(_userManager, _roleManager);
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
    public async Task GetUsersWithRolesAsync_ReturnsPaginatedUsersWithRolesPopulated()
    {
        var role = new IdentityRole("Editor");
        await _roleManager.CreateAsync(role);

        var user = new Users { UserName = "testuser", Email = "test@test.com" };
        await _userManager.CreateAsync(user);
        await _userManager.AddToRoleAsync(user, "Editor");

        var result = await _userService.GetUsersWithRolesAsync(page: 1, pageSize: 10);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].UserName, Is.EqualTo("testuser"));
            Assert.That(result[0].Roles, Contains.Item("Editor"));
        });
    }

    [Test]
    public async Task GetManageRolesViewModelAsync_ReturnsCorrectRoleSelections()
    {
        await _roleManager.CreateAsync(new IdentityRole("Admin"));
        await _roleManager.CreateAsync(new IdentityRole("User"));

        var user = new Users { UserName = "john_doe" };
        await _userManager.CreateAsync(user);
        await _userManager.AddToRoleAsync(user, "User"); 

        var model = await _userService.GetManageRolesViewModelAsync(user.Id);

        Assert.Multiple(() =>
        {
            Assert.That(model.UserId, Is.EqualTo(user.Id));
            Assert.That(model.UserName, Is.EqualTo("john_doe"));
            Assert.That(model.Roles, Has.Count.EqualTo(2));
            
            var adminRole = model.Roles.First(r => r.RoleName == "Admin");
            var userRole = model.Roles.First(r => r.RoleName == "User");
            
            Assert.That(adminRole.IsSelected, Is.False, "Should not have Admin selected.");
            Assert.That(userRole.IsSelected, Is.True, "Should have User selected.");
        });
    }

    [Test]
    public async Task UpdateUserRolesAsync_SuccessfullyUpdatesRoles()
    {
        await _roleManager.CreateAsync(new IdentityRole("RoleA"));
        await _roleManager.CreateAsync(new IdentityRole("RoleB"));

        var user = new Users { UserName = "jane_doe" };
        await _userManager.CreateAsync(user);
        await _userManager.AddToRoleAsync(user, "RoleA"); 

        var model = new ManageUserRolesViewModel
        {
            UserId = user.Id,
            Roles = new List<RoleSelection>
            {
                new RoleSelection { RoleName = "RoleA", IsSelected = false }, 
                new RoleSelection { RoleName = "RoleB", IsSelected = true }   
            }
        };

        var result = await _userService.UpdateUserRolesAsync(model, adminId: "some-other-admin-id");

        var currentRoles = await _userManager.GetRolesAsync(user);
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(currentRoles, Does.Not.Contain("RoleA"));
            Assert.That(currentRoles, Contains.Item("RoleB"));
        });
    }
    
    [Test]
    public async Task UpdateUserRolesAsync_PreventsAdminFromRemovingOwnAdminRole()
    {
        var user = new Users { UserName = "superadmin" };
        await _userManager.CreateAsync(user);
        
        var model = new ManageUserRolesViewModel
        {
            UserId = user.Id,
            Roles = new List<RoleSelection>
            {
                new RoleSelection { RoleName = "Admin", IsSelected = false } 
            }
        };

        var result = await _userService.UpdateUserRolesAsync(model, adminId: user.Id);
        
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.First().Description, Does.Contain("prevent lockout"));
        });
    }
    
    [Test]
    public async Task GetManageRolesViewModelAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        string fakeUserId = "this-id-does-not-exist";

        var result = await _userService.GetManageRolesViewModelAsync(fakeUserId);

        Assert.That(result, Is.Null, "Expected null when the user is not found in the database.");
    }

    [Test]
    public async Task GetManageRolesViewModelAsync_WhenNoRolesExist_ReturnsEmptyRolesList()
    {
        var user = new Users { UserName = "lonely_user" };
        await _userManager.CreateAsync(user);

        var result = await _userService.GetManageRolesViewModelAsync(user.Id);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.UserId, Is.EqualTo(user.Id));
            Assert.That(result.Roles, Is.Empty, "Expected the roles list to be empty since no roles exist in the system.");
        });
    }
    
    [Test]
    public async Task UpdateUserRolesAsync_WhenUserDoesNotExist_ReturnsFailedResult()
    {
        var model = new ManageUserRolesViewModel
        {
            UserId = "fake-user-id",
            Roles = new List<RoleSelection>()
        };

        var result = await _userService.UpdateUserRolesAsync(model, adminId: "some-admin-id");

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.First().Description, Is.EqualTo("User not found"));
        });
    }

    [Test]
    public async Task UpdateUserRolesAsync_AdminEditingSelf_KeepingAdminRole_Succeeds()
    {
        await _roleManager.CreateAsync(new IdentityRole("Admin"));
        await _roleManager.CreateAsync(new IdentityRole("User"));

        var user = new Users { UserName = "superadmin" };
        await _userManager.CreateAsync(user);
        await _userManager.AddToRoleAsync(user, "Admin");
        
        var model = new ManageUserRolesViewModel
        {
            UserId = user.Id,
            Roles = new List<RoleSelection>
            {
                new RoleSelection { RoleName = "Admin", IsSelected = true }, 
                new RoleSelection { RoleName = "User", IsSelected = true } 
            }
        };

        var result = await _userService.UpdateUserRolesAsync(model, adminId: user.Id);

        var updatedRoles = await _userManager.GetRolesAsync(user);
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True, "The update should succeed because they didn't uncheck Admin.");
            Assert.That(updatedRoles, Contains.Item("Admin"));
            Assert.That(updatedRoles, Contains.Item("User"));
        });
    }
}


// IdentityDbContext for Identity to function correctly in tests.
public class TestIdentityDbContext : IdentityDbContext<Users>
{
    public TestIdentityDbContext(DbContextOptions<TestIdentityDbContext> options) 
        : base(options) { }
}