using System;
using System.Security.Claims;
using System.Threading.Tasks;
using InfrastructureApp.Controllers;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;
using NUnit.Framework;
using InfrastructureApp.Services.ImageHashing;

namespace InfrastructureApp_Tests
{
    [TestFixture]
    public class ReportIssueControllerTests
    {
        //creates a fake service/fake substitute for users
        private IReportIssueService _service = null!;
        private UserManager<Users> _userManager = null!;

        [SetUp]
        public void SetUp()
        {
            _service = Substitute.For<IReportIssueService>();
            _userManager = MakeUserManager();
        }

        [TearDown]
        public void TearDown()
        {
            _userManager?.Dispose();
        }

        // -------------------------
        // Helpers
        // -------------------------

        private static UserManager<Users> MakeUserManager()
        {
            var store = Substitute.For<IUserStore<Users>>();
            // UserManager has a big ctor; pass null for dependencies you don't use.
            return Substitute.For<UserManager<Users>>(
                store,
                null, null, null, null, null, null, null, null);
        }

        private ReportIssueController MakeController(ClaimsPrincipal? user = null)
        {
            var controller = new ReportIssueController(_service, _userManager);

            // Set up HttpContext + User
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            if (user != null)
                controller.ControllerContext.HttpContext.User = user;

            // Set up TempData (so controller.TempData["Success"] works)
            controller.TempData = new TempDataDictionary(
                controller.ControllerContext.HttpContext,
                Substitute.For<ITempDataProvider>());

            return controller;
        }

