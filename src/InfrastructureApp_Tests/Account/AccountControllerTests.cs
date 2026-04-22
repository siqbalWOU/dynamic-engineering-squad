using Microsoft.AspNetCore.Mvc.Routing;
using InfrastructureApp.Controllers;
using InfrastructureApp.Models;
using InfrastructureApp.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using InfrastructureApp.Services;

namespace InfrastructureApp_Tests.Account;

[TestFixture]
public class AccountControllerTests
{
    private Mock<UserManager<Users>> _mockUserManager;
    private Mock<SignInManager<Users>> _mockSignInManager;
    private Mock<IAvatarService> _mockAvatarService;
    private Mock<IUserService> _mockUserService;
    private Mock<IEmailService> _mockEmailService;
    private Mock<ILogger<AccountController>> _mockLogger;
    private Mock<IUrlHelper> _mockUrlHelper;
    private AccountController _controller;

    [SetUp]
    public void Setup()
    {
        var store = new Mock<IUserStore<Users>>();
        _mockUserManager = new Mock<UserManager<Users>>(store.Object, null, null, null, null, null, null, null, null);
        _mockSignInManager = new Mock<SignInManager<Users>>(
            _mockUserManager.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<Users>>().Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<ILogger<SignInManager<Users>>>().Object,
            new Mock<IAuthenticationSchemeProvider>().Object,
            new Mock<IUserConfirmation<Users>>().Object
        );
        _mockAvatarService = new Mock<IAvatarService>();
        _mockUserService = new Mock<IUserService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<AccountController>>();
        _mockUrlHelper = new Mock<IUrlHelper>();
        
        _controller = new AccountController(
            _mockUserManager.Object, 
            _mockSignInManager.Object, 
            _mockAvatarService.Object, 
            _mockUserService.Object,
            _mockEmailService.Object,
            _mockLogger.Object);
            
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        _controller.Url = _mockUrlHelper.Object;
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
    }

    [Test]
    public async Task RegisterPost_ReturnsView_WhenModelStateInvalid()
    {
        _controller.ModelState.AddModelError("Email", "Required");
        var model = new RegisterViewModel();

        var result = await _controller.Register(model);

        var viewResult = result as ViewResult;
        Assert.That(model, Is.EqualTo(viewResult.Model));
    }

