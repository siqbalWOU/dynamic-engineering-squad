using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp_Tests.Account;
using InfrastructureApp_Tests.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Reqnroll;

namespace InfrastructureApp_Tests.StepDefinitions;

[Binding]
public class SocialSharingSteps : IDisposable
{
    private readonly WebApplicationFactory<Program> _authFactory;
    private readonly WebApplicationFactory<Program> _unauthFactory;
    private readonly HttpClient _authClient;
    private readonly HttpClient _unauthClient;
    private HttpResponseMessage _response = null!;
    private string _html = string.Empty;
    private int _authReportId;
    private int _unauthReportId;

    public SocialSharingSteps()
    {
        var authDb = "SocialShareAuth_" + Guid.NewGuid();
        var unauthDb = "SocialShareUnauth_" + Guid.NewGuid();

        _authFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                ReplaceDatabase(services, authDb);
                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "TestScheme";
                    options.DefaultAuthenticateScheme = "TestScheme";
                    options.DefaultChallengeScheme = "TestScheme";
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });
            });
        });

        _unauthFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                ReplaceDatabase(services, unauthDb);
            });
        });

        _authClient = _authFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
        _unauthClient = _unauthFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
    }

    private static void ReplaceDatabase(IServiceCollection services, string dbName)
    {
        var descriptors = services.Where(d =>
            d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
            d.ServiceType == typeof(ApplicationDbContext) ||
            d.ServiceType.Name.Contains("DbContextOptions")).ToList();

        foreach (var d in descriptors)
            services.Remove(d);

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName)
                   .ConfigureWarnings(w => w.Ignore(
                       Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
    }

    private static async Task<int> SeedReport(WebApplicationFactory<Program> factory, string description, string status)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var report = await ReportIssueTestDataHelper.CreateTestReportAsync(
            db,
            description,
            status,
            "test-user-id");
        return report.Id;
    }

    [Given("an approved sharing report exists with description {string}")]
    public async Task GivenAnApprovedSharingReportExistsWithDescription(string description)
    {
        _authReportId = await SeedReport(_authFactory, description, "Approved");
        _unauthReportId = await SeedReport(_unauthFactory, description, "Approved");
    }

    [Given("a pending sharing report exists with description {string}")]
    public async Task GivenAPendingSharingReportExistsWithDescription(string description)
    {
        _authReportId = await SeedReport(_authFactory, description, "Pending");
        _unauthReportId = await SeedReport(_unauthFactory, description, "Pending");
    }

    [When("an authenticated user navigates to the sharing report details page")]
    public async Task WhenAnAuthenticatedUserNavigatesToTheSharingReportDetailsPage()
    {
        _response = await _authClient.GetAsync($"/ReportIssue/Details/{_authReportId}");
        TestContext.WriteLine($"GET /ReportIssue/Details/{_authReportId} => {(int)_response.StatusCode} {_response.StatusCode}");
        _html = await _response.Content.ReadAsStringAsync();
    }

    [When("an unauthenticated user navigates to the sharing report details page")]
    public async Task WhenAnUnauthenticatedUserNavigatesToTheSharingReportDetailsPage()
    {
        _response = await _unauthClient.GetAsync($"/ReportIssue/Details/{_unauthReportId}");
        TestContext.WriteLine($"GET /ReportIssue/Details/{_unauthReportId} => {(int)_response.StatusCode} {_response.StatusCode}");
        _html = await _response.Content.ReadAsStringAsync();
    }

    [Then("the sharing page should contain {string}")]
    public void ThenTheSharingPageShouldContain(string expected)
    {
        Assert.That(_response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK), $"Unexpected status code before asserting page content. Body: {_html}");
        Assert.That(_html, Does.Contain(expected));
    }

    [Then("the sharing page should not contain {string}")]
    public void ThenTheSharingPageShouldNotContain(string unexpected)
    {
        Assert.That(_response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK), $"Unexpected status code before asserting page content. Body: {_html}");
        Assert.That(_html, Does.Not.Contain(unexpected));
    }

    public void Dispose()
    {
        _authClient.Dispose();
        _unauthClient.Dispose();
        _authFactory.Dispose();
        _unauthFactory.Dispose();
    }
}