        //This creates a fake authenticated user with the NameIdentifier claim, which is typically what Identity uses for user id.
        private static ClaimsPrincipal MakeUserPrincipal(string userId = "user-123")
        {
            // Note: Controller uses UserManager.GetUserId(User), which typically reads NameIdentifier.
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "testuser")
            }, authenticationType: "TestAuth");

            return new ClaimsPrincipal(identity);
        }

        // -------------------------
        // GET tests
        // -------------------------

        //Verifies the GET action returns a View (not redirect, not error). It doesn’t check view name or model, just the result type.
        [Test]
        public void ReportIssue_Get_ReturnsView()
        {
            var controller = MakeController();

            var result = controller.ReportIssue();

            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public void Create_Get_ReturnsView_WithNewModel()
        {
            var controller = MakeController();

            var result = controller.Create(null, null, null, null);

            Assert.That(result, Is.TypeOf<ViewResult>());
            var view = (ViewResult)result;
            Assert.That(view.Model, Is.TypeOf<ReportIssue>());

            var model = (ReportIssue)view.Model!;
            Assert.That(model.CameraId, Is.Null);
            Assert.That(model.CameraImageUrl, Is.Null);
            Assert.That(model.Latitude, Is.Null);
            Assert.That(model.Longitude, Is.Null);
        }

        [Test]
        public void Create_Get_WithQueryValues_PopulatesModel()
        {
            var controller = MakeController();

            var result = controller.Create("cam-1", "img-url", 44.85m, -123.23m);

            Assert.That(result, Is.TypeOf<ViewResult>());
            var view = (ViewResult)result;
            Assert.That(view.Model, Is.TypeOf<ReportIssue>());

            var model = (ReportIssue)view.Model!;
            Assert.That(model.CameraId, Is.EqualTo("cam-1"));
            Assert.That(model.CameraImageUrl, Is.EqualTo("img-url"));
            Assert.That(model.Latitude, Is.EqualTo(44.85m));
            Assert.That(model.Longitude, Is.EqualTo(-123.23m));
        }



        // -------------------------
        // POST Create tests
        // -------------------------

        //if model validation fails, controller should short-circuit and re-show the form, not save anything.
        [Test]
        public async Task Create_Post_InvalidModelState_ReturnsView_DoesNotCallService()
        {
            var controller = MakeController();
            var report = new ReportIssue();

            controller.ModelState.AddModelError("Description", "Required");

            var result = await controller.Create(report);

            Assert.That(result, Is.TypeOf<ViewResult>());
            var view = (ViewResult)result;
            Assert.That(view.Model, Is.SameAs(report));

            await _service.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!);
        }

        //provide a fake user, return user id, return created report, tests flows + side effects
        [Test]
        public async Task Create_Post_Valid_RedirectsToDetails_SetsTempData_CallsService_WithAuthenticatedUserId()
        {
            // Arrange
            var principal = MakeUserPrincipal("user-abc");
            var controller = MakeController(principal);

            _userManager.GetUserId(Arg.Any<ClaimsPrincipal>()).Returns("user-abc");
            _service.CreateAsync(Arg.Any<ReportIssue>(), "user-abc")
                .Returns(Task.FromResult((reportId: 123, status: "Approved")));

            var report = new ReportIssue(); // ModelState is valid unless you add errors in unit tests

            // Act
            var result = await controller.Create(report);

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            var redirect = (RedirectToActionResult)result;

            Assert.That(redirect.ActionName, Is.EqualTo(nameof(ReportIssueController.Details)));
            Assert.That(redirect.RouteValues, Is.Not.Null);
            Assert.That(redirect.RouteValues!["id"], Is.EqualTo(123));

            Assert.That(controller.TempData["Success"], Is.EqualTo("XP gained! +10 points awarded."));

            await _service.Received(1).CreateAsync(report, "user-abc");
        }

        //when userId is null, goes to default user-guid-001, controller doesn't crash if no authenticated user
        [Test]
        public async Task Create_Post_WhenUserIdNull_UsesFallbackUserGuid001()
        {
            // Arrange
            var controller = MakeController(); // no user set
            _userManager.GetUserId(Arg.Any<ClaimsPrincipal>()).Returns((string?)null);

            _service.CreateAsync(Arg.Any<ReportIssue>(), "user-guid-001")
                .Returns(Task.FromResult((reportId: 55, status: "Approved")));

            var report = new ReportIssue();

            // Act
            var result = await controller.Create(report);

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            await _service.Received(1).CreateAsync(report, "user-guid-001");
        }


        //tests your controller’s error handling for a known/expected exception.
        [Test]
        public async Task Create_Post_WhenServiceThrowsInvalidOperation_AddsModelError_ReturnsView()
        {
            // Arrange
            var controller = MakeController();
            _userManager.GetUserId(Arg.Any<ClaimsPrincipal>()).Returns("user-123");

            _service.CreateAsync(Arg.Any<ReportIssue>(), Arg.Any<string>())
                .Returns(_ => Task.FromException<(int reportId, string status)>(
                    new InvalidOperationException("attach a photo")));

            var report = new ReportIssue();

            // Act
            var result = await controller.Create(report);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            Assert.That(controller.ModelState.IsValid, Is.False);

            Assert.That(controller.ModelState.ContainsKey(string.Empty), Is.True);

            var errors = controller.ModelState[string.Empty].Errors;
            Assert.That(errors, Is.Not.Empty);
            Assert.That(errors[0].ErrorMessage, Does.Contain("attach a photo"));
        }

        //tests your controller’s error handling for a unexpected exception.
        [Test]
        public async Task Create_Post_WhenServiceThrowsUnknown_AddsGenericModelError_ReturnsView()
        {
            // Arrange
            var controller = MakeController();
            _userManager.GetUserId(Arg.Any<ClaimsPrincipal>()).Returns("user-123");

            _service.CreateAsync(Arg.Any<ReportIssue>(), Arg.Any<string>())
                .Returns(_ => Task.FromException<(int reportId, string status)>(
                    new Exception("db down")));

            var report = new ReportIssue();

            // Act
            var result = await controller.Create(report);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            Assert.That(controller.ModelState.IsValid, Is.False);

            Assert.That(controller.ModelState.ContainsKey(string.Empty), Is.True);

            var errors = controller.ModelState[string.Empty].Errors;
            Assert.That(errors, Is.Not.Empty);
            Assert.That(errors[0].ErrorMessage,
                Is.EqualTo("Something went wrong saving your report. Please try again."));
        }

        //tests the error when a user submits duplicate images
        [Test]
        public async Task Create_Post_WhenServiceThrowsDuplicateImage_AddsPhotoModelError_ReturnsView()
        {
            var controller = MakeController();
            _userManager.GetUserId(Arg.Any<ClaimsPrincipal>()).Returns("user-123");

            _service.CreateAsync(Arg.Any<ReportIssue>(), Arg.Any<string>())
                .Returns(_ => Task.FromException<(int reportId, string status)>(
                    new DuplicateImageException("You already used this image in a previous report. Please upload a different image.")));

            var report = new ReportIssue();

            var result = await controller.Create(report);

            Assert.That(result, Is.TypeOf<ViewResult>());
            Assert.That(controller.ModelState.IsValid, Is.False);

            Assert.That(controller.ModelState.ContainsKey(nameof(report.Photo)), Is.True);

            var errors = controller.ModelState[nameof(report.Photo)]!.Errors;
            Assert.That(errors, Is.Not.Empty);
            Assert.That(errors[0].ErrorMessage, Does.Contain("already used this image"));
        }

        // -------------------------
        // Details tests
        // -------------------------

        //if report not found, return not found
        [Test]
        public async Task Details_WhenReportNotFound_ReturnsNotFound()
        {
            var controller = MakeController();

            _service.GetByIdAsync(999).Returns((ReportIssue?)null);

            var result = await controller.Details(999);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        //verifying the controller passes the entity to the view
        [Test]
        public async Task Details_WhenReportFound_ReturnsView_WithModel()
        {
            var controller = MakeController();

            var report = new ReportIssue
            {
                Id = 7,
                Description = "Pothole",
                Status = "Pending",
                UserId = "user-1"
            };

            _service.GetByIdAsync(7).Returns(report);

            var result = await controller.Details(7);

            Assert.That(result, Is.TypeOf<ViewResult>());
            var view = (ViewResult)result;
            Assert.That(view.Model, Is.SameAs(report));
        }
    }
}