    [Test]
    public async Task RegisterPost_RedirectsToRegisterConfirmation_WhenSuccessfullyRegistered()
    {
        var model = new RegisterViewModel()
        {
            Username = "testuser",
            Email = "test@test.com",
            Password = "Password2@"
        };

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<Users>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<Users>()))
            .ReturnsAsync("token");
        _mockUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
            .Returns("http://localhost/confirm");

        var result = await _controller.Register(model);

        var redirect = result as RedirectToActionResult;

        Assert.That(redirect.ActionName, Is.EqualTo("RegisterConfirmation"));
        Assert.That(redirect.RouteValues["email"], Is.EqualTo("test@test.com"));
        _mockEmailService.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task RegisterPost_AddsErrors_WhenIdentityFails()
    {
        var model = new RegisterViewModel
        {
            Username = "Test",
            Email = "email@test.com",
        };

        var identityError = new IdentityError
        {
            Description = "Password is too simple."
        };

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<Users>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(identityError));

        var result = await _controller.Register(model);

        Assert.That(_controller.ModelState.IsValid, Is.False);
        Assert.That(_controller.ModelState[string.Empty].Errors[0].ErrorMessage, Is.EqualTo("Password is too simple."));
    }

    [Test]
    public async Task ConfirmEmail_ReturnsNotFound_WhenUserDoesNotExist()
    {
        _mockUserManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Users)null);

        var result = await _controller.ConfirmEmail("invalid", "token");

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task ConfirmEmail_ReturnsView_WhenSuccessful()
    {
        var user = new Users { Id = "user1" };
        _mockUserManager.Setup(x => x.FindByIdAsync("user1"))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.ConfirmEmailAsync(user, "token"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _controller.ConfirmEmail("user1", "token");

        Assert.That(result, Is.InstanceOf<ViewResult>());
        var viewResult = result as ViewResult;
        Assert.That(viewResult.ViewName, Is.Null.Or.EqualTo("ConfirmEmail"));
    }

    [Test]
    public async Task ConfirmEmail_ReturnsErrorView_WhenTokenInvalid()
    {
        var user = new Users { Id = "user1" };
        _mockUserManager.Setup(x => x.FindByIdAsync("user1"))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.ConfirmEmailAsync(user, "wrong-token"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        var result = await _controller.ConfirmEmail("user1", "wrong-token");

        Assert.That(result, Is.InstanceOf<ViewResult>());
        var viewResult = result as ViewResult;
        Assert.That(viewResult.ViewName, Is.EqualTo("ErrorConfirmingEmail"));
    }
        
    [Test]
    public async Task Login_ReturnsView_WhenModelStateIsInvalid()
    {
        _controller.ModelState.AddModelError("UserName", "Required");
        var model = new LoginViewModel();
        
        var result = await _controller.Login(model);
        
        Assert.That(result, Is.InstanceOf<ViewResult>());
        var viewResult = result as ViewResult;
        Assert.That(viewResult.Model, Is.EqualTo(model));
    }
    
    [Test]
    public async Task Login_RedirectsToHome_WhenSignInSucceeds()
    {
        var model = new LoginViewModel 
        { 
            UserName = "test@example.com", 
            Password = "Password123!", 
            RememberMe = false 
        };

        _mockSignInManager
            .Setup(x => x.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        
        var result = await _controller.Login(model);
        
        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var redirect = result as RedirectToActionResult;
        Assert.That(redirect.ActionName, Is.EqualTo("Index"));
        Assert.That(redirect.ControllerName, Is.EqualTo("Home"));
    }
    
    [Test]
    public async Task Login_AddsError_WhenSignInFails()
    {
        var model = new LoginViewModel { UserName = "wrong@user.com", Password = "WrongPassword" };

        _mockSignInManager
            .Setup(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        
        var result = await _controller.Login(model);
        
        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.That(_controller.ModelState.IsValid, Is.False);
        Assert.That(_controller.ModelState.ContainsKey(string.Empty), Is.True);
        
        var error = _controller.ModelState[string.Empty].Errors[0];
        Assert.That(error.ErrorMessage, Is.EqualTo("Invalid login attempt."));
    }

    [Test]
    public async Task Login_ShowsResendButton_WhenEmailNotConfirmed()
    {
        var model = new LoginViewModel { UserName = "unconfirmed", Password = "Password123!" };
        var user = new Users { Id = "user1", UserName = "unconfirmed" };

        _mockSignInManager
            .Setup(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.NotAllowed);
        
        _mockUserManager.Setup(x => x.FindByNameAsync("unconfirmed"))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user))
            .ReturnsAsync(false);

        var result = await _controller.Login(model);

        Assert.That(result, Is.InstanceOf<ViewResult>());
        var viewResult = result as ViewResult;
        Assert.That(_controller.ViewBag.ShowResendButton, Is.True);
        Assert.That(_controller.ViewBag.UserId, Is.EqualTo("user1"));
        Assert.That(_controller.ModelState[string.Empty].Errors[0].ErrorMessage, Is.EqualTo("You must confirm your email before logging in."));
    }

    [Test]
    public async Task ResendEmailConfirmation_SendsEmail_AndReturnsView()
    {
        var user = new Users { Id = "user1", Email = "test@test.com" };
        _mockUserManager.Setup(x => x.FindByIdAsync("user1"))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(user))
            .ReturnsAsync("token");
        _mockUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
            .Returns("http://localhost/confirm");

        var result = await _controller.ResendEmailConfirmation("user1");

        Assert.That(result, Is.InstanceOf<ViewResult>());
        var viewResult = result as ViewResult;
        Assert.That(viewResult.ViewName, Is.EqualTo("RegisterConfirmation"));
        _mockEmailService.Verify(x => x.SendEmailAsync("test@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
