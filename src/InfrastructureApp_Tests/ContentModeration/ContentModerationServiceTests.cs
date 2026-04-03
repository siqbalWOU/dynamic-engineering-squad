using System;                     // Basic .NET types like Guid, Exception
using System.Collections.Generic; // Dictionary<T>
using System.IO;                  // File and directory helpers
using System.Net;                 // HttpStatusCode
using System.Net.Http;            // HttpClient, HttpMessageHandler, HttpRequestMessage, HttpResponseMessage
using System.Text;                // Encoding
using System.Text.Json;           // JsonSerializer
using System.Threading;           // CancellationToken
using System.Threading.Tasks;     // Task / async / await
using InfrastructureApp.Services.ContentModeration; // The service and result types being tested
using Microsoft.Extensions.Configuration;           // IConfiguration
using Microsoft.Extensions.Hosting;                 // IHostEnvironment
using NSubstitute;                                  // Used to fake IHostEnvironment
using NUnit.Framework;                              // NUnit test framework

namespace InfrastructureApp_Tests.Services.ContentModeration
{
    [TestFixture] // Marks this class as an NUnit test class
    public class ContentModerationServiceTests
    {
        // Temporary root folder used by each test.
        // Each test gets its own unique directory so tests do not interfere with each other.
        private string _tempRoot = null!;

        [SetUp]
        public void SetUp()
        {
            // Build a unique temp directory path like:
            // C:\Temp\ContentModerationServiceTests\<guid>
            _tempRoot = Path.Combine(Path.GetTempPath(), "ContentModerationServiceTests", Guid.NewGuid().ToString());

            // Actually create the directory before each test.
            Directory.CreateDirectory(_tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            // After each test, clean up the temp folder if it still exists.
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }

        // ---------------------------------------------------
        // Fake HttpMessageHandler
        // ---------------------------------------------------
        // HttpClient internally uses HttpMessageHandler to actually send requests.
        // By faking this handler, we can intercept outgoing HTTP calls and return
        // whatever response we want without touching the real internet.
        private sealed class FakeHttpMessageHandler : HttpMessageHandler
        {
            // Delegate that decides what HTTP response to return.
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            // Tracks how many times HTTP was called.
            public int CallCount { get; private set; }

            // Stores the last outgoing HTTP request for inspection in assertions.
            public HttpRequestMessage? LastRequest { get; private set; }

            public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // Record that an HTTP call happened.
                CallCount++;

                // Save the outgoing request so tests can inspect method, URL, headers, etc.
                LastRequest = request;

                // Return a fake response produced by the delegate.
                return Task.FromResult(_responder(request));
            }
        }

        // Builds an in-memory IConfiguration object from key/value pairs.
        // This lets the tests fake appsettings values like:
        // - Moderation:BadWordsFilePath
        // - OpenAIModerationAPIkey
        private static IConfiguration BuildConfig(Dictionary<string, string?> values)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }

        // Builds a fake IHostEnvironment using NSubstitute.
        // The service uses ContentRootPath to find badWords.txt on disk.
        private IHostEnvironment BuildHostEnvironment(string contentRootPath)
        {
            var env = Substitute.For<IHostEnvironment>();
            env.ContentRootPath.Returns(contentRootPath);
            return env;
        }

        // Builds an HttpClient using the fake message handler.
        private static HttpClient BuildHttpClient(HttpMessageHandler handler)
        {
            return new HttpClient(handler);
        }

        // Creates a fake badWords.txt file in the temp folder for the current test.
        // Returns the full file path to the created file.
        private string CreateBadWordsFile(params string[] words)
        {
            var dataDir = Path.Combine(_tempRoot, "Data", "Moderation");
            Directory.CreateDirectory(dataDir);

            var filePath = Path.Combine(dataDir, "badWords.txt");

            // Write one blocked word per line.
            File.WriteAllLines(filePath, words);

            return filePath;
        }

