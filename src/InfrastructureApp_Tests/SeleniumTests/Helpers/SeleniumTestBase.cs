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

namespace InfrastructureApp_Tests.SeleniumTests.Helpers
{
    public abstract class SeleniumTestBase
    {
        protected IWebDriver Driver = null!;

        protected static readonly string BaseUrl =
            "http://127.0.0.1:5044";

        protected static IHost? ServerHost;
        protected static SqliteConnection? SqliteConnection;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            if (ServerHost == null)
            {
                SqliteConnection = new SqliteConnection("DataSource=:memory:");
                SqliteConnection.Open();

                var builder = WebApplication.CreateBuilder(new string[] { "--environment", "Testing" });
                
                builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(SqliteConnection));
                
                builder.Services.AddIdentity<Users, IdentityRole>(options => {
                    options.SignIn.RequireConfirmedAccount = true;
                    options.SignIn.RequireConfirmedEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

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
                builder.Services.AddScoped<ITripCheckService, TripCheckService>();
                builder.Services.AddScoped<IContentModerationService, ContentModerationService>();
                builder.Services.AddScoped<IImageHashService, ImageHashService>();
                builder.Services.AddScoped<IReportDescriptionSuggestionService, ReportDescriptionSuggestionService>();
                builder.Services.AddScoped<LeaderboardService>();
                builder.Services.AddHttpContextAccessor();

                builder.Services.Configure<InfrastructureApp.Configuration.GoogleMapsOptions>(options => {
                    options.ApiKey = "fake-key";
                });
                builder.Services.Configure<InfrastructureApp.Configuration.EmailOptions>(options => {
                    options.SenderEmail = "test@example.com";
                });

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
        }

        [SetUp]
        public async Task SetUpDriver()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--window-size=1280,900");

            Driver = new ChromeDriver(options);
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
            await OneTimeTearDownStatic();
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

        protected void Login(string username = "ErinBleu", string password = "Password1234!")
        {
            Driver.Navigate().GoToUrl($"{BaseUrl}/Account/Login");

            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElement(By.Id("UserName")));

            Driver.FindElement(By.Id("UserName")).SendKeys(username);
            Driver.FindElement(By.Id("Password")).SendKeys(password);
            Driver.FindElement(By.CssSelector("input[type='submit']")).Click();

            wait.Until(d => !d.Url.Contains("/Account/Login"));
        }
    }
}
