using System.Reflection;
using InfrastructureApp.Controllers;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels.AuditLogs;
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
        public async Task Index_ReturnsPagedAuditLogViewModel()
        {
            var auditLogService = Substitute.For<IAuditLogService>();
            auditLogService.GetPageAsync(1, 50, Arg.Any<CancellationToken>())
                .Returns(new AuditLogsIndexViewModel
                {
                    Items = new List<AuditLog>
                    {
                        new AuditLog { Id = 7, UserName = "admin", Role = "Admin", Email = "admin@test.com", Action = "Did something", TimestampUtc = DateTime.UtcNow }
                    },
                    CurrentPage = 1,
                    PageSize = 50,
                    TotalItems = 1,
                    TotalPages = 1
                });

            var controller = new AuditLogsController(auditLogService);

            var result = await controller.Index(cancellationToken: CancellationToken.None);

            Assert.That(result, Is.TypeOf<ViewResult>());
            var model = (AuditLogsIndexViewModel)((ViewResult)result).Model!;
            Assert.That(model.Items.Count, Is.EqualTo(1));
            Assert.That(model.CurrentPage, Is.EqualTo(1));
            Assert.That(model.TotalPages, Is.EqualTo(1));
        }

        [Test]
        public async Task Index_PassesRequestedPageToService()
        {
            var auditLogService = Substitute.For<IAuditLogService>();
            auditLogService.GetPageAsync(2, 50, Arg.Any<CancellationToken>())
                .Returns(new AuditLogsIndexViewModel
                {
                    CurrentPage = 2,
                    PageSize = 50,
                    TotalPages = 3
                });

            var controller = new AuditLogsController(auditLogService);

            await controller.Index(2, CancellationToken.None);

            auditLogService.Received(1).GetPageAsync(2, 50, Arg.Any<CancellationToken>());
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
            Assert.That(content, Does.Contain("Previous"));
            Assert.That(content, Does.Contain("Next"));
            Assert.That(content, Does.Contain("Page @Model.CurrentPage / @Model.TotalPages"));
        }
    }
}
