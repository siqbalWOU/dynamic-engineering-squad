//implementation of the IReportDescriptionSuggestionService

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace InfrastructureApp.Services.ReportAssist
{
    public sealed class ReportDescriptionSuggestionService : IReportDescriptionSuggestionService
    {
        private readonly IWebHostEnvironment _env;
        private readonly Lazy<List<string>> _suggestions;

        public ReportDescriptionSuggestionService(IWebHostEnvironment env)
        {
            _env = env;

            /**do not load the JSON file immediately
            load it only the first time suggestions are actually needed
            then keep the data in memory for reuse**/
            _suggestions = new Lazy<List<string>>(LoadSuggestions);
        }

        //this method returns suggestions when typing in description box
        public Task<IReadOnlyList<string>> GetSuggestionsAsync(string input, CancellationToken ct = default)
        {
            // If input is null, empty, or whitespace, return no suggestions.
            if (string.IsNullOrWhiteSpace(input))
            {
                return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
            }

            // Remove spaces from the beginning/end.
            string normalizedInput = input.Trim();

            // Find the last word the user is currently typing.
            // Example:
            // "There is a brok" -> "brok"
            string lastToken = normalizedInput
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault() ?? string.Empty;

            // If the current word is too short, do not return suggestions.
            if (lastToken.Length < 2)
            {
                return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
            }

            /** Return suggestions where either:
            *  - the whole input matches, or
            *  - the last typed word matches
            *
            * Prefix matches rank highest.
            * Limit to 5 suggestions.
            */
            var matches = _suggestions.Value
                .Select(s => new
                {
                    Suggestion = s,
                    Score =
                        s.StartsWith(normalizedInput, StringComparison.OrdinalIgnoreCase) ? 0 :
                        s.StartsWith(lastToken, StringComparison.OrdinalIgnoreCase) ? 1 :
                        s.Contains(normalizedInput, StringComparison.OrdinalIgnoreCase) ? 2 :
                        s.Contains(lastToken, StringComparison.OrdinalIgnoreCase) ? 3 : 999
                })
                .Where(x => x.Score < 999)
                .OrderBy(x => x.Score)
                .ThenBy(x => x.Suggestion)
                .Select(x => x.Suggestion)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();

            return Task.FromResult<IReadOnlyList<string>>(matches);
        }
        //loads the list of autocomplete suggestions
        private List<string> LoadSuggestions()
        {
            string filePath = Path.Combine(_env.ContentRootPath, "Data", "Moderation", "descriptionSuggestions.json");

            if (!File.Exists(filePath))
            {
                return new List<string>();
            }

            string json = File.ReadAllText(filePath);

            var items = JsonSerializer.Deserialize<List<string>>(json);

            return items ?? new List<string>();
        }
    }
}