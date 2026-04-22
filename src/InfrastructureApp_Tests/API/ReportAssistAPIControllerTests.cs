//verify that invalid input returns early without calling the service, and valid input calls the service exactly once and returns its result wrapped in an Ok response

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InfrastructureApp.Controllers.Api;
using InfrastructureApp.Services.ReportAssist;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Controllers.Api
{
    [TestFixture] // Marks this class as a test class for NUnit
    public class ReportAssistApiControllerTests
    {
        // Mock of your service dependency (fake implementation)
        private IReportDescriptionSuggestionService _suggestionService = null!;

        // The controller we are testing
        private ReportAssistApiController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            // Create a fake service using NSubstitute
            _suggestionService = Substitute.For<IReportDescriptionSuggestionService>();

            // Inject the fake service into the controller
            _controller = new ReportAssistApiController(_suggestionService);
        }

        /**
         * TEST 1:
         * If query is null → return empty array
         * AND do NOT call the service
         */
        [Test]
        public async Task GetSuggestions_NullQuery_ReturnsOkWithEmptyArray()
        {
            // Act: call controller with null query
            var result = await _controller.GetSuggestions(null, CancellationToken.None);

            // Assert: result should be HTTP 200 OK
            Assert.That(result, Is.TypeOf<OkObjectResult>());

            var okResult = (OkObjectResult)result;

            // Assert: response body is a string array
            Assert.That(okResult.Value, Is.InstanceOf<string[]>());

            var values = (string[])okResult.Value!;

            // Assert: array is empty
            Assert.That(values, Is.Empty);

            // VERY IMPORTANT:
            // Service should NOT be called for invalid input
            await _suggestionService
                .DidNotReceive()
                .GetSuggestionsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        /**
         * TEST 2:
         * If query is "" (empty string) → same behavior as null
         */
        [Test]
        public async Task GetSuggestions_EmptyQuery_ReturnsOkWithEmptyArray()
        {
            var result = await _controller.GetSuggestions("", CancellationToken.None);

            Assert.That(result, Is.TypeOf<OkObjectResult>());

            var okResult = (OkObjectResult)result;
            Assert.That(okResult.Value, Is.InstanceOf<string[]>());

            var values = (string[])okResult.Value!;
            Assert.That(values, Is.Empty);

            // Service should NOT be called
            await _suggestionService
                .DidNotReceive()
                .GetSuggestionsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        /**
         * TEST 3:
         * If query is only whitespace → also treated as invalid
         */
        [Test]
        public async Task GetSuggestions_WhitespaceQuery_ReturnsOkWithEmptyArray()
        {
            var result = await _controller.GetSuggestions("   ", CancellationToken.None);

            Assert.That(result, Is.TypeOf<OkObjectResult>());

            var okResult = (OkObjectResult)result;
            Assert.That(okResult.Value, Is.InstanceOf<string[]>());

            var values = (string[])okResult.Value!;
            Assert.That(values, Is.Empty);

            // Service should NOT be called
            await _suggestionService
                .DidNotReceive()
                .GetSuggestionsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        /**
         * TEST 4:
         * Valid query → service should be called exactly once
         */
        [Test]
        public async Task GetSuggestions_ValidQuery_CallsServiceOnce()
        {
            // Arrange
            var query = "broken";
            var ct = new CancellationTokenSource().Token;

            // Mock what the service returns
            _suggestionService
                .GetSuggestionsAsync(query, ct)
                .Returns(new List<string> { "broken sign", "broken streetlight" });

            // Act
            await _controller.GetSuggestions(query, ct);

            // Assert:
            // Service should be called exactly once with correct parameters
            await _suggestionService.Received(1).GetSuggestionsAsync(query, ct);
        }

        /**
         * TEST 5:
         * Valid query → controller should return what the service returns
         */
        [Test]
        public async Task GetSuggestions_ValidQuery_ReturnsOkWithSuggestions()
        {
            // Arrange
            var query = "broken";

            // Expected data from service
            var expected = new List<string> { "broken sign", "broken streetlight" };

            _suggestionService
                .GetSuggestionsAsync(query, Arg.Any<CancellationToken>())
                .Returns(expected);

            // Act
            var result = await _controller.GetSuggestions(query, CancellationToken.None);

            // Assert: result is HTTP 200 OK
            Assert.That(result, Is.TypeOf<OkObjectResult>());

            var okResult = (OkObjectResult)result;

            // Assert: controller returns EXACT same object from service
            Assert.That(okResult.Value, Is.SameAs(expected));
        }

        /**
         * TEST 6:
         * Ensure CancellationToken is passed correctly to the service
         */
        [Test]
        public async Task GetSuggestions_ValidQuery_PassesCancellationTokenToService()
        {
            var query = "broken";

            using var cts = new CancellationTokenSource();
            var ct = cts.Token;

            _suggestionService
                .GetSuggestionsAsync(query, ct)
                .Returns(new List<string>());

            // Act
            await _controller.GetSuggestions(query, ct);

            // Assert:
            // Verify the SAME token was passed into the service
            await _suggestionService.Received(1).GetSuggestionsAsync(query, ct);
        }

        /**
        * TEST 7:
        * Short last token (e.g., "p a") → should return empty and NOT call service
        */
        [Test]
        public async Task GetSuggestions_ShortLastToken_ReturnsOkWithEmptyArray()
        {
            var result = await _controller.GetSuggestions("p a", CancellationToken.None);

            Assert.That(result, Is.TypeOf<OkObjectResult>());

            var okResult = (OkObjectResult)result;
            Assert.That(okResult.Value, Is.InstanceOf<string[]>());

            var values = (string[])okResult.Value!;
            Assert.That(values, Is.Empty);

            await _suggestionService
                .DidNotReceive()
                .GetSuggestionsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        /**
        * TEST 8:
        * Single character input → should return empty and NOT call service
        */
        [Test]
        public async Task GetSuggestions_OneCharacterQuery_ReturnsOkWithEmptyArray()
        {
            var result = await _controller.GetSuggestions("p", CancellationToken.None);

            Assert.That(result, Is.TypeOf<OkObjectResult>());

            var okResult = (OkObjectResult)result;
            Assert.That(okResult.Value, Is.InstanceOf<string[]>());

            var values = (string[])okResult.Value!;
            Assert.That(values, Is.Empty);

            await _suggestionService
                .DidNotReceive()
                .GetSuggestionsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }
}