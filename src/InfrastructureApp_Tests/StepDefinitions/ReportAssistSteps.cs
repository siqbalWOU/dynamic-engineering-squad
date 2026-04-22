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
    public class ReportAssistSteps : IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private HttpClient _client = null!;
        private HttpResponseMessage _response = null!;
        private readonly ScenarioContext _scenarioContext;

        public ReportAssistSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;

            var dbName = "ReportAssistTest_" + Guid.NewGuid().ToString();

            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registrations
                    var descriptors = services.Where(d =>
                        d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                        d.ServiceType == typeof(ApplicationDbContext) ||
                        d.ServiceType.Name.Contains("DbContextOptions")).ToList();

                    foreach (var d in descriptors) services.Remove(d);

                    // Use InMemory DB
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(dbName);
                    });

                    // Disable antiforgery for testing
                    services.PostConfigure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
                    {
                        options.Filters.Add(new Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryTokenAttribute());
                    });
                });
            });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true
            });
        }

        // ───────────────
        // GIVEN
        // ───────────────

        [Given("I am on the Report Issue page")]
        public async Task GivenIAmOnTheReportIssuePage()
        {
            _response = await _client.GetAsync("/ReportIssue/Create");

            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var html = await _response.Content.ReadAsStringAsync();
            _scenarioContext["PageHtml"] = html;
        }

        // ───────────────
        // WHEN
        // ───────────────

        [When("I type {string} into the report description box")]
        public async Task WhenITypeIntoTheReportDescriptionBox(string input)
        {
            // Simulate calling your autocomplete API directly
            _response = await _client.GetAsync($"/api/reportassist/suggestions?q={input}");

            var content = await _response.Content.ReadAsStringAsync();
            _scenarioContext["SuggestionsResponse"] = content;
        }

        [When("I click the first autocomplete suggestion")]
        public void WhenIClickTheFirstAutocompleteSuggestion()
        {
            // Since this is server-side test (not Selenium),
            // we simulate selection from returned suggestions

            var json = _scenarioContext["SuggestionsResponse"]?.ToString();

            Assert.That(json, Is.Not.Null.And.Not.Empty);

            // VERY SIMPLE PARSE (you can replace with System.Text.Json if needed)
            // Assume format: ["suggestion1","suggestion2"]
            var first = json!
                .Replace("[", "")
                .Replace("]", "")
                .Replace("\"", "")
                .Split(',')
                .FirstOrDefault();

            _scenarioContext["SelectedSuggestion"] = first;
        }

        // ───────────────
        // THEN
        // ───────────────

        [Then("autocomplete suggestions should be displayed")]
        public async Task ThenAutocompleteSuggestionsShouldBeDisplayed()
        {
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var content = await _response.Content.ReadAsStringAsync();

            Assert.That(content, Is.Not.Empty);
            Assert.That(content, Does.Contain("["));
        }

        [Then("autocomplete suggestions should not be displayed")]
        public async Task ThenAutocompleteSuggestionsShouldNotBeDisplayed()
        {
            var content = await _response.Content.ReadAsStringAsync();

            // Expect empty or no results
            Assert.That(content == "[]" || string.IsNullOrWhiteSpace(content), Is.True);
        }

        [Then("no autocomplete suggestions should be returned")]
        public async Task ThenNoAutocompleteSuggestionsShouldBeReturned()
        {
            var content = await _response.Content.ReadAsStringAsync();
            var suggestions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();

            Assert.That(suggestions, Is.Empty);
        }

        [Then("the description box should contain the selected suggestion")]
        public void ThenTheDescriptionBoxShouldContainTheSelectedSuggestion()
        {
            var selected = _scenarioContext["SelectedSuggestion"]?.ToString();

            Assert.That(selected, Is.Not.Null.And.Not.Empty);

            // In API-based test, we verify selection logic
            // (UI behavior is handled separately in JS/Selenium tests)

            Assert.That(selected.Length, Is.GreaterThan(0));
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}