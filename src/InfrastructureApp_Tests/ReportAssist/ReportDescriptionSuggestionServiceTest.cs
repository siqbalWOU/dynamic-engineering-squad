/** 
 * This test file checks the behavior of ReportDescriptionSuggestionService.
 *
 * It verifies things like:
 * - null / empty / whitespace input returns no suggestions
 * - suggestions are loaded from the JSON file
 * - matches are found correctly
 * - prefix matches are ranked before contains matches
 * - the last typed word can be used for matching
 * - duplicates are removed
 * - results are limited to 5
 * - matching is case-insensitive
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InfrastructureApp.Services.ReportAssist;
using Microsoft.AspNetCore.Hosting;
using NSubstitute;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Services.ReportAssist
{
    [TestFixture] // Marks this class as an NUnit test class
    public class ReportDescriptionSuggestionServiceTests
    {
        // Temporary fake project root used for each test
        private string _tempRoot = null!;

        // Fake web host environment so we can control ContentRootPath
        private IWebHostEnvironment _env = null!;

        // The real service we are testing
        private ReportDescriptionSuggestionService _service = null!;

        [SetUp]
        public void SetUp()
        {
            // Create a unique temporary folder for this test run.
            // This acts like the application's ContentRootPath.
            _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempRoot);

            // Create a fake IWebHostEnvironment
            _env = Substitute.For<IWebHostEnvironment>();

            // Tell the fake environment to use our temp folder as the content root
            _env.ContentRootPath.Returns(_tempRoot);

            // Create a fresh service instance using the fake environment
            _service = new ReportDescriptionSuggestionService(_env);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up the temp folder after each test
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }

        //null / empty / whitespace input returns no suggestions
        [Test]
        public async Task GetSuggestionsAsync_NullInput_ReturnsEmpty()
        {
            // Act: call the service with null input
            var result = await _service.GetSuggestionsAsync(null!);

            // Assert: should return no suggestions
            Assert.That(result, Is.Empty);
        }

        //null / empty / whitespace input returns no suggestions
        [Test]
        public async Task GetSuggestionsAsync_EmptyInput_ReturnsEmpty()
        {
            // Act: call the service with an empty string
            var result = await _service.GetSuggestionsAsync("");

            // Assert: should return no suggestions
            Assert.That(result, Is.Empty);
        }

        //null / empty / whitespace input returns no suggestions
        [Test]
        public async Task GetSuggestionsAsync_WhitespaceInput_ReturnsEmpty()
        {
            // Act: call the service with only spaces
            var result = await _service.GetSuggestionsAsync("   ");

            // Assert: should return no suggestions
            Assert.That(result, Is.Empty);
        }

        //suggestions are loaded from the JSON file that we have, if not it fails
        [Test]
        public async Task GetSuggestionsAsync_MissingJsonFile_ReturnsEmpty()
        {
            // Act:
            // We do NOT create the JSON file here, so the service should act like no suggestions exist
            var result = await _service.GetSuggestionsAsync("broken");

            // Assert: should return no suggestions
            Assert.That(result, Is.Empty);
        }

        //matches are found correctly
        [Test]
        public async Task GetSuggestionsAsync_PrefixMatch_ReturnsMatchingSuggestions()
        {
            // Arrange:
            // Write a fake descriptionSuggestions.json file with sample data
            WriteSuggestionsJson(
                "broken sign",
                "broken streetlight",
                "pothole in road",
                "graffiti on wall"
            );

            // Act:
            // Search using "broken"
            var result = await _service.GetSuggestionsAsync("broken");

            // Assert:
            // Only suggestions starting with or containing "broken" should be returned
            Assert.That(result, Is.EqualTo(new[]
            {
                "broken sign",
                "broken streetlight"
            }));
        }

        //matches are found correctly regardless of case
        [Test]
        public async Task GetSuggestionsAsync_IsCaseInsensitive()
        {
            // Arrange:
            // Use different capitalization in the saved suggestions
            WriteSuggestionsJson(
                "Broken Sign",
                "BROKEN STREETLIGHT",
                "Pothole"
            );

            // Act:
            // Search with lowercase input
            var result = await _service.GetSuggestionsAsync("broken");

            // Assert:
            // Matching should ignore case
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result, Does.Contain("Broken Sign"));
            Assert.That(result, Does.Contain("BROKEN STREETLIGHT"));
        }

        //the last typed word can be used for matching
        [Test]
        public async Task GetSuggestionsAsync_UsesLastTypedWord()
        {
            // Arrange:
            WriteSuggestionsJson(
                "broken sign",
                "fallen tree",
                "streetlight broken",
                "pothole"
            );

            // Act:
            // The full phrase "there is a brok" probably won't match,
            // but the last typed token "brok" should be used for matching
            var result = await _service.GetSuggestionsAsync("there is a brok");

            // Assert:
            // Suggestions related to "brok" should still be found
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result, Does.Contain("broken sign"));
            Assert.That(result, Does.Contain("streetlight broken"));
        }

        //prefix matches are ranked before contains matches
        [Test]
        public async Task GetSuggestionsAsync_PrefixMatchesRankBeforeContainsMatches()
        {
            // Arrange:
            WriteSuggestionsJson(
                "broken sign",
                "streetlight broken",
                "sign is broken",
                "broken sidewalk"
            );

            // Act:
            var result = await _service.GetSuggestionsAsync("broken");

            // Assert:
            // Suggestions that START with "broken" should come before
            // suggestions that merely CONTAIN "broken" later in the text.
            // Within equal ranking, alphabetical ordering is used.
            Assert.That(result.ToList(), Is.EqualTo(new[]
            {
                "broken sidewalk",
                "broken sign",
                "sign is broken",
                "streetlight broken"
            }));
        }

        //duplicates are removed
        [Test]
        public async Task GetSuggestionsAsync_RemovesDuplicateSuggestions_IgnoringCase()
        {
            // Arrange:
            // Add duplicates with different casing
            WriteSuggestionsJson(
                "broken sign",
                "Broken Sign",
                "broken streetlight"
            );

            // Act:
            var result = await _service.GetSuggestionsAsync("broken");

            // Assert:
            // Duplicate suggestions should only appear once
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(
                result.Count(x => x.Equals("broken sign", StringComparison.OrdinalIgnoreCase)),
                Is.EqualTo(1)
            );
        }

        //results are limited to 5
        [Test]
        public async Task GetSuggestionsAsync_LimitsResultsToFive()
        {
            // Arrange:
            // Add more than 5 matching suggestions
            WriteSuggestionsJson(
                "broken sign",
                "broken streetlight",
                "broken sidewalk",
                "broken curb",
                "broken pole",
                "broken hydrant",
                "broken fence"
            );

            // Act:
            var result = await _service.GetSuggestionsAsync("broken");

            // Assert:
            // Service should only return the first 5 matches
            Assert.That(result, Has.Count.EqualTo(5));
        }

        //tests trim functionality
        [Test]
        public async Task GetSuggestionsAsync_TrimsInputBeforeSearching()
        {
            // Arrange:
            WriteSuggestionsJson(
                "broken sign",
                "broken streetlight",
                "pothole"
            );

            // Act:
            // Input has extra spaces at the start and end
            var result = await _service.GetSuggestionsAsync("   broken   ");

            // Assert:
            // Trimming should happen before searching, so results still match
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result, Does.Contain("broken sign"));
            Assert.That(result, Does.Contain("broken streetlight"));
        }

        /// <summary>
        /// Helper method used by the tests.
        ///
        /// It creates the folder:
        ///   Data/Moderation
        /// inside the fake ContentRootPath,
        /// then writes descriptionSuggestions.json with the provided suggestions.
        ///
        /// This lets each test control exactly what suggestion data the service loads.
        /// </summary>
        private void WriteSuggestionsJson(params string[] suggestions)
        {
            // Build the folder path the real service expects
            var folder = Path.Combine(_tempRoot, "Data", "Moderation");
            Directory.CreateDirectory(folder);

            // Build the JSON file path
            var filePath = Path.Combine(folder, "descriptionSuggestions.json");

            // Convert the string array into JSON text
            var json = System.Text.Json.JsonSerializer.Serialize(suggestions.ToList());

            // Write the JSON into the file
            File.WriteAllText(filePath, json);
        }
    }
}