        // Helper method to create an HTTP response with a JSON body.
        // This is used to simulate fake OpenAI API responses.
        private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, object body)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json")
            };
        }

        //This test checks that blank input is treated as safe.
        [Test]
        public async Task CheckAsync_WhenTextIsNullOrWhitespace_ReturnsAllowed()
        {
            // Create a handler that would fail the test if HTTP is ever called.
            // Empty input should return immediately and never hit OpenAI.
            var handler = new FakeHttpMessageHandler(_ =>
                throw new Exception("HTTP should not be called for empty input."));

            var http = BuildHttpClient(handler);

            // Empty config: no bad words path, no API key.
            // This is okay because empty input should short-circuit before needing them.
            var config = BuildConfig(new Dictionary<string, string?>());

            var env = BuildHostEnvironment(_tempRoot);

            // System Under Test (SUT)
            var sut = new ContentModerationService(http, config, env);

            // Test empty, whitespace, and null input.
            var result1 = await sut.CheckAsync("");
            var result2 = await sut.CheckAsync("   ");
            var result3 = await sut.CheckAsync((string?)null!);

            // Assert all results say "allowed" and that no HTTP calls were made.
            Assert.Multiple(() =>
            {
                Assert.That(result1.Performed, Is.True);
                Assert.That(result1.IsAllowed, Is.True);
                Assert.That(result1.Flagged, Is.False);

                Assert.That(result2.Performed, Is.True);
                Assert.That(result2.IsAllowed, Is.True);
                Assert.That(result2.Flagged, Is.False);

                Assert.That(result3.Performed, Is.True);
                Assert.That(result3.IsAllowed, Is.True);
                Assert.That(result3.Flagged, Is.False);

                Assert.That(handler.CallCount, Is.EqualTo(0));
            });
        }

        //This test checks the local blocklist layer.
        //bad words like shit and damn get blocked immediately
        [Test]
        public async Task CheckAsync_WhenLocalBadWordExists_BlocksImmediately_AndDoesNotCallHttp()
        {
            // Create a fake local badWords.txt containing blocked words.
            CreateBadWordsFile("shit", "damn");

            // If HTTP gets called, fail the test.
            // The local blocklist should catch the text before OpenAI is needed.
            var handler = new FakeHttpMessageHandler(_ =>
                throw new Exception("HTTP should not be called when local blocklist catches input."));

            var http = BuildHttpClient(handler);

            // Configure the service to use our fake badWords file and a fake API key.
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["Moderation:BadWordsFilePath"] = Path.Combine("Data", "Moderation", "badWords.txt"),
                ["OpenAIModerationAPIkey"] = "test-key"
            });

            var env = BuildHostEnvironment(_tempRoot);
            var sut = new ContentModerationService(http, config, env);

            // Input contains a local bad word.
            var result = await sut.CheckAsync("This pothole is shit.");

            // Assert the service blocked it immediately using the local list.
            Assert.Multiple(() =>
            {
                Assert.That(result.Performed, Is.True);
                Assert.That(result.IsAllowed, Is.False);
                Assert.That(result.Flagged, Is.True);
                Assert.That(result.Reason, Does.Contain("Blocked word detected"));
                Assert.That(result.Reason, Does.Contain("shit"));
                Assert.That(handler.CallCount, Is.EqualTo(0));
            });
        }

        //This verifies that local blocking ignores case.
        //SHIT is the same as shit
        [Test]
        public async Task CheckAsync_LocalBlocklist_IsCaseInsensitive()
        {
            // Create local bad word in lowercase.
            CreateBadWordsFile("shit");

            var handler = new FakeHttpMessageHandler(_ =>
                throw new Exception("HTTP should not be called when local blocklist catches input."));

            var http = BuildHttpClient(handler);
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["Moderation:BadWordsFilePath"] = Path.Combine("Data", "Moderation", "badWords.txt"),
                ["OpenAIModerationAPIkey"] = "test-key"
            });

            var env = BuildHostEnvironment(_tempRoot);
            var sut = new ContentModerationService(http, config, env);

            // Input uses uppercase letters.
            var result = await sut.CheckAsync("This road is SHIT");

            // Assert the service still catches it.
            Assert.Multiple(() =>
            {
                Assert.That(result.Performed, Is.True);
                Assert.That(result.IsAllowed, Is.False);
                Assert.That(result.Flagged, Is.True);
                Assert.That(result.Reason, Does.Contain("shit").IgnoreCase);
                Assert.That(handler.CallCount, Is.EqualTo(0));
            });
        }

        //This verifies your normalization catches basic leetspeak.
        //shit --> sh1t
        [Test]
        public async Task CheckAsync_LocalBlocklist_CatchesSimpleObfuscation()
        {
            // Local list has normal spelling.
            CreateBadWordsFile("shit");

            var handler = new FakeHttpMessageHandler(_ =>
                throw new Exception("HTTP should not be called when local blocklist catches input."));

            var http = BuildHttpClient(handler);
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["Moderation:BadWordsFilePath"] = Path.Combine("Data", "Moderation", "badWords.txt"),
                ["OpenAIModerationAPIkey"] = "test-key"
            });

            var env = BuildHostEnvironment(_tempRoot);
            var sut = new ContentModerationService(http, config, env);

            // The service should normalize "sh1t" -> "shit"
            var result = await sut.CheckAsync("This road is sh1t");

            Assert.Multiple(() =>
            {
                Assert.That(result.Performed, Is.True);
                Assert.That(result.IsAllowed, Is.False);
                Assert.That(result.Flagged, Is.True);
                Assert.That(result.Reason, Does.Contain("shit"));
                Assert.That(handler.CallCount, Is.EqualTo(0));
            });
        }

        //This tests a failure path.
        //bad words file path points to a file that does not exist
        //API key missing
        [Test]
        public async Task CheckAsync_WhenBadWordsFileMissing_AndApiKeyMissing_ReturnsNotPerformed()
        {
            // No badWords file created.
            // No API key provided.
            // So local list will be empty and OpenAI cannot be called.
            var handler = new FakeHttpMessageHandler(_ =>
                throw new Exception("HTTP should not be called when API key is missing."));

            var http = BuildHttpClient(handler);
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["Moderation:BadWordsFilePath"] = Path.Combine("Data", "Moderation", "does-not-exist.txt")
            });

            var env = BuildHostEnvironment(_tempRoot);
            var sut = new ContentModerationService(http, config, env);

            var result = await sut.CheckAsync("Normal description with no blocked words.");

            // Assert the service reports failure due to missing API key.
            Assert.Multiple(() =>
            {
                Assert.That(result.Performed, Is.False);
                Assert.That(result.IsAllowed, Is.False);
                Assert.That(result.Flagged, Is.True);
                Assert.That(result.Reason, Is.EqualTo("Missing moderation API key."));
                Assert.That(handler.CallCount, Is.EqualTo(0));
            });
        }

        //This tests the fallback to OpenAI when local moderation does not block.
        [Test]
        public async Task CheckAsync_WhenLocalListDoesNotMatch_AndOpenAiAllows_ReturnsAllowed()
        {
            // Local list exists, but input should not match it.
            CreateBadWordsFile("shit", "damn");

            // Fake OpenAI returns flagged = false
            var handler = new FakeHttpMessageHandler(req =>
            {
                return JsonResponse(HttpStatusCode.OK, new
                {
                    results = new[]
                    {
                        new
                        {
                            flagged = false,
                            categories = new { }
                        }
                    }
                });
            });

            var http = BuildHttpClient(handler);
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["Moderation:BadWordsFilePath"] = Path.Combine("Data", "Moderation", "badWords.txt"),
                ["OpenAIModerationAPIkey"] = "test-key"
            });

            var env = BuildHostEnvironment(_tempRoot);
            var sut = new ContentModerationService(http, config, env);

            // Input is clean locally, so the service should fall back to OpenAI.
            var result = await sut.CheckAsync("There is a pothole near Main Street.");

            Assert.Multiple(() =>
            {
                // Service should allow the content.
                Assert.That(result.Performed, Is.True);
                Assert.That(result.IsAllowed, Is.True);
                Assert.That(result.Flagged, Is.False);

                // Exactly one HTTP call should have been made.
                Assert.That(handler.CallCount, Is.EqualTo(1));

                // Verify outgoing HTTP request details.
                Assert.That(handler.LastRequest, Is.Not.Null);
                Assert.That(handler.LastRequest!.Method, Is.EqualTo(HttpMethod.Post));
                Assert.That(handler.LastRequest.RequestUri!.ToString(), Is.EqualTo("https://api.openai.com/v1/moderations"));
                Assert.That(handler.LastRequest.Headers.Authorization, Is.Not.Null);
                Assert.That(handler.LastRequest.Headers.Authorization!.Scheme, Is.EqualTo("Bearer"));
                Assert.That(handler.LastRequest.Headers.Authorization!.Parameter, Is.EqualTo("test-key"));
            });
        }


        
        [Test]
        public async Task CheckAsync_WhenLocalListDoesNotMatch_AndOpenAiFlags_ReturnsBlockedWithCategory()
        {
            // Local list exists, but input avoids listed words.
            CreateBadWordsFile("shit", "damn");

            // Fake OpenAI returns flagged = true and harassment = true
            var handler = new FakeHttpMessageHandler(req =>
            {
                return JsonResponse(HttpStatusCode.OK, new
                {
                    results = new[]
                    {
                        new
                        {
                            flagged = true,
                            categories = new
                            {
                                harassment = true,
                                violence = false
                            }
                        }
                    }
                });
            });

            var http = BuildHttpClient(handler);
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["Moderation:BadWordsFilePath"] = Path.Combine("Data", "Moderation", "badWords.txt"),
                ["OpenAIModerationAPIkey"] = "test-key"
            });

            var env = BuildHostEnvironment(_tempRoot);
            var sut = new ContentModerationService(http, config, env);

            var result = await sut.CheckAsync("I want to insult somebody without using a listed bad word.");

            // Assert the service uses OpenAI result and returns the flagged category.
            Assert.Multiple(() =>
            {
                Assert.That(result.Performed, Is.True);
                Assert.That(result.IsAllowed, Is.False);
                Assert.That(result.Flagged, Is.True);
                Assert.That(result.Reason, Is.EqualTo("Flagged category: harassment"));
                Assert.That(handler.CallCount, Is.EqualTo(1));
            });
        }

        /**This tests another configuration failure case.

        Setup:

        local file exists

        input does not match local list

        API key is blank

        Expected:

        no HTTP call

        return "Missing moderation API key."

        This confirms your service handles missing credentials cleanly.**/
        [Test]
        public async Task CheckAsync_WhenApiKeyMissing_AndNoLocalMatch_ReturnsMissingKeyResult()
        {
            // Local file exists but input does not match any blocked words.
            CreateBadWordsFile("shit", "damn");

            // Without API key, the service cannot call OpenAI.
            var handler = new FakeHttpMessageHandler(_ =>
                throw new Exception("HTTP should not be called when API key is missing."));

            var http = BuildHttpClient(handler);
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["Moderation:BadWordsFilePath"] = Path.Combine("Data", "Moderation", "badWords.txt"),
                ["OpenAIModerationAPIkey"] = ""
            });

            var env = BuildHostEnvironment(_tempRoot);
            var sut = new ContentModerationService(http, config, env);

            var result = await sut.CheckAsync("There is a pothole near Main Street.");

            Assert.Multiple(() =>
            {
                Assert.That(result.Performed, Is.False);
                Assert.That(result.IsAllowed, Is.False);
                Assert.That(result.Flagged, Is.True);
                Assert.That(result.Reason, Is.EqualTo("Missing moderation API key."));
                Assert.That(handler.CallCount, Is.EqualTo(0));
            });
        }
    }
}