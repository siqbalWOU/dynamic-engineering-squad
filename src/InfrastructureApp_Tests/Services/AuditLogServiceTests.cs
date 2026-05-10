using System.Security.Claims;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace InfrastructureApp_Tests.Services
{
    [TestFixture]
    public class AuditLogServiceTests
    {
        [Test]
        public async Task LogAsync_ResolvesUserFields_AndPersistsAuditLog()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("AuditLogService_" + Guid.NewGuid())
                .Options;

            await using var db = new ApplicationDbContext(options);
            var userManager = CreateUserManager();
            var user = new Users { Id = "user-1", UserName = "alice", Email = "alice@test.com" };

            userManager.FindByIdAsync("user-1").Returns(user);
            userManager.GetRolesAsync(user).Returns(new List<string> { "Admin" });

            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "user-1")
                    }, "TestAuth"))
                }
            };

            var service = new AuditLogService(db, userManager, httpContextAccessor, NullLogger<AuditLogService>.Instance);

            await service.LogAsync("Test audit action.", "user-1");

            var log = await db.AuditLogs.SingleAsync();
            Assert.Multiple(() =>
            {
                Assert.That(log.AspNetUserId, Is.EqualTo("user-1"));
                Assert.That(log.UserName, Is.EqualTo("alice"));
                Assert.That(log.Email, Is.EqualTo("alice@test.com"));
                Assert.That(log.Role, Is.EqualTo("Admin"));
                Assert.That(log.Action, Is.EqualTo("Test audit action."));
            });
        }

        [Test]
        public async Task LogAsync_WhenSaveFails_DoesNotThrowToCaller()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("AuditLogFailure_" + Guid.NewGuid())
                .Options;

            await using var db = new ThrowingAuditLogDbContext(options);
            var service = new AuditLogService(
                db,
                CreateUserManager(),
                new HttpContextAccessor(),
                NullLogger<AuditLogService>.Instance);

            Assert.DoesNotThrowAsync(async () => await service.LogAsync("This should not throw."));
        }

        [Test]
        public async Task GetLatestAsync_ReturnsNewestEntriesFirst()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("AuditLogLatest_" + Guid.NewGuid())
                .Options;

            await using var db = new ApplicationDbContext(options);
            db.AuditLogs.AddRange(
                new AuditLog { Action = "Older", TimestampUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc) },
                new AuditLog { Action = "Newest", TimestampUtc = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc) });
            await db.SaveChangesAsync();

            var service = new AuditLogService(
                db,
                CreateUserManager(),
                new HttpContextAccessor(),
                NullLogger<AuditLogService>.Instance);

            var logs = await service.GetLatestAsync();

            Assert.That(logs.Select(log => log.Action), Is.EqualTo(new[] { "Newest", "Older" }));
        }

        private static UserManager<Users> CreateUserManager()
        {
            var store = Substitute.For<IUserStore<Users>>();
            return Substitute.For<UserManager<Users>>(
                store, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        private sealed class ThrowingAuditLogDbContext : ApplicationDbContext
        {
            public ThrowingAuditLogDbContext(DbContextOptions<ApplicationDbContext> options)
                : base(options)
            {
            }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                throw new DbUpdateException("Expected failure.");
            }
        }
    }
}
