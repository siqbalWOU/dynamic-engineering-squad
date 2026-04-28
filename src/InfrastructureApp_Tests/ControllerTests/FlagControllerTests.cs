using System.Security.Claims;
using System.Threading.Tasks;
using InfrastructureApp.Controllers;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Controllers
{
    [TestFixture]
    public class FlagControllerTests
    {
        private IFlagService _flagService = null!;
        private UserManager<Users> _userManager = null!;
        private FlagController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            _flagService = Substitute.For<IFlagService>();
            _userManager = CreateUserManagerSubstitute();
            _controller = new FlagController(_flagService, _userManager);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-123")
            }, "TestAuth"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [TearDown]
        public void TearDown()
        {
            _userManager?.Dispose();
            _controller?.Dispose();
        }

        [Test]
        public async Task Create_WhenSuccessful_ReturnsJsonSuccess()
        {
            // Arrange
            int reportId = 1;
            string category = "Spam";
            _userManager.GetUserId(_controller.User).Returns("user-123");
            _flagService.FlagReportAsync(reportId, "user-123", category)
                .Returns((true, "Success message"));

            // Act
            var result = await _controller.Create(reportId, category);

            // Assert
            Assert.That(result, Is.TypeOf<JsonResult>());
            var jsonResult = (JsonResult)result;
            
            // Verify dynamic values
            var data = jsonResult.Value;
            var successProp = data!.GetType().GetProperty("success");
            var messageProp = data.GetType().GetProperty("message");
            
            Assert.That(successProp!.GetValue(data), Is.True);
            Assert.That(messageProp!.GetValue(data), Is.EqualTo("Success message"));
        }

        [Test]
        public async Task Create_WhenFails_ReturnsJsonFailure()
        {
            // Arrange
            int reportId = 1;
            _userManager.GetUserId(_controller.User).Returns("user-123");
            _flagService.FlagReportAsync(reportId, "user-123", "Spam")
                .Returns((false, "Already flagged"));

            // Act
            var result = await _controller.Create(reportId, "Spam");

            // Assert
            Assert.That(result, Is.TypeOf<JsonResult>());
            var jsonResult = (JsonResult)result;
            var data = jsonResult.Value;
            
            var successProp = data!.GetType().GetProperty("success");
            Assert.That(successProp!.GetValue(data), Is.False);
        }

        [Test]
        public async Task Status_ReturnsFlaggedState()
        {
            // Arrange
            int reportId = 1;
            _userManager.GetUserId(_controller.User).Returns("user-123");
            _flagService.HasUserFlaggedAsync(reportId, "user-123").Returns(true);

            // Act
            var result = await _controller.Status(reportId);

            // Assert
            Assert.That(result, Is.TypeOf<JsonResult>());
            var jsonResult = (JsonResult)result;
            var data = jsonResult.Value;
            
            var flaggedProp = data!.GetType().GetProperty("hasUserFlagged");
            Assert.That(flaggedProp!.GetValue(data), Is.True);
        }

        private static UserManager<Users> CreateUserManagerSubstitute()
        {
            var store = Substitute.For<IUserStore<Users>>();
            return Substitute.For<UserManager<Users>>(
                store, null!, null!, null!, null!, null!, null!, null!, null!);
        }
    }
}
