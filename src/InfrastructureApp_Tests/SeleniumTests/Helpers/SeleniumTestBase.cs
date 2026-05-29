using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using NUnit.Framework;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using InfrastructureApp.Models;
using InfrastructureApp.Data;
using InfrastructureApp.Services;
using InfrastructureApp.Services.ContentModeration;
using InfrastructureApp.Services.ImageHashing;
using InfrastructureApp.Services.ReportAssist;
using InfrastructureApp.Services.ImageSeverity;
using InfrastructureApp_Tests.TestDoubles;
using System.Net;
using System.Net.Sockets;
#pragma warning disable NUnit1032

namespace InfrastructureApp_Tests.SeleniumTests.Helpers
{
    [NonParallelizable]
    public abstract class SeleniumTestBase
    {
        protected static IWebDriver Driver = null!;

        protected static string BaseUrl { get; private set; } = string.Empty;

        protected static IHost? ServerHost;
        protected static SqliteConnection? SqliteConnection;
        private static readonly SemaphoreSlim ServerStartLock = new(1, 1);

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            if (ServerHost != null)
            {
                return;
            }

            await ServerStartLock.WaitAsync();
            try
            {
                if (ServerHost != null)
                {
                    return;
                }

                SqliteConnection = new SqliteConnection("DataSource=:memory:");
                SqliteConnection.Open();

                var contentRoot = FindInfrastructureAppContentRoot();
                var builder = WebApplication.CreateBuilder(new WebApplicationOptions
                {
                    Args = new string[] { "--environment", "Testing" },
                    ContentRootPath = contentRoot
                });
                
                builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(SqliteConnection));
                
                builder.Services.AddIdentity<Users, IdentityRole>(options => {
                    options.SignIn.RequireConfirmedAccount = true;
                    options.SignIn.RequireConfirmedEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

                builder.Services.Configure<SecurityStampValidatorOptions>(options =>
                {
                    options.ValidationInterval = TimeSpan.Zero;
                });

                builder.Services.AddControllersWithViews().AddApplicationPart(typeof(Program).Assembly);
                
                builder.Services.Configure<InfrastructureApp.Services.TripCheckOptions>(options => {
                    options.BaseUrl = "https://tripcheck.com";
                    options.CacheMinutes = 5;
                });

                builder.Services.AddHttpClient<ITripCheckService, TripCheckService>();
                builder.Services.AddScoped<IEmailService, AzureEmailService>();
                builder.Services.AddScoped<IAvatarService, AvatarService>();
                builder.Services.AddScoped<IDashboardRepository, DashboardRepositoryEf>();
                builder.Services.AddScoped<IReportIssueRepository, ReportIssueRepositoryEf>();
                builder.Services.AddScoped<IReportIssueService, ReportIssueService>();
                builder.Services.AddScoped<IGeocodingService, GeocodingService>();
                builder.Services.AddScoped<INearbyIssueService, NearbyIssueService>();
                builder.Services.AddScoped<ILeaderboardRepository, LeaderboardRepositoryEf>();
                builder.Services.AddScoped<IUserService, UserService>();
                builder.Services.AddScoped<IVoteService, VoteService>();
                builder.Services.AddScoped<IIssueNameService, IssueNameService>();
                builder.Services.AddScoped<IVerifyFixService, VerifyFixService>();
                builder.Services.AddScoped<IFlagService, FlagService>();
                builder.Services.AddScoped<IPointsShopService, PointsShopService>();
                builder.Services.AddScoped<IAuditLogService, AuditLogService>();
                builder.Services.AddScoped<IStatusHistoryService, StatusHistoryService>();
                builder.Services.AddScoped<IModerationService, ModerationService>();
                builder.Services.AddScoped<ITripCheckService, TripCheckService>();
                builder.Services.AddScoped<IContentModerationService, ContentModerationService>();
                builder.Services.AddScoped<IImageHashService, ImageHashService>();
                builder.Services.AddScoped<IReportDescriptionSuggestionService, ReportDescriptionSuggestionService>();
                builder.Services.AddScoped<LeaderboardService>();
                builder.Services.AddScoped<IImageModerationService, FakeImageModerationService>();
                builder.Services.AddScoped<IImageSeverityEstimationService, FakeImageSeverityEstimationService>();
                builder.Services.AddHttpContextAccessor();

                builder.Services.Configure<InfrastructureApp.Configuration.GoogleMapsOptions>(options => {
                    options.ApiKey = "fake-key";
                });
                builder.Services.Configure<InfrastructureApp.Configuration.EmailOptions>(options => {
                    options.SenderEmail = "test@example.com";
                });

                BaseUrl = $"http://127.0.0.1:{GetFreeTcpPort()}";
                builder.WebHost.UseUrls(BaseUrl);
                builder.WebHost.UseKestrel();

                var app = builder.Build();

                app.UseStaticFiles();
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                using (var scope = app.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    db.Database.EnsureCreated();
                }

                ServerHost = app;
                await ServerHost.StartAsync();
            }
            finally
            {
                ServerStartLock.Release();
            }
        }

