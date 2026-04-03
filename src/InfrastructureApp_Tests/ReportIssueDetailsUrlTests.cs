using InfrastructureApp.Controllers;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;

namespace InfrastructureApp_Tests
{
    [TestFixture]
    public class ReportIssueDetailsUrlTests
    {
        // Mock service used to simulate report data for the controller tests.
        private IReportIssueService _service = null!;

        // Controller instance being tested.
        private ReportIssueController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            // Create a mock service so we can control what the controller receives
            // without calling the real database.
            _service = Substitute.For<IReportIssueService>();

            // SCRUM-101
            // Initialize the controller used to test the Details URL behavior.
            _controller = new ReportIssueController(_service, null!);
        }

        [TearDown]
        public void TearDown()
        {
            // Dispose controller after each test to keep the test lifecycle clean.
            _controller?.Dispose();
        }

        // -------------------------------------------------------
        // SCRUM-101
        // Test that a valid report URL loads the report details page.
        //
        // Example real URL:
        // /ReportIssue/Details/5
        //
        // The service returns a report and the controller should
        // return a ViewResult containing that report as the model.
        // -------------------------------------------------------
        [Test]
        public async Task Details_WhenReportExists_ReturnsViewResult()
        {
            // Arrange
            // Create a fake report that the service will return.
            var report = new ReportIssue
            {
                Id = 5,
                Description = "Pothole near school",
                Status = "Approved"
            };

            // Configure the mock service so that when the controller
            // asks for report id 5, it receives this fake report.
            _service.GetByIdAsync(5).Returns(report);

            // Act
            // Call the controller action exactly the same way
            // ASP.NET MVC would when the URL is visited.
            var result = await _controller.Details(5);

            // Assert
            // The controller should return a ViewResult (Details.cshtml).
            Assert.That(result, Is.TypeOf<ViewResult>());

            // Verify the correct report model was passed to the view.
            var viewResult = (ViewResult)result;
            Assert.That(viewResult.Model, Is.EqualTo(report));
        }

        // -------------------------------------------------------
        // SCRUM-101
        // Test that an invalid report URL returns NotFound.
        //
        // Example invalid URL:
        // /ReportIssue/Details/999
        //
        // If the service cannot find the report, the controller
        // should return a 404 NotFound response instead of a view.
        // -------------------------------------------------------
        [Test]
        public async Task Details_WhenReportDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            // Simulate the service returning no report.
            _service.GetByIdAsync(999).Returns((ReportIssue?)null);

            // Act
            var result = await _controller.Details(999);

            // Assert
            // Controller should return a NotFound result (HTTP 404).
            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }
    }
}