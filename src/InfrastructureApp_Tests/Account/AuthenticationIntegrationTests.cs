using System.Net;
using InfrastructureApp_Tests.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace InfrastructureApp_Tests.Account;

[TestFixture]
public class AuthenticationIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private HttpClient _authedClient;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        
        _authedClient = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services
                    .AddAuthentication(defaultScheme: "TestScheme")
                    .AddScheme<AuthenticationSchemeOptions,
                        TestAuthHandler>("TestScheme",
                        options => { });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions());
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
        _authedClient.Dispose();
    }

    [Test]
    public async Task
        UnauthedUser_AccessingDashboard_RedirectsToLogin()
    {
        var protectedUrl = "/Dashboard";
        
        var response = await _client.GetAsync(protectedUrl);
        Assert.That(response.StatusCode,
            Is.EqualTo(HttpStatusCode.Redirect));

        var redirectLoc = response.Headers.Location?.ToString();
        Assert.That(redirectLoc,
            Does.Contain("/Login"));
    }
    
    [Test]
    public async Task
        UnauthedUser_AccessingReportIssues_RedirectsToLogin()
    {
        var protectedUrl = "/ReportIssue/ReportIssue";
        
        var response = await _client.GetAsync(protectedUrl);
        Assert.That(response.StatusCode,
            Is.EqualTo(HttpStatusCode.Redirect));

        var redirectLoc = response.Headers.Location?.ToString();
        Assert.That(redirectLoc,
            Does.Contain("/Login"));
    }
    
    [Test]
    public async Task
        UnauthedUser_AccessingCreateReportIssues_RedirectsToLogin()
    {
        var protectedUrl = "/ReportIssue/Create";
        
        var response = await _client.GetAsync(protectedUrl);
        Assert.That(response.StatusCode,
            Is.EqualTo(HttpStatusCode.Redirect));

        var redirectLoc = response.Headers.Location?.ToString();
        Assert.That(redirectLoc,
            Does.Contain("/Login"));
    }

    // Test removed due to team discussion deciding details should be publicly accessible.
    // [Test]
    // public async Task
    //     UnauthedUser_AccessingReportIssuesDetails_RedirectsToLogin()
    // {
    //     var protectedUrl = "/ReportIssue/Details/1";
    //     
    //     var response = await _client.GetAsync(protectedUrl);
    //     Assert.That(response.StatusCode,
    //         Is.EqualTo(HttpStatusCode.Redirect));
    //
    //     var redirectLoc = response.Headers.Location?.ToString();
    //     Assert.That(redirectLoc,
    //         Does.Contain("/Login"));
    // }

    [Test]
    public async Task AuthedUser_AccessingDashboard_ReturnsOk()
    {
        var protectedUrl = "/Dashboard";

        var response = await _authedClient.GetAsync(protectedUrl);
        
        Assert.That(response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }
    
    [Test]
    public async Task AuthedUser_AccessingReportIssue_ReturnsOk()
    {
        var protectedUrl = "/ReportIssue/ReportIssue";

        var response = await _authedClient.GetAsync(protectedUrl);
        
        Assert.That(response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }
    
    [Test]
    public async Task AuthedUser_AccessingCreateReportIssue_ReturnsOk()
    {
        var protectedUrl = "/ReportIssue/Create";

        var response = await _authedClient.GetAsync(protectedUrl);
        
        Assert.That(response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }
    
    // [Test]
    // public async Task AuthedUser_AccessingReportIssueDetails_ReturnsOk()
    // {
    //     var protectedUrl = "/ReportIssue/Details/1";
    //
    //     var response = await _authedClient.GetAsync(protectedUrl);
    //     
    //     Assert.That(response.StatusCode,
    //         Is.EqualTo(HttpStatusCode.OK));
    // }

    [Test]
    public async Task
        UnAuthedUser_PostToReportIssues_RedirectsToLogin()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            "/ReportIssue/Create");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            {"Description", "Test"},
            {"Photo",null},
            {"Latitude", "0"},
            {"Longitude", "0"}
        });
        
        var response = await _client.SendAsync(request);
        Assert.That(response.StatusCode,
            Is.EqualTo(HttpStatusCode.Redirect));
        
        var redirectLoc = response.Headers.Location?.ToString();
        Assert.That(redirectLoc,
            Does.Contain("/Login"));
    }
}