using System.Net;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using InfrastructureApp.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Moq;
using Reqnroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public class EmailVerificationSteps : IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private HttpClient _client = null!;
        private HttpResponseMessage _response = null!;
        private readonly Mock<IEmailService> _mockEmailService = new();
        private readonly ScenarioContext _scenarioContext;
        private string? _lastEmailSentTo;
        private string? _lastEmailSubject;
        private string? _lastEmailBody;

        public EmailVerificationSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
            var dbName = "EmailVerificationTest_" + Guid.NewGuid().ToString();

            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    var descriptors = services.Where(d => 
                        d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) || 
                        d.ServiceType == typeof(ApplicationDbContext) ||
                        d.ServiceType.Name.Contains("DbContextOptions")).ToList();
                    
                    foreach (var d in descriptors) services.Remove(d);
                    
                    services.AddDbContext<ApplicationDbContext>(options => 
                    {
                        options.UseInMemoryDatabase(dbName);
                    });

                    services.Configure<IdentityOptions>(options =>
                    {
                        options.Password.RequireDigit = false;
                        options.Password.RequiredLength = 1;
                        options.Password.RequireLowercase = false;
                        options.Password.RequireUppercase = false;
                        options.Password.RequireNonAlphanumeric = false;
                        options.SignIn.RequireConfirmedAccount = true;
                    });

                    var mockConfirmation = new Mock<IUserConfirmation<Users>>();
                    mockConfirmation.Setup(c => c.IsConfirmedAsync(It.IsAny<UserManager<Users>>(), It.IsAny<Users>()))
                        .ReturnsAsync((UserManager<Users> um, Users u) => u.EmailConfirmed);
                    services.AddScoped(_ => mockConfirmation.Object);

                    services.AddAntiforgery(options => 
                    {
                        options.HeaderName = "X-XSRF-TOKEN";
                    });
                    
                    var mockAntiforgery = new Mock<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
                    mockAntiforgery.Setup(a => a.GetAndStoreTokens(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
                        .Returns(new Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet("token", "cookie", "field", "header"));
                    mockAntiforgery.Setup(a => a.GetTokens(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
                        .Returns(new Microsoft.AspNetCore.Antiforgery.AntiforgeryTokenSet("token", "cookie", "field", "header"));
                    mockAntiforgery.Setup(a => a.IsRequestValidAsync(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
                        .ReturnsAsync(true);
                    services.AddSingleton(_ => mockAntiforgery.Object);

                    services.PostConfigure<Microsoft.AspNetCore.Mvc.MvcOptions>(options => 
                    {
                        options.Filters.Add(new Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryTokenAttribute());
                    });

                    services.AddScoped(_ => _mockEmailService.Object);

                    _mockEmailService
                        .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                        .Callback<string, string, string>((e, s, b) => 
                        {
                            _lastEmailSentTo = e;
                            _lastEmailSubject = s;
                            _lastEmailBody = b;
                        })
                        .Returns(Task.CompletedTask);
                });
            });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

            using (var scope = _factory.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                if (!roleManager.RoleExistsAsync("User").GetAwaiter().GetResult())
                {
                    roleManager.CreateAsync(new IdentityRole("User")).GetAwaiter().GetResult();
                }
            }
        }

        // ── Givens ──

        [Given("I am on the registration page")]
        public void GivenIAmOnTheRegistrationPage()
        {
        }

        [Given("a user {string} with email {string} exists but is not confirmed")]
        public async Task GivenAUserExistsButIsNotConfirmed(string username, string email)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var user = new Users { UserName = username, Email = email, EmailConfirmed = false };
            await userManager.CreateAsync(user, "Password123!");
        }

        [Given("I am on the login page")]
        public void GivenIAmOnTheLoginPage() { }

        [Given("a user {string} with a valid confirmation token exists")]
        public async Task GivenAUserWithValidTokenExists(string username)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var user = new Users { UserName = username, Email = $"{username}@example.com", EmailConfirmed = false };
            await userManager.CreateAsync(user, "Password123!");
            
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            _scenarioContext["Token"] = token;
            _scenarioContext["UserId"] = user.Id;
        }

        [Given("I attempted to login as {string} and saw the resend button")]
        public async Task GivenIAttemptedToLoginAndSawResend(string username)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("UserName", username),
                new KeyValuePair<string, string>("Password", "Password123!")
            });
            _response = await _client.PostAsync("/Account/Login", content);
            
            if (_response.StatusCode == HttpStatusCode.OK)
            {
                 var html = await _response.Content.ReadAsStringAsync();
                 Assert.That(html, Does.Contain("Resend Verification Email"));
                 var match = System.Text.RegularExpressions.Regex.Match(html, "name=\"userId\" value=\"([^\"]+)\"");
                 _scenarioContext["UserId"] = match.Groups[1].Value;
            }
            else
            {
                Assert.Fail($"Login post failed with status {_response.StatusCode}");
            }
        }

        // ── Whens ──

        [When("I submit a valid registration form with username {string} and email {string}")]
        public async Task WhenISubmitAValidRegistrationForm(string username, string email)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Username", username),
                new KeyValuePair<string, string>("Email", email),
                new KeyValuePair<string, string>("Password", "Password123!"),
                new KeyValuePair<string, string>("ConfirmPassword", "Password123!")
            });
            _response = await _client.PostAsync("/Account/Register", content);
            if (_response.StatusCode == HttpStatusCode.BadRequest)
            {
                var body = await _response.Content.ReadAsStringAsync();
                Console.WriteLine("Registration BadRequest Body: " + body);
            }
        }

        [When("I attempt to login with username {string} and password {string}")]
        public async Task WhenIAttemptToLogin(string username, string password)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("UserName", username),
                new KeyValuePair<string, string>("Password", password)
            });
            _response = await _client.PostAsync("/Account/Login", content);
        }

        [When("I navigate to the confirmation link")]
        public async Task WhenINavigateToConfirmationLink()
        {
            var userId = _scenarioContext["UserId"].ToString();
            var token = _scenarioContext["Token"].ToString();
            _response = await _client.GetAsync($"/Account/ConfirmEmail?userId={userId}&token={WebUtility.UrlEncode(token)}");
        }

        [When("I click the resend verification email button")]
        public async Task WhenIClickResendButton()
        {
            var userId = _scenarioContext["UserId"].ToString();
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("userId", userId)
            });
            _response = await _client.PostAsync("/Account/ResendEmailConfirmation", content);
        }

        // ── Thens ──

        [Then("I should be redirected to the registration confirmation page")]
        public void ThenIShouldBeRedirectedToRegistrationConfirmation()
        {
            Assert.That(_response.RequestMessage?.RequestUri?.ToString(), Does.Contain("RegisterConfirmation"));
        }

        [Then("a confirmation email should be sent to {string}")]
        public void ThenAConfirmationEmailShouldBeSentTo(string email)
        {
            _mockEmailService.Verify(x => x.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
            Assert.That(_lastEmailSentTo, Is.EqualTo(email));
        }

        [Then("I should see an error message {string}")]
        public async Task ThenIShouldSeeErrorMessage(string message)
        {
            var html = await _response.Content.ReadAsStringAsync();
            Assert.That(html, Does.Contain(message));
        }

        [Then("I should see a button to resend the verification email")]
        public async Task ThenIShouldSeeResendButton()
        {
            var html = await _response.Content.ReadAsStringAsync();
            Assert.That(html, Does.Contain("Resend Verification Email"));
        }

        [Then("I should see the email confirmed success message")]
        public async Task ThenIShouldSeeSuccessMessage()
        {
            var html = await _response.Content.ReadAsStringAsync();
            Assert.That(html, Does.Contain("Email Confirmed!"));
        }

        [Then("the user {string} should be marked as confirmed in the database")]
        public async Task ThenUserShouldBeMarkedAsConfirmed(string username)
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var user = await userManager.FindByNameAsync(username);
            Assert.That(user!.EmailConfirmed, Is.True);
        }

        [Then("I should see a message {string}")]
        public async Task ThenIShouldSeeMessage(string message)
        {
            var html = await _response.Content.ReadAsStringAsync();
            Assert.That(html, Does.Contain(message));
        }

        [Then("a new confirmation email should be sent to {string}")]
        public void ThenANewConfirmationEmailShouldBeSentTo(string email)
        {
            _mockEmailService.Verify(x => x.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
