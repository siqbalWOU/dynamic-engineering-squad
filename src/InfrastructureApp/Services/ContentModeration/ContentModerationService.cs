/**This contains the behavior / logic:

load bad words

check local blocklist

call OpenAI moderation

return a decision

Responsibilities:
    1. Load a local bad-words list from a file
    2. Check text against that local blocklist first
    3. If no local blocked word is found, call the OpenAI moderation API
    4. Return a ContentModerationResult describing the final decision
    
**/


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace InfrastructureApp.Services.ContentModeration;

public sealed class ContentModerationService : IContentModerationService
{
    // HttpClient is used to call the OpenAI moderation API.
    private readonly HttpClient _http;
    // IConfiguration is used to read settings such as API key and bad word file path.
    private readonly IConfiguration _config;
    // IHostEnvironment gives access to environment-specific info,
    // including the application's root path on disk.
    private readonly IHostEnvironment _env;

    // Loaded once for the lifetime of the service
    // Lazy<HashSet<string>> means:
    // - The bad words file is NOT loaded immediately at startup
    // - It is only loaded the first time we actually need it
    // - Then it stays cached for the lifetime of the service
    private readonly Lazy<HashSet<string>> _badWords;

    // Simple retry schedule: (total 4 attempts)
    private static readonly int[] RetryDelaysMs = { 1000, 3000, 8000 };

    // Pull out "word-like" tokens.
    // Keeps letters/numbers/apostrophes so we can normalize and compare.
    // Example matches:
    // "hello", "can't", "abc123"
    private static readonly Regex TokenRegex =
        new Regex(@"[a-z0-9']+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public ContentModerationService(HttpClient http, IConfiguration config, IHostEnvironment env)
    {
        _http = http;
        _config = config;
        _env = env;
        // Lazy-load the bad words list only when first needed.
        _badWords = new Lazy<HashSet<string>>(LoadBadWords, isThreadSafe: true);
    }

    // Main public method required by IContentModerationService.
    // Checks whether the provided text is allowed.
    public async Task<ContentModerationResult> CheckAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new ContentModerationResult(Performed: true, IsAllowed: true, Flagged: false);

        // -----------------------------------------
        // LAYER 1: Local blocklist check
        // -----------------------------------------
        // First check the input against a locally stored bad-words list.
        // This is faster and cheaper than calling OpenAI.
        var localMatch = FindBlockedWord(text);
        if (localMatch is not null)
        {
            Console.WriteLine($"[Moderation] Local blocklist hit: {localMatch}");

            return new ContentModerationResult(
                Performed: true,
                IsAllowed: false,
                Flagged: true,
                Reason: $"Blocked word detected: {localMatch}"
            );
        }

        // -----------------------------------------
        // LAYER 2: OpenAI moderation fallback
        // -----------------------------------------
        // If local moderation did not find anything,
        // fall back to the OpenAI moderation API.
        return await CheckWithOpenAiAsync(text, ct);
    }

    // Looks for any blocked word inside the input.
    // Returns the first matching blocked token, or null if nothing is found.
    private string? FindBlockedWord(string input)
    {
        // Accessing _badWords.Value triggers LoadBadWords() the first time only.
        var set = _badWords.Value;

        // Normalize text first so obfuscated text becomes easier to detect.
        // Example:
        // "h@te" -> "hate"
        // "f.u.c.k" -> "fuck"
        var normalized = NormalizeForComparison(input);

        // Extract tokens from the normalized string using the regex.
        foreach (Match match in TokenRegex.Matches(normalized))
        {
            var token = match.Value;
            if (set.Contains(token))
                return token;
        }

        // No blocked word found.
        return null;
    }

    // Loads bad words from a text file into a HashSet.
    // HashSet provides fast lookup (roughly O(1)).
    private HashSet<string> LoadBadWords()
    {
        
        //place it somewhere predictable like /Data/Moderation/badWords.txt
        var configuredPath = _config["Moderation:BadWordsFilePath"];

        // If a configured path exists, use it.
        string path;
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            // If it is already an absolute path, use it directly.
            // Otherwise combine it with the app's content root path.
            path = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(_env.ContentRootPath, configuredPath);
        }
        else
        {
            // Default fallback path if no config setting exists.
            path = Path.Combine(_env.ContentRootPath, "badWords.txt");
        }

        // If the file does not exist, log a warning and return an empty set.
        if (!File.Exists(path))
        {
            Console.WriteLine($"[Moderation] WARNING: bad words file not found at: {path}");
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        // Read all lines from the file and normalize them.
        // Steps:
        // 1. Trim whitespace
        // 2. Remove blank lines
        // 3. Normalize for comparison
        // 4. Remove blank entries again
        // 5. Store in case-insensitive HashSet
        var words = File.ReadLines(path)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(NormalizeForComparison)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Console.WriteLine($"[Moderation] Loaded {words.Count} local blocked terms from {path}");
        return words;
    }

    // Normalizes text so comparisons are more effective.
    // This helps detect common obfuscation and leetspeak.
    private static string NormalizeForComparison(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var s = value.ToLowerInvariant();

        // Replace common leetspeak characters with normal letters.
        // Examples:
        // "h4te" -> "hate"
        // "v!olence" -> "violence"
        // Common leetspeak / obfuscation cleanup
        s = s.Replace('0', 'o')
             .Replace('1', 'i')
             .Replace('3', 'e')
             .Replace('4', 'a')
             .Replace('5', 's')
             .Replace('7', 't')
             .Replace('@', 'a')
             .Replace('$', 's')
             .Replace('!', 'i');

        // Remove punctuation between letters so that words like:
        // f.u.c.k
        // f-u-c-k
        // f_u_c_k
        // become:
        // fuck
        //
        // We keep:
        // - letters
        // - digits
        // - apostrophes
        // - whitespace
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            if (char.IsLetterOrDigit(c) || c == '\'' || char.IsWhiteSpace(c))
                sb.Append(c);
        }

