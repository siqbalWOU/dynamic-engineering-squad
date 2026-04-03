using System.Threading;
using System.Threading.Tasks;

namespace InfrastructureApp.Services.ContentModeration;

public interface IContentModerationService
{
    /*
        Checks whether the provided text passes moderation rules.

        Parameters:
        text
            The user-provided text that needs to be moderated.

        ct
            Optional cancellation token allowing the caller
            to cancel the moderation request if needed
            (for example when a web request is aborted).

        Returns:
        Task<ContentModerationResult>

        Because this method may call external APIs,
        it is asynchronous and returns a Task.

        The result object describes:
        - whether moderation was performed
        - whether the content is allowed
        - whether it was flagged
        - the reason/category if blocked
    */
    Task<ContentModerationResult> CheckAsync(string text, CancellationToken ct = default);
}