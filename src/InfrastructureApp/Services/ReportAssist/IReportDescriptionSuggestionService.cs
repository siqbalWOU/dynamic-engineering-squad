/**This defines one job:
take whatever the user typed
return a list of matching suggestion strings**/

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InfrastructureApp.Services.ReportAssist
{
    public interface IReportDescriptionSuggestionService
    {
        Task<IReadOnlyList<string>> GetSuggestionsAsync(string input, CancellationToken ct = default);
    }
}