        [SetUp]
        public async Task SetUpDriver()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless=new");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument($"--user-data-dir={Path.Combine(Path.GetTempPath(), "chrome-test-" + Guid.NewGuid())}");

            Driver = new ChromeDriver(ChromeDriverService.CreateDefaultService(), options, TimeSpan.FromSeconds(60));
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

            // Create default test user for all tests
            await CreateTestUser("ErinBleu", "Password1234!");
        }

        protected async Task CreateTestUser(string username, string password)
        {
            using var scope = ServerHost!.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Users>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var existing = await userManager.FindByNameAsync(username);
            if (existing == null)
            {
                var user = new Users { UserName = username, Email = $"{username}@example.com", EmailConfirmed = true };
                await userManager.CreateAsync(user, password);
                db.UserPoints.Add(new UserPoints { UserId = user.Id, CurrentPoints = 0, LifetimePoints = 0 });
                await db.SaveChangesAsync();
            }
        }

        [TearDown]
        public void TearDownDriver()
        {
            try
            {
                Driver?.Quit();
                Driver?.Dispose();
            }
            catch { }
            finally
            {
                Driver = null!;
            }
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await Task.CompletedTask;
        }

        public static async Task OneTimeTearDownStatic()
        {
            if (ServerHost != null)
            {
                await ServerHost.StopAsync();
                ServerHost.Dispose();
                ServerHost = null;
            }
            SqliteConnection?.Dispose();
            SqliteConnection = null;
        }

        protected void ScrollAndClick(IWebElement element)
        {
            try
            {
                ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView({ block: 'center', inline: 'nearest' });", element);
                Thread.Sleep(500); // Wait for scroll to finish
                element.Click();
            }
            catch (ElementClickInterceptedException)
            {
                ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].click();", element);
            }
        }

        protected void Login(string username = "ErinBleu", string password = "Password1234!")
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Account/Login");

            var usernameInput = WaitForVisible(By.CssSelector("[data-testid='login-username']"));
            var passwordInput = WaitForVisible(By.CssSelector("[data-testid='login-password']"));
            var submitButton = WaitForClickable(By.CssSelector("[data-testid='login-submit']"));

            usernameInput.Clear();
            usernameInput.SendKeys(username);
            passwordInput.Clear();
            passwordInput.SendKeys(password);
            submitButton.Click();

            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => !d.Url.Contains("/Account/Login"));
        }

        protected IWebElement WaitForVisible(By by, int seconds = 10)
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(seconds));
            wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));

            return wait.Until(driver =>
            {
                var element = driver.FindElement(by);
                return element.Displayed ? element : null;
            })!;
        }

        protected IWebElement WaitForClickable(By by, int seconds = 10)
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(seconds));
            wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));

            return wait.Until(driver =>
            {
                var element = driver.FindElement(by);
                return element.Displayed && element.Enabled ? element : null;
            })!;
        }

        private static string FindInfrastructureAppContentRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);

            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, "src", "InfrastructureApp");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException(
                $"Could not locate the InfrastructureApp content root from '{AppContext.BaseDirectory}'.");
        }

        private static int GetFreeTcpPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
#pragma warning restore NUnit1032
