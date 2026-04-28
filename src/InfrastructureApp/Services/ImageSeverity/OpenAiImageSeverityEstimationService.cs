using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace InfrastructureApp.Services.ImageSeverity
{
    public sealed class OpenAiImageSeverityEstimationService : IImageSeverityEstimationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public OpenAiImageSeverityEstimationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<SeverityEstimationResult> EstimateSeverityAsync(string imageSource, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(imageSource))
                return SeverityEstimationResult.Failed("Image source was empty.");

            var apiKey = _configuration["OpenAIModerationAPIkey"];
            var model = _configuration["OpenAI:SeverityModel"] ?? "gpt-4.1-mini";

            if (string.IsNullOrWhiteSpace(apiKey))
                return SeverityEstimationResult.Failed("OpenAI API key is missing.");

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var rubric = """
            Classify the uploaded infrastructure-damage image into exactly one severity level.

            Rubric:
            - Low: cosmetic/minor visible issue, no immediate hazard
            - Medium: clear damage, should be scheduled
            - High: major damage, likely safety risk
            - Critical: immediate danger or severe infrastructure failure

            Return only valid JSON matching the schema.
            If the image does not clearly show infrastructure damage, choose Low unless the image is unusable.
            """;

            var payload = new
            {
                model,
                input = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "input_text",
                                text = rubric
                            },
                            new
                            {
                                type = "input_image",
                                image_url = imageSource
                            }
                        }
                    }
                },
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "severity_result",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            properties = new
                            {
                                severityStatus = new
                                {
                                    type = "string",
                                    @enum = new[] { "Low", "Medium", "High", "Critical" }
                                },
                                reason = new
                                {
                                    type = "string"
                                }
                            },
                            required = new[] { "severityStatus", "reason" },
                            additionalProperties = false
                        }
                    }
                }
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            try
            {
                using var response = await _httpClient.SendAsync(request, ct);
                var json = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    return SeverityEstimationResult.Failed(
                        $"Severity API returned {(int)response.StatusCode}. Body: {json}");
                }

                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("output", out var outputArray) ||
                    outputArray.ValueKind != JsonValueKind.Array ||
                    outputArray.GetArrayLength() == 0)
                {
                    return SeverityEstimationResult.Failed($"No output array was returned. Body: {json}");
                }

                var firstOutput = outputArray[0];

                if (!firstOutput.TryGetProperty("content", out var contentArray) ||
                    contentArray.ValueKind != JsonValueKind.Array ||
                    contentArray.GetArrayLength() == 0)
                {
                    return SeverityEstimationResult.Failed($"No content array was returned. Body: {json}");
                }

                var firstContent = contentArray[0];

                if (!firstContent.TryGetProperty("text", out var textElement))
                {
                    return SeverityEstimationResult.Failed($"No text field was returned in content. Body: {json}");
                }

                var outputJson = textElement.GetString();
                if (string.IsNullOrWhiteSpace(outputJson))
                {
                    return SeverityEstimationResult.Failed($"Structured output text was empty. Body: {json}");
                }

                using var resultDoc = JsonDocument.Parse(outputJson);
                var root = resultDoc.RootElement;

                var severityStatus = root.GetProperty("severityStatus").GetString();
                var reason = root.GetProperty("reason").GetString();

                if (severityStatus is not ("Low" or "Medium" or "High" or "Critical"))
                    return SeverityEstimationResult.Failed($"Model returned invalid severity '{severityStatus}'.");

                return SeverityEstimationResult.Success(severityStatus!, reason);
            }
            catch (Exception ex)
            {
                return SeverityEstimationResult.Failed(ex.Message);
            }
        }
    }
}