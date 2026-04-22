using System.Security.Claims;
using InfrastructureApp.Controllers;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace InfrastructureApp_Tests
{
    public class HomePageRecentReportsTests
    {
        private FakeReportIssueRepository _fakeRepo = null!;

        [SetUp]
        public void Setup()
        {
            // Setup fake repository before each test
            _fakeRepo = new FakeReportIssueRepository();
        }

        // -------------------------------------------------------
        // TEST 1: Check Index returns a ViewResult
        // This tests that the Home page loads correctly
        // -------------------------------------------------------
        [Test]
        public async Task Index_ReturnsViewResult()
        {
            // Arrange: create controller with fake logger and repo to simulate environment
            var controller = new HomeController(
                NullLogger<HomeController>.Instance,
                _fakeRepo
            );

            // Act: call Index method to load Home page
            var result = await controller.Index();

            // Assert: verify that the result is a ViewResult (page loads correctly)
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        // -------------------------------------------------------
        // TEST 2: Check only 3 reports are shown
        // Even if more reports exist, only 3 should display
        // -------------------------------------------------------
        [Test]
        public async Task Index_ReturnsOnlyThreeReports_WhenMoreThanThreeExist()
        {
            // Arrange: add 4 reports to repo to test limit behavior
            _fakeRepo.LatestReports = new List<ReportIssue>
            {
                new ReportIssue { Id = 1, Description = "Report 1", Status = "Approved" },
                new ReportIssue { Id = 2, Description = "Report 2", Status = "Approved" },
                new ReportIssue { Id = 3, Description = "Report 3", Status = "Approved" },
                new ReportIssue { Id = 4, Description = "Report 4", Status = "Approved" }
            };

            var controller = new HomeController(
                NullLogger<HomeController>.Instance,
                _fakeRepo
            );

            // Act: call Index and get model data from View
            var result = await controller.Index() as ViewResult;
            var model = result?.Model as List<ReportIssue>;

            // Assert: verify only 3 reports are returned (homepage preview limit)
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Count, Is.EqualTo(3));
        }

        // -------------------------------------------------------
        // TEST 3: Check empty list when no reports exist
        // Home page should show empty list instead of crashing
        // -------------------------------------------------------
        [Test]
        public async Task Index_ReturnsEmptyList_WhenRepositoryReturnsNoReports()
        {
            // Arrange: set repo to return empty list (no reports exist)
            _fakeRepo.LatestReports = new List<ReportIssue>();

            var controller = new HomeController(
                NullLogger<HomeController>.Instance,
                _fakeRepo
            );

            // Act: call Index and get model from View
            var result = await controller.Index() as ViewResult;
            var model = result?.Model as List<ReportIssue>;

            // Assert: verify model exists and contains 0 reports (no crash)
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Count, Is.EqualTo(0));
        }

        // -------------------------------------------------------
        // TEST 4: Check admin user passes true to repository
        // Admin should see all reports
        // -------------------------------------------------------
        [Test]
        public async Task Index_PassesAdminTrue_WhenUserIsAdmin()
        {
            // Arrange: create fake admin user and assign to HttpContext
            var adminUser = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Role, "Admin") },
                    "TestAuth"));

            var controller = new HomeController(
                NullLogger<HomeController>.Instance,
                _fakeRepo
            );

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = adminUser
                }
            };

            // Act: call Index which checks user role and passes value to repo
            await controller.Index();

            // Assert: verify repo received true for isAdmin (admin behavior works)
            Assert.That(_fakeRepo.LastIsAdminValue, Is.True);
        }

        // -------------------------------------------------------
        // TEST 5: Check behavior when repository is missing
        // Should return empty list instead of crashing
        // -------------------------------------------------------
        [Test]
        public async Task Index_ReturnsEmptyList_WhenRepositoryIsNotProvided()
        {
            // Arrange: create controller without repo to simulate missing dependency
            var controller = new HomeController(
                NullLogger<HomeController>.Instance
            );

            // Act: call Index and get model from View
            var result = await controller.Index() as ViewResult;
            var model = result?.Model as List<ReportIssue>;

            // Assert: verify it safely returns empty list instead of crashing
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Count, Is.EqualTo(0));
        }

        // -------------------------------------------------------
        // ADDED TEST for SCRUM 113
        // TEST 6: Check reports include ids for details navigation
        // Recent reports need valid ids so the details page can open
        // -------------------------------------------------------
        [Test]
        public async Task Index_ReturnsReportsWithIds_ForDetailsNavigation()
        {
            // Arrange: add reports with valid ids
            _fakeRepo.LatestReports = new List<ReportIssue>
            {
                new ReportIssue { Id = 10, Description = "Report 1", Status = "Approved" },
                new ReportIssue { Id = 11, Description = "Report 2", Status = "Approved" }
            };

            var controller = new HomeController(
                NullLogger<HomeController>.Instance,
                _fakeRepo
            );

            // Act: call Index and get model from View
            var result = await controller.Index() as ViewResult;
            var model = result?.Model as List<ReportIssue>;

            // Assert: verify all returned reports have ids
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.All(r => r.Id > 0), Is.True);
        }

        // -------------------------------------------------------
        // Fake Repository for testing (no real database)
        // -------------------------------------------------------
        private class FakeReportIssueRepository : IReportIssueRepository
        {
            public List<ReportIssue> LatestReports { get; set; } = new();
            public bool LastIsAdminValue { get; private set; }

            public Task<ReportIssue?> GetByIdAsync(int id)
            {
                return Task.FromResult<ReportIssue?>(null);
            }

            public Task AddAsync(ReportIssue report)
            {
                return Task.CompletedTask;
            }

            public Task SaveChangesAsync()
            {
                return Task.CompletedTask;
            }

            public Task<List<ReportIssue>> GetLatestReportsAsync(bool isAdmin)
            {
                LastIsAdminValue = isAdmin;
                return Task.FromResult(LatestReports);
            }

            public Task<List<ReportIssue>> SearchLatestReportsAsync(bool isAdmin, string? keyword, string? sort)
            {
                return Task.FromResult(new List<ReportIssue>());
            }
        }
    }
}