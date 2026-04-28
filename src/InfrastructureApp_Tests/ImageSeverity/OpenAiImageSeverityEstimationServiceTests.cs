using System;
using System.Collections.Generic;
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
    public class OpenAiImageSeverityEstimationServiceTests
    {
        [Test]
        public async Task EstimateSeverityAsync_WhenImageSourceIsEmpty_ReturnsFailed()
        {
            var handler = new FakeHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

            var httpClient = new HttpClient(handler);
            var config = MakeConfig("test-api-key", "gpt-4.1-mini");
            var service = new OpenAiImageSeverityEstimationService(httpClient, config);

            var result = await service.EstimateSeverityAsync("");

            Assert.That(result.Performed, Is.False);
            Assert.That(result.SeverityStatus, Is.EqualTo(ImageSeverityStatuses.Pending));
            Assert.That(result.Reason, Is.EqualTo("Image source was empty."));
        }

        [Test]
        public async Task EstimateSeverityAsync_WhenApiKeyMissing_ReturnsFailed()
        {
            var handler = new FakeHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

            var httpClient = new HttpClient(handler);
            var config = MakeConfig(null, "gpt-4.1-mini");
            var service = new OpenAiImageSeverityEstimationService(httpClient, config);

            var result = await service.EstimateSeverityAsync("data:image/png;base64,abc123");

            Assert.That(result.Performed, Is.False);
            Assert.That(result.SeverityStatus, Is.EqualTo(ImageSeverityStatuses.Pending));
            Assert.That(result.Reason, Is.EqualTo("OpenAI API key is missing."));
        }

        [Test]
        public async Task EstimateSeverityAsync_WhenApiReturnsNonSuccess_ReturnsFailed()
        {
            var handler = new FakeHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\":\"bad request\"}")
                }));

            var httpClient = new HttpClient(handler);
            var config = MakeConfig("test-api-key", "gpt-4.1-mini");
            var service = new OpenAiImageSeverityEstimationService(httpClient, config);

            var result = await service.EstimateSeverityAsync("data:image/png;base64,abc123");

            Assert.That(result.Performed, Is.False);
            Assert.That(result.SeverityStatus, Is.EqualTo(ImageSeverityStatuses.Pending));
            Assert.That(result.Reason, Does.Contain("Severity API returned 400"));
        }

        [Test]
        public async Task EstimateSeverityAsync_WhenOutputArrayMissing_ReturnsFailed()
        {
            var json = """
            {
              "id": "resp_123"
            }
            """;

            var handler = new FakeHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                }));

            var httpClient = new HttpClient(handler);
            var config = MakeConfig("test-api-key", "gpt-4.1-mini");
            var service = new OpenAiImageSeverityEstimationService(httpClient, config);

            var result = await service.EstimateSeverityAsync("data:image/png;base64,abc123");

            Assert.That(result.Performed, Is.False);
            Assert.That(result.Reason, Does.Contain("No output array was returned"));
        }

        [Test]
        public async Task EstimateSeverityAsync_WhenContentArrayMissing_ReturnsFailed()
        {
            var json = """
            {
              "output": [
                {
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
            var config = MakeConfig("test-api-key", "gpt-4.1-mini");
            var service = new OpenAiImageSeverityEstimationService(httpClient, config);

            var result = await service.EstimateSeverityAsync("data:image/png;base64,abc123");

            Assert.That(result.Performed, Is.False);
            Assert.That(result.Reason, Does.Contain("No content array was returned"));
        }

        [Test]
        public async Task EstimateSeverityAsync_WhenTextMissing_ReturnsFailed()
        {
            var json = """
            {
              "output": [
                {
                  "content": [
                    {
                      "type": "output_text"
                    }
                  ]
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
            var config = MakeConfig("test-api-key", "gpt-4.1-mini");
            var service = new OpenAiImageSeverityEstimationService(httpClient, config);

            var result = await service.EstimateSeverityAsync("data:image/png;base64,abc123");

            Assert.That(result.Performed, Is.False);
            Assert.That(result.Reason, Does.Contain("No text field was returned"));
        }

        [Test]
        public async Task EstimateSeverityAsync_WhenStructuredOutputTextEmpty_ReturnsFailed()
        {
            var json = """
            {
              "output": [
                {
                  "content": [
                    {
                      "text": ""
                    }
                  ]
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
            var config = MakeConfig("test-api-key", "gpt-4.1-mini");
            var service = new OpenAiImageSeverityEstimationService(httpClient, config);

            var result = await service.EstimateSeverityAsync("data:image/png;base64,abc123");

            Assert.That(result.Performed, Is.False);
            Assert.That(result.Reason, Does.Contain("Structured output text was empty"));
        }

        [Test]
        public async Task EstimateSeverityAsync_WhenSeverityIsInvalid_ReturnsFailed()
        {
            var nestedOutput = """
            {"severityStatus":"Extreme","reason":"Too severe"}
            """;

            var json = $$"""
            {
              "output": [
                {
                  "content": [
                    {
                      "text": {{System.Text.Json.JsonSerializer.Serialize(nestedOutput)}}
                    }
                  ]
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
            var config = MakeConfig("test-api-key", "gpt-4.1-mini");
            var service = new OpenAiImageSeverityEstimationService(httpClient, config);

            var result = await service.EstimateSeverityAsync("data:image/png;base64,abc123");

            Assert.That(result.Performed, Is.False);
            Assert.That(result.Reason, Does.Contain("invalid severity"));
        }

        [Test]
        public async Task EstimateSeverityAsync_WhenResponseIsValid_ReturnsSuccess()
        {
            var nestedOutput = """
            {"severityStatus":"High","reason":"Large pothole with deep cracking"}
            """;

            var json = $$"""
            {
              "output": [
                {
                  "content": [
                    {
                      "text": {{System.Text.Json.JsonSerializer.Serialize(nestedOutput)}}
                    }
                  ]
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
            var config = MakeConfig("test-api-key", "gpt-4.1-mini");
            var service = new OpenAiImageSeverityEstimationService(httpClient, config);

            var result = await service.EstimateSeverityAsync("data:image/png;base64,abc123");

            Assert.That(result.Performed, Is.True);
            Assert.That(result.SeverityStatus, Is.EqualTo(ImageSeverityStatuses.High));
            Assert.That(result.Reason, Is.EqualTo("Large pothole with deep cracking"));
        }

        [Test]
        public async Task EstimateSeverityAsync_WhenHttpClientThrows_ReturnsFailed()
        {
            var handler = new FakeHttpMessageHandler((_, _) =>
                throw new HttpRequestException("Network failure"));

            var httpClient = new HttpClient(handler);
            var config = MakeConfig("test-api-key", "gpt-4.1-mini");
            var service = new OpenAiImageSeverityEstimationService(httpClient, config);

            var result = await service.EstimateSeverityAsync("data:image/png;base64,abc123");

            Assert.That(result.Performed, Is.False);
            Assert.That(result.SeverityStatus, Is.EqualTo(ImageSeverityStatuses.Pending));
            Assert.That(result.Reason, Does.Contain("Network failure"));
        }

        [Test]
        public async Task EstimateSeverityAsync_SendsBearerHeader_AndExpectedRequestBody()
        {
            HttpRequestMessage? capturedRequest = null;
            string? capturedBody = null;

            var nestedOutput = """
            {"severityStatus":"Medium","reason":"Visible crack requiring scheduled repair"}
            """;

            var json = $$"""
            {
            "output": [
                {
                "content": [
                    {
                    "text": {{System.Text.Json.JsonSerializer.Serialize(nestedOutput)}}
                    }
                ]
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
            var config = MakeConfig("test-api-key", "custom-severity-model");
            var service = new OpenAiImageSeverityEstimationService(httpClient, config);

            await service.EstimateSeverityAsync("data:image/png;base64,abc123");

            Assert.That(capturedRequest, Is.Not.Null);
            Assert.That(capturedRequest!.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(capturedRequest.RequestUri!.ToString(), Is.EqualTo("https://api.openai.com/v1/responses"));

            Assert.That(capturedRequest.Headers.Authorization, Is.Not.Null);
            Assert.That(capturedRequest.Headers.Authorization!.Scheme, Is.EqualTo("Bearer"));
            Assert.That(capturedRequest.Headers.Authorization.Parameter, Is.EqualTo("test-api-key"));

            Assert.That(capturedBody, Is.Not.Null);
            Assert.That(capturedBody, Does.Contain("\"model\":\"custom-severity-model\""));
            Assert.That(capturedBody, Does.Contain("\"type\":\"input_text\""));
            Assert.That(capturedBody, Does.Contain("\"type\":\"input_image\""));
            Assert.That(capturedBody, Does.Contain("\"image_url\":\"data:image/png;base64,abc123\""));
            Assert.That(capturedBody, Does.Contain("\"severityStatus\""));
            Assert.That(capturedBody, Does.Contain("\"Critical\""));
        }

        [Test]
        public async Task EstimateSeverityAsync_WhenModelMissing_UsesDefaultModel()
        {
            string? capturedBody = null;

            var nestedOutput = """
            {"severityStatus":"Low","reason":"Minor cosmetic issue"}
            """;

            var json = $$"""
            {
            "output": [
                {
                "content": [
                    {
                    "text": {{System.Text.Json.JsonSerializer.Serialize(nestedOutput)}}
                    }
                ]
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
            var config = MakeConfig("test-api-key", null);
            var service = new OpenAiImageSeverityEstimationService(httpClient, config);

            await service.EstimateSeverityAsync("data:image/png;base64,abc123");

            Assert.That(capturedBody, Is.Not.Null);
            Assert.That(capturedBody, Does.Contain("\"model\":\"gpt-4.1-mini\""));
        }

        private static IConfiguration MakeConfig(string? apiKey, string? model)
        {
            var values = new[]
            {
                new KeyValuePair<string, string?>("OpenAIModerationAPIkey", apiKey),
                new KeyValuePair<string, string?>("OpenAI:SeverityModel", model)
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