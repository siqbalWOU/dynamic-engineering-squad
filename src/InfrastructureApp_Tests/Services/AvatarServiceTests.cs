using InfrastructureApp.Models;
using InfrastructureApp.Services;
using InfrastructureApp.ViewModels.Account;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InfrastructureApp_Tests.Services
{
    [TestFixture]
    public class AvatarServiceTests
    {
        private Mock<UserManager<Users>> _mockUserManager = null!;
        private Mock<IWebHostEnvironment> _mockEnv = null!;          // ← add
        private AvatarService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _mockUserManager = MockUserManager();

            _mockEnv = new Mock<IWebHostEnvironment>();              // ← add
            _mockEnv.Setup(e => e.ContentRootPath)                  // ← add
                    .Returns(Path.GetTempPath());                    // ← add

            _service = new AvatarService(_mockUserManager.Object, _mockEnv.Object);  // ← add _mockEnv.Object
        }

        // -------------------------------
        //   Helper: Fake UserManager
        // -------------------------------
        private static Mock<UserManager<Users>> MockUserManager()
        {
            var store = new Mock<IUserStore<Users>>();
            return new Mock<UserManager<Users>>(
                store.Object, null, null, null, null, null, null, null, null
            );
        }

        // -------------------------------
        //   Helper: Fake IFormFile
        // -------------------------------
        private static IFormFile MakeFakeFile(string contentType, long sizeBytes)
        {
            var mock = new Mock<IFormFile>();
            var content = new byte[sizeBytes];
            var stream  = new MemoryStream(content);

            mock.Setup(f => f.ContentType).Returns(contentType);
            mock.Setup(f => f.Length).Returns(sizeBytes);
            mock.Setup(f => f.FileName).Returns("test.jpg");
            mock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            return mock.Object;
        }

        // -------------------------------
        //   TEST: Building the ViewModel
        // -------------------------------
        [Test]
        public void BuildChooseAvatarViewModel_ShouldBuildWithCorrectSelection()
        {
            var firstKey = AvatarCatalog.Keys.First();
            var user = new Users { AvatarKey = firstKey };

            var vm = _service.BuildChooseAvatarViewModel(user);

            Assert.That(vm.SelectedAvatarKey, Is.EqualTo(firstKey));
            Assert.That(vm.Options.Any(o => o.Key == firstKey), Is.True);
            Assert.That(vm.Options.Single(o => o.Key == firstKey).IsSelected, Is.True);
        }

        [Test]
        public void BuildChooseAvatarViewModel_ShouldOverrideSelection_WhenSelectedKeyProvided()
        {
            var firstKey = AvatarCatalog.Keys.First();
            var user = new Users { AvatarKey = firstKey };

            var vm = _service.BuildChooseAvatarViewModel(user);

            Assert.That(vm.SelectedAvatarKey, Is.EqualTo(firstKey));
            Assert.That(vm.Options.Any(o => o.Key == firstKey), Is.True);
            Assert.That(vm.Options.Single(o => o.Key == firstKey).IsSelected, Is.True);
        }

        [Test]
        public void BuildChooseAvatarViewModel_ShouldIncludeErrorMessage()
        {
            var user = new Users { AvatarKey = "avatar1" };

            var vm = _service.BuildChooseAvatarViewModel(user, error: "Something went wrong");

            Assert.That(vm.ErrorMessage, Is.EqualTo("Something went wrong"));
        }

        // -------------------------------
        //   TEST: SaveAvatarAsync
        // -------------------------------
        [Test]
        public async Task SaveAvatarAsync_ShouldFail_WhenAvatarKeyIsInvalid()
        {
            var user = new Users { AvatarKey = "avatar1" };

            var (success, error) = await _service.SaveAvatarAsync(user, null);

            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo("Please select an avatar."));
        }

        [Test]
        public async Task SaveAvatarAsync_ShouldSave_WhenValidAvatar()
        {
            var user = new Users { AvatarKey = "avatar1" };
            var newKey = AvatarCatalog.Keys.First();

            _mockUserManager
                .Setup(m => m.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            var (success, error) = await _service.SaveAvatarAsync(user, newKey);

            Assert.That(success, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(user.AvatarKey, Is.EqualTo(newKey));
        }

        [Test]
        public async Task SaveAvatarAsync_ShouldFail_WhenUserManagerFails()
        {
            var user = new Users { AvatarKey = "avatar1" };
            var newKey = AvatarCatalog.Keys.First();

            _mockUserManager
                .Setup(m => m.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Failed());

            var (success, error) = await _service.SaveAvatarAsync(user, newKey);

            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo("Could not save your avatar. Please try again."));
        }

        // -------------------------------
        //   TEST: SaveUploadedAvatarAsync
        // -------------------------------
        [Test]
        public async Task SaveUploadedAvatarAsync_ShouldFail_WhenFileIsNull()
        {
            var user = new Users();

            var (success, error) = await _service.SaveUploadedAvatarAsync(user, null!);

            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo("Please select an image file to upload."));
        }

        [Test]
        public async Task SaveUploadedAvatarAsync_ShouldFail_WhenContentTypeIsInvalid()
        {
            var user = new Users();
            var file = MakeFakeFile("image/gif", 1024);

            var (success, error) = await _service.SaveUploadedAvatarAsync(user, file);

            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo("Only JPG and PNG files are accepted."));
        }

        [Test]
        public async Task SaveUploadedAvatarAsync_ShouldFail_WhenFileTooLarge()
        {
            var user = new Users();
            var file = MakeFakeFile("image/jpeg", 6 * 1024 * 1024); // 6 MB

            var (success, error) = await _service.SaveUploadedAvatarAsync(user, file);

            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo("File exceeds the 5 MB size limit."));
        }

        [Test]
        public async Task SaveUploadedAvatarAsync_ShouldSave_WhenFileIsValid()
        {
            var user = new Users();
            var file = MakeFakeFile("image/png", 1024);

            _mockUserManager
                .Setup(m => m.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            var (success, error) = await _service.SaveUploadedAvatarAsync(user, file);

            Assert.That(success, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(user.AvatarUrl, Does.StartWith("/uploads/avatars/"));
            Assert.That(user.AvatarKey, Is.Null);
        }

        [Test]
        public async Task SaveUploadedAvatarAsync_ShouldFail_WhenUserManagerFails()
        {
            var user = new Users();
            var file = MakeFakeFile("image/jpeg", 1024);

            _mockUserManager
                .Setup(m => m.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Failed());

            var (success, error) = await _service.SaveUploadedAvatarAsync(user, file);

            Assert.That(success, Is.False);
            Assert.That(error, Is.EqualTo("Could not save your photo. Please try again."));
        }
    }
}