using InfrastructureApp.Controllers.API;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace InfrastructureApp_Tests
{
    [TestFixture]
    public class LatestReportsModalMapTests
    {
        // Mock repository used to simulate data access without calling the real database
        private IReportIssueRepository _repo = null!;

        // Mock logger required by the controller constructor
        private ILogger<ReportsAPIController> _logger = null!;

        // Instance of the API controller being tested
        private ReportsAPIController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            // Created a mock repository so the controller can be tested without calling the real database
            _repo = Substitute.For<IReportIssueRepository>();

            // Created a mock logger since the controller expects a logger dependency
            _logger = Substitute.For<ILogger<ReportsAPIController>>();

            // Create the controller instance using the mocked dependencies
            _controller = new ReportsAPIController(_repo, _logger);

            // Provide a minimal request environment so the controller behaves
            // like it is handling a real web request
            _controller.ControllerContext = new ControllerContext
            {
                // Set a basic request context so the controller behaves like it is handling
                // a real API request during the test
                HttpContext = new DefaultHttpContext()
            };
        }

        // -------------------------------------------------------
        // SCRUM-98:
        // Approved report should return OK for modal details API
        // -------------------------------------------------------
        [Test]
        public async Task GetReportById_WhenApprovedReportExists_ReturnsOk()
        {
            // Arrange: create an approved report with map data that the modal can use
            var report = new ReportIssue
            {
                Id = 5,
                Description = "Pothole near school",
                Status = "Approved",
                ImageUrl = "/uploads/issues/pothole.jpg",
                CreatedAt = new DateTime(2026, 3, 6, 10, 0, 0),
                Latitude = 44.9429m,
                Longitude = -123.0351m
            };

            // Arrange: repository should return this report when the controller asks for Id 5
            _repo.GetByIdAsync(5).Returns(report);

            // Act: call the modal details endpoint for this report
            var result = await _controller.GetReportById(5);

            // Assert: approved report should return HTTP 200 OK
            Assert.That(result.Result, Is.TypeOf<OkObjectResult>());

            // Convert the result to OkObjectResult so the test can check the data returned by the API
            var okResult = (OkObjectResult)result.Result!;

            // Assert: OK response should contain a value for the modal to use
            Assert.That(okResult.Value, Is.Not.Null);
        }

        // -------------------------------------------------------
        // SCRUM-98:
        // Missing report should return NotFound
        // -------------------------------------------------------
        [Test]
        public async Task GetReportById_WhenReportDoesNotExist_ReturnsNotFound()
        {
            // Arrange: repository returns null to simulate a missing report
            _repo.GetByIdAsync(999).Returns((ReportIssue?)null);

            // Act: request a report Id that does not exist
            var result = await _controller.GetReportById(999);

            // Assert: controller should return 404 NotFound for a missing report
            Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
        }

        // -------------------------------------------------------
        // SCRUM-98:
        // Non-approved report should not be returned to normal user
        // -------------------------------------------------------
        [Test]
        public async Task GetReportById_WhenPendingReportExists_ReturnsNotFound()
        {
            // Arrange: create a pending report that should be hidden from a normal user
            var report = new ReportIssue
            {
                Id = 7,
                Description = "Streetlight issue",
                Status = "Pending",
                Latitude = 44.95m,
                Longitude = -123.04m
            };

            // Arrange: repository returns the pending report
            _repo.GetByIdAsync(7).Returns(report);

            // Act: normal user requests the pending report
            var result = await _controller.GetReportById(7);

            // Assert: non-approved report should not be exposed, so return 404 NotFound
            Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
        }
    }
}