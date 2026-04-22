/**This action gives you a URL like this: /api/reportassist/suggestions?q=broken
If the input is empty, it returns: []
If the input has text, it asks the service for matches and returns them as JSON. **/

//The controller performs only lightweight request validation and delegates all autocomplete matching and ranking behavior to the service.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InfrastructureApp.Services.ReportAssist;
using Microsoft.AspNetCore.Mvc;

namespace InfrastructureApp.Controllers.Api
{
    [ApiController]
    [Route("api/reportassist")]
    public class ReportAssistApiController : ControllerBase
    {
        private readonly IReportDescriptionSuggestionService _suggestionService;

        public ReportAssistApiController(IReportDescriptionSuggestionService suggestionService)
        {
            _suggestionService = suggestionService;
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions([FromQuery] string? q, CancellationToken ct)
        {
            // If query is null, empty, or only whitespace,
            // return an empty array immediately and do not call the service.
            if (string.IsNullOrWhiteSpace(q))
            {
                return Ok(Array.Empty<string>());
            }

            // Trim leading/trailing spaces.
            string normalizedInput = q.Trim();

            // Get the last token the user is currently typing.
            string lastToken = normalizedInput
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault() ?? string.Empty;

            // If the CURRENT WORD is shorter than 2 characters,
            // return an empty array immediately and do not call the service.
            if (lastToken.Length < 2)
            {
                return Ok(Array.Empty<string>());
            }

            // Valid input -> ask the service for suggestions.
            var suggestions = await _suggestionService.GetSuggestionsAsync(q, ct);

            return Ok(suggestions);
        }
    }
}