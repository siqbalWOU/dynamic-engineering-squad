using InfrastructureApp.Controllers;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace InfrastructureApp_Tests
{
    [TestFixture]
    public class PublicProfileControllerTests
    {
        private Mock<IDashboardRepository> _repoMock = null!;

        [SetUp]
        public void SetUp()
        {
            _repoMock = new Mock<IDashboardRepository>();
        }

        private DashboardController CreateController() => new DashboardController(_repoMock.Object);

        [Test]
        public async Task Index_WithUsername_ReturnsViewResult()
        {
            _repoMock.Setup(r => r.GetPublicProfileAsync("alice", 1))
                     .ReturnsAsync(new DashboardViewModel { Username = "alice" });

            var result = await CreateController().Index("alice");

            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task Index_WithUsername_WhenUserNotFound_ReturnsNotFound()
        {
            _repoMock.Setup(r => r.GetPublicProfileAsync("ghost", 1))
                     .ReturnsAsync((DashboardViewModel?)null);

            var result = await CreateController().Index("ghost");

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task Index_WithUsername_CallsGetPublicProfileAsyncOnce()
        {
            _repoMock.Setup(r => r.GetPublicProfileAsync("alice", 1))
                     .ReturnsAsync(new DashboardViewModel { Username = "alice" });

            await CreateController().Index("alice");

            _repoMock.Verify(r => r.GetPublicProfileAsync("alice", 1), Times.Once);
        }

        [Test]
        public async Task Index_WithUsername_ModelIsNotOwnDashboard()
        {
            _repoMock.Setup(r => r.GetPublicProfileAsync("alice", 1))
                     .ReturnsAsync(new DashboardViewModel { Username = "alice", IsOwnDashboard = false });

            var result = await CreateController().Index("alice") as ViewResult;
            var model = result!.Model as DashboardViewModel;

            Assert.That(model!.IsOwnDashboard, Is.False);
        }

        [Test]
        public async Task Index_WithUsername_EmailIsNotExposed()
        {
            _repoMock.Setup(r => r.GetPublicProfileAsync("alice", 1))
                     .ReturnsAsync(new DashboardViewModel { Username = "alice", Email = "" });

            var result = await CreateController().Index("alice") as ViewResult;
            var model = result!.Model as DashboardViewModel;

            Assert.That(model!.Email, Is.Empty);
        }

        [Test]
        public async Task Index_WithUsername_PassesPageToRepository()
        {
            _repoMock.Setup(r => r.GetPublicProfileAsync("alice", 2))
                     .ReturnsAsync(new DashboardViewModel { Username = "alice", CurrentPage = 2, TotalPages = 3 });

            await CreateController().Index("alice", 2);

            _repoMock.Verify(r => r.GetPublicProfileAsync("alice", 2), Times.Once);
        }

        [Test]
        public async Task Index_WithUsername_ReturnsPublicProfileWithReports()
        {
            var reports = new List<PublicProfileReportViewModel>
            {
                new() { Id = 1, Title = "Broken Sidewalk", CreatedAt = new DateTime(2026, 5, 14, 0, 0, 0, DateTimeKind.Utc) }
            };
            _repoMock.Setup(r => r.GetPublicProfileAsync("alice", 1))
                     .ReturnsAsync(new DashboardViewModel { Username = "alice", PublicProfileReports = reports });

            var result = await CreateController().Index("alice") as ViewResult;
            var model = result!.Model as DashboardViewModel;

            Assert.That(model!.PublicProfileReports.Count, Is.EqualTo(1));
            Assert.That(model.PublicProfileReports[0].Title, Is.EqualTo("Broken Sidewalk"));
        }

        [Test]
        public async Task Index_WithoutUsername_CallsGetDashboardSummaryAsync()
        {
            _repoMock.Setup(r => r.GetDashboardSummaryAsync())
                     .ReturnsAsync(new DashboardViewModel());

            await CreateController().Index();

            _repoMock.Verify(r => r.GetDashboardSummaryAsync(), Times.Once);
        }

        [Test]
        public async Task Index_WithoutUsername_DoesNotCallGetPublicProfileAsync()
        {
            _repoMock.Setup(r => r.GetDashboardSummaryAsync())
                     .ReturnsAsync(new DashboardViewModel());

            await CreateController().Index();

            _repoMock.Verify(r => r.GetPublicProfileAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task Index_WithUsername_PaginationDefaults_PageOne()
        {
            _repoMock.Setup(r => r.GetPublicProfileAsync("alice", 1))
                     .ReturnsAsync(new DashboardViewModel { Username = "alice", CurrentPage = 1, TotalPages = 1 });

            var result = await CreateController().Index("alice") as ViewResult;
            var model = result!.Model as DashboardViewModel;

            Assert.That(model!.CurrentPage, Is.EqualTo(1));
        }
    }
}
