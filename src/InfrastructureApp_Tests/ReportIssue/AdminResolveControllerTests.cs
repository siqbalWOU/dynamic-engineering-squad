using System.Security.Claims;
using InfrastructureApp.Controllers;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;
using NUnit.Framework;

namespace InfrastructureApp_Tests
{
    [TestFixture]
    public class AdminResolveControllerTests
    {
        private IReportIssueService _service = null!;
        private ReportIssueController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            _service = Substitute.For<IReportIssueService>();

            var store = Substitute.For<IUserStore<Users>>();
            var userManager = Substitute.For<UserManager<Users>>(
                store, null, null, null, null, null, null, null, null);

            var voteService = Substitute.For<IVoteService>();
            voteService.GetVoteStatusAsync(Arg.Any<int>(), Arg.Any<string?>())
                .Returns((0, false));

            var verifyService = Substitute.For<IVerifyFixService>();
            verifyService.GetVerifyStatusAsync(Arg.Any<int>(), Arg.Any<string?>())
                .Returns((0, false));

            var flagService = Substitute.For<IFlagService>();

            _controller = new ReportIssueController(_service, userManager, voteService, verifyService, flagService)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity())
                    }
                }
            };

            _controller.TempData = new TempDataDictionary(
                _controller.ControllerContext.HttpContext,
                Substitute.For<ITempDataProvider>());
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
        }

        // ── MarkResolved ──────────────────────────────────────────────────────

        [Test]
        public async Task MarkResolved_WhenReportExists_RedirectsToDetails()
        {
            _service.UpdateStatusAsync(5, "Resolved").Returns(true);

            var result = await _controller.MarkResolved(5);

            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            var redirect = (RedirectToActionResult)result;
            Assert.That(redirect.ActionName, Is.EqualTo("Details"));
            Assert.That(redirect.RouteValues!["id"], Is.EqualTo(5));
        }

        [Test]
        public async Task MarkResolved_WhenReportExists_SetsTempData()
        {
            _service.UpdateStatusAsync(5, "Resolved").Returns(true);

            await _controller.MarkResolved(5);

            Assert.That(_controller.TempData["Success"], Is.EqualTo("Report marked as Resolved and added to the verify queue."));
        }

        [Test]
        public async Task MarkResolved_WhenReportNotFound_ReturnsNotFound()
        {
            _service.UpdateStatusAsync(999, "Resolved").Returns(false);

            var result = await _controller.MarkResolved(999);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task MarkResolved_CallsServiceWithCorrectStatus()
        {
            _service.UpdateStatusAsync(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

            await _controller.MarkResolved(7);

            await _service.Received(1).UpdateStatusAsync(7, "Resolved");
        }

        // ── MarkVerifiedFixed ─────────────────────────────────────────────────

        [Test]
        public async Task MarkVerifiedFixed_WhenReportExists_RedirectsToDetails()
        {
            _service.UpdateStatusAsync(5, "Verified Fixed").Returns(true);

            var result = await _controller.MarkVerifiedFixed(5);

            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            var redirect = (RedirectToActionResult)result;
            Assert.That(redirect.ActionName, Is.EqualTo("Details"));
            Assert.That(redirect.RouteValues!["id"], Is.EqualTo(5));
        }

        [Test]
        public async Task MarkVerifiedFixed_WhenReportExists_SetsTempData()
        {
            _service.UpdateStatusAsync(5, "Verified Fixed").Returns(true);

            await _controller.MarkVerifiedFixed(5);

            Assert.That(_controller.TempData["Success"], Is.EqualTo("Report marked as Verified Fixed."));
        }

        [Test]
        public async Task MarkVerifiedFixed_WhenReportNotFound_ReturnsNotFound()
        {
            _service.UpdateStatusAsync(999, "Verified Fixed").Returns(false);

            var result = await _controller.MarkVerifiedFixed(999);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task MarkVerifiedFixed_CallsServiceWithCorrectStatus()
        {
            _service.UpdateStatusAsync(Arg.Any<int>(), Arg.Any<string>()).Returns(true);

            await _controller.MarkVerifiedFixed(7);

            await _service.Received(1).UpdateStatusAsync(7, "Verified Fixed");
        }
    }
}
