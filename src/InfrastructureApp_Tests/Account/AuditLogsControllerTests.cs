using System.Reflection;
using InfrastructureApp.Controllers;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace InfrastructureApp_Tests.Account
{
    [TestFixture]
    public class AuditLogsControllerTests
    {
        [Test]
        public void Controller_IsRestrictedToAdminRole()
        {
            var attribute = typeof(AuditLogsController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute!.Roles, Is.EqualTo("Admin"));
        }

        [Test]
        public async Task Index_ReturnsReadOnlyAuditLogList()
        {
            var auditLogService = Substitute.For<IAuditLogService>();
            auditLogService.GetLatestAsync(100, Arg.Any<CancellationToken>())
                .Returns(new List<AuditLog>
                {
                    new AuditLog { Id = 7, UserName = "admin", Role = "Admin", Email = "admin@test.com", Action = "Did something", TimestampUtc = DateTime.UtcNow }
                });

            var controller = new AuditLogsController(auditLogService);

            var result = await controller.Index(CancellationToken.None);

            Assert.That(result, Is.TypeOf<ViewResult>());
            var model = (IReadOnlyList<AuditLog>)((ViewResult)result).Model!;
            Assert.That(model.Count, Is.EqualTo(1));
        }

        [Test]
        public void IndexView_IsReadOnly()
        {
            var viewPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "InfrastructureApp", "Views", "AuditLogs", "Index.cshtml");
            var normalizedPath = Path.GetFullPath(viewPath);
            var content = File.ReadAllText(normalizedPath);

            Assert.That(content, Does.Not.Contain("<form"));
            Assert.That(content, Does.Not.Contain("Edit"));
            Assert.That(content, Does.Not.Contain("Delete"));
        }
    }
}