        return sb.ToString();
    }

    // Calls OpenAI's moderation endpoint and returns the moderation result.
    private async Task<ContentModerationResult> CheckWithOpenAiAsync(string text, CancellationToken ct)
    {
        // Read the API key from configuration.
        var apiKey = _config["OpenAIModerationAPIkey"];
        // If no key is configured, return a failed moderation result.
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new ContentModerationResult(
                Performed: false,
                IsAllowed: false,
                Flagged: true,
                Reason: "Missing moderation API key."
            );
        }

        // Create a linked cancellation token so we respect the caller's token
        // but also enforce our own timeout of 40 seconds.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(40));

        // Retry loop.
        // Because attempt <= RetryDelaysMs.Length and the array length is 3,
        // this will run attempts 0,1,2,3 => total 4 attempts.
        for (int attempt = 0; attempt <= RetryDelaysMs.Length; attempt++)
        {
            Console.WriteLine($"[Moderation] OpenAI attempt #{attempt + 1}");

            // Build the HTTP request to the moderation endpoint.
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/moderations");
            // Add the Bearer token authorization header.
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // Build JSON payload sent to OpenAI.
            var payload = new
            {
                model = "omni-moderation-latest",
                input = text
            };

            // Serialize payload to JSON and attach it as request content.
            req.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage? resp = null;
            try
            {
                // Send the HTTP request.
                resp = await _http.SendAsync(req, timeoutCts.Token);
            }
            catch (Exception ex) when (attempt < RetryDelaysMs.Length)
            {
                // If an exception occurs and retries remain,
                // wait and try again.
                var delay = TimeSpan.FromMilliseconds(RetryDelaysMs[attempt]);
                Console.WriteLine($"[Moderation] OpenAI exception: {ex.Message}. Retrying after {delay.TotalMilliseconds} ms");
                await Task.Delay(delay, timeoutCts.Token);
                continue;
            }
            catch (Exception ex)
            {
                // If retries are exhausted, return a failed result.
                return new ContentModerationResult(
                    Performed: false,
                    IsAllowed: false,
                    Flagged: true,
                    Reason: $"Moderation exception: {ex.Message}"
                );
            }

            using (resp)
            {
                Console.WriteLine($"[Moderation] OpenAI response status: {(int)resp.StatusCode}");

                // If the API call succeeded
                if (resp.IsSuccessStatusCode)
                {
                    // Read response body as JSON stream
                    await using var stream = await resp.Content.ReadAsStreamAsync(timeoutCts.Token);
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: timeoutCts.Token);

                    // OpenAI moderation responses contain a "results" array.
                    // We use the first result object.
                    var r0 = doc.RootElement.GetProperty("results")[0];
                    // Read the top-level flagged boolean.
                    bool flagged = r0.GetProperty("flagged").GetBoolean();

                    Console.WriteLine($"[Moderation] OpenAI flagged={flagged}");

                    // If not flagged, allow the content.
                    if (!flagged)
                    {
                        Console.WriteLine("[Moderation] Decision: ALLOW");
                        return new ContentModerationResult(Performed: true, IsAllowed: true, Flagged: false);
                    }

                    // If flagged, try to determine which category triggered the flag.
                    string? category = null;
                    if (r0.TryGetProperty("categories", out var cats))
                    {
                        foreach (var prop in cats.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.True)
                            {
                                category = prop.Name;
                                break;
                            }
                        }
                    }

                    Console.WriteLine($"[Moderation] Decision: BLOCK by OpenAI. category={category ?? "(none)"}");

                    return new ContentModerationResult(
                        Performed: true,
                        IsAllowed: false,
                        Flagged: true,
                        Reason: category is null ? "Flagged by moderation." : $"Flagged category: {category}"
                    );
                }

                // For non-success responses, check whether we should retry.
                var statusCode = (int)resp.StatusCode;
                // Retry on rate limiting (429) or server errors (500+).
                bool shouldRetry = statusCode == 429 || statusCode >= 500;

                if (shouldRetry && attempt < RetryDelaysMs.Length)
                {
                    // If server provided Retry-After header, use it.
                    // Otherwise use our hardcoded retry delay.
                    var retryAfter = GetRetryAfterDelay(resp);
                    var delay = retryAfter ?? TimeSpan.FromMilliseconds(RetryDelaysMs[attempt]);

                    Console.WriteLine($"[Moderation] Retrying after {delay.TotalMilliseconds} ms");
                    await Task.Delay(delay, timeoutCts.Token);
                    continue;
                }

                // If we are not retrying, read the response body
                var body = await resp.Content.ReadAsStringAsync(timeoutCts.Token);
                return new ContentModerationResult(
                    Performed: false,
                    IsAllowed: false,
                    Flagged: true,
                    Reason: $"Moderation request failed: {statusCode} {resp.ReasonPhrase}. Body: {body}"
                );
            }
        }
        // This should normally never happen.
        return new ContentModerationResult(
            Performed: false,
            IsAllowed: false,
            Flagged: true,
            Reason: "Unexpected moderation retry loop exit."
        );
    }

    // Reads the Retry-After response header, if present,
    // and converts it into a TimeSpan delay.
    private static TimeSpan? GetRetryAfterDelay(HttpResponseMessage resp)
    {
        if (resp.Headers.TryGetValues("Retry-After", out var values))
        {
            var v = values.FirstOrDefault();
            // Retry-After is treated here as integer seconds.
            if (int.TryParse(v, out int seconds) && seconds >= 0)
                return TimeSpan.FromSeconds(seconds);
        }

        // No usable Retry-After header found.
        return null;
    }
}