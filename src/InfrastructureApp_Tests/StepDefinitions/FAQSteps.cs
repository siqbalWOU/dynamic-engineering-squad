using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace InfrastructureApp_Tests.StepDefinitions
{
    [Binding]
    public class FAQSteps : IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private HttpClient _client = null!;
        private HttpResponseMessage _response = null!;
        private string _html = string.Empty;

        public FAQSteps()
        {
            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                builder.ConfigureServices(services =>
                {
                    var descriptors = services.Where(d =>
                        d.ServiceType == typeof(DbContextOptions<InfrastructureApp.Data.ApplicationDbContext>) ||
                        d.ServiceType == typeof(InfrastructureApp.Data.ApplicationDbContext) ||
                        d.ServiceType.Name.Contains("DbContextOptions")).ToList();

                    foreach (var d in descriptors)
                        services.Remove(d);

                    services.AddDbContext<InfrastructureApp.Data.ApplicationDbContext>(options =>
                        options.UseInMemoryDatabase("FAQTest_" + Guid.NewGuid()));
                });
            });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true
            });
        }

        [Given("I navigate to the FAQ page")]
        public async Task GivenINavigateToTheFAQPage()
        {
            _response = await _client.GetAsync("/Home/FAQ");
            _html = await _response.Content.ReadAsStringAsync();
        }

        [Given("I navigate to the Home page")]
        public async Task GivenINavigateToTheHomePage()
        {
            _response = await _client.GetAsync("/");
            _html = await _response.Content.ReadAsStringAsync();
        }

        [Then("the response should be 200 OK")]
        public void ThenTheResponseShouldBe200OK()
        {
            Assert.That(_response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Then("the page should contain {string}")]
        public void ThenThePageShouldContain(string expectedText)
        {
            Assert.That(_html, Does.Contain(expectedText));
        }

        [Then("the page should not contain {string}")]
        public void ThenThePageShouldNotContain(string unexpectedText)
        {
            Assert.That(_html, Does.Not.Contain(unexpectedText));
        }

        [Then("the footer should contain a link to the FAQ page")]
        public void ThenTheFooterShouldContainALinkToTheFAQPage()
        {
            Assert.That(_html, Does.Contain("/Home/FAQ"));
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}
