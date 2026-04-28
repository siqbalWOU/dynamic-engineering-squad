using System.Security.Claims;
using System.Threading.Tasks;
using InfrastructureApp.Controllers;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using InfrastructureApp.Services.ContentModeration;
using InfrastructureApp.Services.ImageSeverity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;

namespace InfrastructureApp_Tests.ControllerTests
{
    [TestFixture]
    public sealed class ImageSeverityControllerTests
    {
        [Test]
        public async Task Details_When_Report_Has_High_Severity_Returns_View_With_High_Model()
        {
            var service = Substitute.For<IReportIssueService>();
            var userManager = CreateUserManagerSubstitute();

            userManager.GetUserId(Arg.Any<ClaimsPrincipal>()).Returns("user-guid-001");

            var controller = CreateController(service, userManager);

            var report = new ReportIssue
            {
                Id = 101,
                Status = "Approved",
                Description = "Large pothole near campus",
                SeverityStatus = ImageSeverityStatuses.High,
                SeverityReason = "Large pothole with deep cracking",
                Latitude = 44.9429m,
                Longitude = -123.0351m
            };

            service.GetByIdAsync(101).Returns(report);

            var result = await controller.Details(101);

            var view = result as ViewResult;
            Assert.That(view, Is.Not.Null);

            var model = view!.Model as ReportIssue;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.SeverityStatus, Is.EqualTo(ImageSeverityStatuses.High));
            Assert.That(model.SeverityReason, Does.Contain("Large pothole with deep cracking"));
        }

        [Test]
        public async Task Details_When_Report_Has_Pending_Severity_Returns_View_With_Pending_Model()
        {
            var service = Substitute.For<IReportIssueService>();
            var userManager = CreateUserManagerSubstitute();

            userManager.GetUserId(Arg.Any<ClaimsPrincipal>()).Returns("user-guid-001");

            var controller = CreateController(service, userManager);

            var report = new ReportIssue
            {
                Id = 202,
                Status = "Approved",
                Description = "Cracked sidewalk near library",
                SeverityStatus = ImageSeverityStatuses.Pending,
                SeverityReason = null,
                Latitude = 44.9429m,
                Longitude = -123.0351m
            };

            service.GetByIdAsync(202).Returns(report);

            var result = await controller.Details(202);

            var view = result as ViewResult;
            Assert.That(view, Is.Not.Null);

            var model = view!.Model as ReportIssue;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.SeverityStatus, Is.EqualTo(ImageSeverityStatuses.Pending));
            Assert.That(model.SeverityReason, Is.Null);
        }

        [Test]
        public async Task Create_Post_When_Image_Moderation_Is_Rejected_Returns_Create_View_With_Model_Error()
        {
            var service = Substitute.For<IReportIssueService>();
            var userManager = CreateUserManagerSubstitute();

            userManager.GetUserId(Arg.Any<ClaimsPrincipal>()).Returns("user-guid-001");

            var controller = CreateController(service, userManager);

            var report = new ReportIssue
            {
                Description = "Bad image upload test",
                Latitude = 44.9429m,
                Longitude = -123.0351m,
                CameraImageUrl = "https://example.com/test-image.jpg"
            };

            service.CreateAsync(Arg.Any<ReportIssue>(), Arg.Any<string>())
                .Returns(Task.FromException<(int reportId, string status)>(
                    new ContentModerationRejectedException(
                        "The uploaded image contains inappropriate content and cannot be submitted.",
                        "flagged")));

            var result = await controller.Create(report);

            var view = result as ViewResult;
            Assert.That(view, Is.Not.Null);
            Assert.That(view!.Model, Is.SameAs(report));

            Assert.That(controller.ModelState.IsValid, Is.False);
            Assert.That(
                controller.ModelState[string.Empty]!.Errors[0].ErrorMessage,
                Does.Contain("cannot be submitted"));
        }

        private static ReportIssueController CreateController(
            IReportIssueService service,
            UserManager<Users> userManager)
        {
            var voteService = Substitute.For<IVoteService>();
            voteService.GetVoteStatusAsync(Arg.Any<int>(), Arg.Any<string?>())
                .Returns((0, false));

            var verifyFixService = Substitute.For<IVerifyFixService>();
            verifyFixService.GetVerifyStatusAsync(Arg.Any<int>(), Arg.Any<string?>())
                .Returns((0, false));

            var flagService = Substitute.For<IFlagService>();

            return new ReportIssueController(service, userManager, voteService, verifyFixService, flagService)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity())
                    }
                }
            };
        }

        private static UserManager<Users> CreateUserManagerSubstitute()
        {
            var store = Substitute.For<IUserStore<Users>>();

            return Substitute.For<UserManager<Users>>(
                store,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);
        }
    }
}