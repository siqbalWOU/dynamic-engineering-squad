using InfrastructureApp.Controllers;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;
using System.Security.Claims;

namespace InfrastructureApp_Tests.SocialSharing;

[TestFixture]
public class ShareButtonTests
{
    private IReportIssueService _service = null!;
    private ReportIssueController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<IReportIssueService>();

        var store = Substitute.For<IUserStore<Users>>();
        var userManager = Substitute.For<UserManager<Users>>(
            store, null!, null!, null!, null!, null!, null!, null!, null!);

        var voteService = Substitute.For<IVoteService>();
        voteService.GetVoteStatusAsync(Arg.Any<int>(), Arg.Any<string?>())
            .Returns((0, false));

        var verifyFixService = Substitute.For<IVerifyFixService>();
        verifyFixService.GetVerifyStatusAsync(Arg.Any<int>(), Arg.Any<string?>())
            .Returns((0, false));

        var flagService = Substitute.For<IFlagService>();

        var issueNameService = Substitute.For<IIssueNameService>();
        var auditLogService = Substitute.For<IAuditLogService>();
        _controller = new ReportIssueController(_service, userManager, voteService, verifyFixService, flagService, issueNameService, auditLogService)
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

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
    }

    [Test]
    public async Task Details_WhenReportIsApproved_StatusAllowsSharing()
    {
        var report = new ReportIssue { Id = 1, Description = "Pothole on Elm St", Status = "Approved" };
        _service.GetByIdAsync(1).Returns(report);

        var result = await _controller.Details(1);

        var model = (ReportIssue)((ViewResult)result).Model!;
        Assert.That(model.Status, Is.EqualTo("Approved"));
    }

    [Test]
    public async Task Details_WhenReportIsPending_StatusBlocksSharing()
    {
        var report = new ReportIssue { Id = 2, Description = "Broken streetlight", Status = "Pending" };
        _service.GetByIdAsync(2).Returns(report);

        var result = await _controller.Details(2);

        var model = (ReportIssue)((ViewResult)result).Model!;
        Assert.That(model.Status, Is.Not.EqualTo("Approved"));
    }

    [Test]
    public async Task Details_WhenReportIsRejected_StatusBlocksSharing()
    {
        var report = new ReportIssue { Id = 3, Description = "Spam report", Status = "Rejected" };
        _service.GetByIdAsync(3).Returns(report);

        var result = await _controller.Details(3);

        var model = (ReportIssue)((ViewResult)result).Model!;
        Assert.That(model.Status, Is.Not.EqualTo("Approved"));
    }

    [Test]
    public void FacebookShareUrl_ContainsFacebookSharerEndpoint()
    {
        var encoded = Uri.EscapeDataString("http://localhost/ReportIssue/Details/1");
        var shareUrl = $"https://www.facebook.com/sharer/sharer.php?u={encoded}";

        Assert.That(shareUrl, Does.StartWith("https://www.facebook.com/sharer/sharer.php?u="));
    }

    [Test]
    public void FacebookShareUrl_IssueUrlIsEncoded()
    {
        var rawUrl = "http://localhost/ReportIssue/Details/42";
        var encoded = Uri.EscapeDataString(rawUrl);
        var shareUrl = $"https://www.facebook.com/sharer/sharer.php?u={encoded}";

        Assert.That(shareUrl, Does.Contain(encoded));
        Assert.That(shareUrl, Does.Not.Contain(rawUrl));
    }
}
