using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using InfrastructureApp.Services.ImageSeverity;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Services.ImageSeverity
{
    [TestFixture]
    public class OpenAiImageModerationServiceTests
    {
        [Test]
        public async Task ModerateImageAsync_WhenImageSourceIsEmpty_ReturnsFailed()
        {
            var handler = new FakeHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

            var httpClient = new HttpClient(handler);
            var config = MakeConfig(
                apiKey: "test-api-key",
                model: "omni-moderation-latest");

            var service = new OpenAiImageModerationService(httpClient, config);

            var result = await service.ModerateImageAsync("");

            Assert.That(result.Performed, Is.False);
            Assert.That(result.IsViable, Is.False);
            Assert.That(result.Reason, Is.EqualTo("Image source was empty."));
        }

        [Test]
        public async Task ModerateImageAsync_WhenApiKeyIsMissing_ReturnsFailed()
        {
            var handler = new FakeHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

            var httpClient = new HttpClient(handler);
            var config = MakeConfig(
                apiKey: null,
                model: "omni-moderation-latest");

            var service = new OpenAiImageModerationService(httpClient, config);

            var result = await service.ModerateImageAsync("data:image/png;base64,abc123");

            Assert.That(result.Performed, Is.False);
            Assert.That(result.IsViable, Is.False);
            Assert.That(result.Reason, Is.EqualTo("OpenAI API key is missing."));
        }

        [Test]
        public async Task ModerateImageAsync_WhenApiReturnsNonSuccessStatus_ReturnsFailed()
        {
            var handler = new FakeHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\":\"bad request\"}")
                }));

            var httpClient = new HttpClient(handler);
            var config = MakeConfig(
                apiKey: "test-api-key",
                model: "omni-moderation-latest");

            var service = new OpenAiImageModerationService(httpClient, config);

            var result = await service.ModerateImageAsync("data:image/png;base64,abc123");

            Assert.That(result.Performed, Is.False);
            Assert.That(result.IsViable, Is.False);
            Assert.That(result.Reason, Does.Contain("Moderation API returned 400"));
        }

        [Test]
        public async Task ModerateImageAsync_WhenResponseHasNoResults_ReturnsFailed()
        {
            var handler = new FakeHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"results\":[]}")
                }));

            var httpClient = new HttpClient(handler);
            var config = MakeConfig(
                apiKey: "test-api-key",
                model: "omni-moderation-latest");

            var service = new OpenAiImageModerationService(httpClient, config);

            var result = await service.ModerateImageAsync("data:image/png;base64,abc123");

            Assert.That(result.Performed, Is.False);
            Assert.That(result.IsViable, Is.False);
            Assert.That(result.Reason, Is.EqualTo("Moderation API returned no results."));
        }

        [Test]
        public async Task ModerateImageAsync_WhenImageIsFlagged_ReturnsRejected()
        {
            var json = """
                       {
                         "results": [
                           {
                             "flagged": true
                           }
                         ]
                       }
                       """;

            var handler = new FakeHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                }));

            var httpClient = new HttpClient(handler);
            var config = MakeConfig(
                apiKey: "test-api-key",
                model: "omni-moderation-latest");

            var service = new OpenAiImageModerationService(httpClient, config);

            var result = await service.ModerateImageAsync("data:image/png;base64,abc123");

            Assert.That(result.Performed, Is.True);
            Assert.That(result.IsViable, Is.False);
            Assert.That(result.Reason, Is.EqualTo("Image was flagged by moderation."));
        }

        [Test]
        public async Task ModerateImageAsync_WhenImageIsNotFlagged_ReturnsPassed()
        {
            var json = """
                       {
                         "results": [
                           {
                             "flagged": false
                           }
                         ]
                       }
                       """;

            var handler = new FakeHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                }));

            var httpClient = new HttpClient(handler);
            var config = MakeConfig(
                apiKey: "test-api-key",
                model: "omni-moderation-latest");

            var service = new OpenAiImageModerationService(httpClient, config);

            var result = await service.ModerateImageAsync("data:image/png;base64,abc123");

            Assert.That(result.Performed, Is.True);
            Assert.That(result.IsViable, Is.True);
            Assert.That(result.Reason, Is.Null);
        }

        [Test]
        public async Task ModerateImageAsync_WhenHttpClientThrows_ReturnsFailed()
        {
            var handler = new FakeHttpMessageHandler((_, _) =>
                throw new HttpRequestException("Network failure"));

            var httpClient = new HttpClient(handler);
            var config = MakeConfig(
                apiKey: "test-api-key",
                model: "omni-moderation-latest");

            var service = new OpenAiImageModerationService(httpClient, config);

            var result = await service.ModerateImageAsync("data:image/png;base64,abc123");

            Assert.That(result.Performed, Is.False);
            Assert.That(result.IsViable, Is.False);
            Assert.That(result.Reason, Does.Contain("Network failure"));
        }

        [Test]
        public async Task ModerateImageAsync_SendsBearerToken_AndExpectedRequestBody()
        {
            HttpRequestMessage? capturedRequest = null;
            string? capturedBody = null;

            var json = """
                    {
                        "results": [
                        {
                            "flagged": false
                        }
                        ]
                    }
                    """;

            var handler = new FakeHttpMessageHandler(async (request, _) =>
            {
                capturedRequest = request;
                capturedBody = await request.Content!.ReadAsStringAsync();

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                };
            });

            var httpClient = new HttpClient(handler);
            var config = MakeConfig(
                apiKey: "test-api-key",
                model: "custom-model");

            var service = new OpenAiImageModerationService(httpClient, config);

            await service.ModerateImageAsync("data:image/png;base64,abc123");

            Assert.That(capturedRequest, Is.Not.Null);
            Assert.That(capturedRequest!.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(capturedRequest.RequestUri!.ToString(), Is.EqualTo("https://api.openai.com/v1/moderations"));

            Assert.That(capturedRequest.Headers.Authorization, Is.Not.Null);
            Assert.That(capturedRequest.Headers.Authorization!.Scheme, Is.EqualTo("Bearer"));
            Assert.That(capturedRequest.Headers.Authorization.Parameter, Is.EqualTo("test-api-key"));

            Assert.That(capturedBody, Is.Not.Null);
            Assert.That(capturedBody, Does.Contain("\"model\":\"custom-model\""));
            Assert.That(capturedBody, Does.Contain("\"type\":\"image_url\""));
            Assert.That(capturedBody, Does.Contain("\"url\":\"data:image/png;base64,abc123\""));
        }

        [Test]
        public async Task ModerateImageAsync_WhenModelMissing_UsesDefaultModel()
        {
            string? capturedBody = null;

            var json = """
                    {
                        "results": [
                        {
                            "flagged": false
                        }
                        ]
                    }
                    """;

            var handler = new FakeHttpMessageHandler(async (request, _) =>
            {
                capturedBody = await request.Content!.ReadAsStringAsync();

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                };
            });

            var httpClient = new HttpClient(handler);
            var config = MakeConfig(
                apiKey: "test-api-key",
                model: null);

            var service = new OpenAiImageModerationService(httpClient, config);

            await service.ModerateImageAsync("data:image/png;base64,abc123");

            Assert.That(capturedBody, Is.Not.Null);
            Assert.That(capturedBody, Does.Contain("\"model\":\"omni-moderation-latest\""));
        }

        private static IConfiguration MakeConfig(string? apiKey, string? model)
        {
            var values = new[]
            {
                new KeyValuePair<string, string?>("OpenAIModerationAPIkey", apiKey),
                new KeyValuePair<string, string?>("OpenAI:ModerationModel", model)
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }

        private sealed class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

            public FakeHttpMessageHandler(
                Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return _handler(request, cancellationToken);
            }
        }
    }
}