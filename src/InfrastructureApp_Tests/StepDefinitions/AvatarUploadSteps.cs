using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using Reqnroll;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public class AvatarUploadSteps
    {
        private Users _user = new();
        private AvatarService _service = null!;
        private Mock<UserManager<Users>> _mockUserManager = null!;
        private (bool Success, string? ErrorMessage) _result;

        public AvatarUploadSteps()
        {
            var store = new Mock<IUserStore<Users>>();
            _mockUserManager = new Mock<UserManager<Users>>(
                store.Object, null, null, null, null, null, null, null, null);

            _mockUserManager
                .Setup(m => m.UpdateAsync(It.IsAny<Users>()))
                .ReturnsAsync(IdentityResult.Success);

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.Setup(e => e.ContentRootPath).Returns(Path.GetTempPath());

            _service = new AvatarService(_mockUserManager.Object, mockEnv.Object);
        }

        // ── Givens ──

        [Given("a registered user exists")]
        public void GivenARegisteredUserExists()
        {
            _user = new Users { UserName = "testuser", Email = "test@test.com" };
        }

        [Given("a registered user exists with an uploaded photo")]
        public void GivenARegisteredUserExistsWithUploadedPhoto()
        {
            _user = new Users
            {
                UserName = "testuser",
                Email    = "test@test.com",
                AvatarUrl = "/uploads/avatars/existing.jpg",
                AvatarKey = null
            };
        }

        // ── Whens ──

        [When("they upload a valid PNG file under 5MB")]
        public async Task WhenTheyUploadAValidPngFile()
        {
            var file = MakeFakeFile("image/png", 1024);
            _result = await _service.SaveUploadedAvatarAsync(_user, file);
        }


        [When("they upload a GIF file")]
        public async Task WhenTheyUploadAGifFile()
        {
            var file = MakeFakeFile("image/gif", 1024);
            _result = await _service.SaveUploadedAvatarAsync(_user, file);
        }

        [When("they select a preset avatar key")]
        public async Task WhenTheySelectAPresetAvatarKey()
        {
            var key = AvatarCatalog.Keys.First();
            _result = await _service.SaveAvatarAsync(_user, key);
        }

        // ── Thens ──

        [Then("the avatar should be saved successfully")]
        public void ThenTheAvatarShouldBeSavedSuccessfully()
        {
            Assert.That(_result.Success, Is.True);
        }

        [Then("the upload should fail")]
        public void ThenTheUploadShouldFail()
        {
            Assert.That(_result.Success, Is.False);
        }

        [Then("the avatar error message should be {string}")]
        public void ThenTheErrorMessageShouldBe(string expectedMessage)
        {
            Assert.That(_result.ErrorMessage, Is.EqualTo(expectedMessage));
        }

        [Then("the user AvatarUrl should start with {string}")]
        public void ThenTheUserAvatarUrlShouldStartWith(string prefix)
        {
            Assert.That(_user.AvatarUrl, Does.StartWith(prefix));
        }

        [Then("the user AvatarKey should be null")]
        public void ThenTheUserAvatarKeyShouldBeNull()
        {
            Assert.That(_user.AvatarKey, Is.Null);
        }

        [Then("the user AvatarKey should be set to the selected key")]
        public void ThenTheUserAvatarKeyShouldBeSet()
        {
            Assert.That(_user.AvatarKey, Is.Not.Null);
            Assert.That(AvatarCatalog.IsValid(_user.AvatarKey), Is.True);
        }

        [Then("the user AvatarUrl should be null")]
        public void ThenTheUserAvatarUrlShouldBeNull()
        {
            Assert.That(_user.AvatarUrl, Is.Null);
        }

        // ── Helper ──
        private static IFormFile MakeFakeFile(string contentType, long sizeBytes)
        {
            var mock    = new Mock<IFormFile>();
            var content = new byte[sizeBytes];
            var stream  = new MemoryStream(content);

            mock.Setup(f => f.ContentType).Returns(contentType);
            mock.Setup(f => f.Length).Returns(sizeBytes);
            mock.Setup(f => f.FileName).Returns("test.jpg");
            mock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            return mock.Object;
        }
    }
}
