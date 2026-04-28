using System.Threading;
using System.Threading.Tasks;
using InfrastructureApp.Services.ImageSeverity;

namespace InfrastructureApp_Tests.TestDoubles
{
    /// <summary>
    /// Test double for image moderation.
    /// Returns whatever result is currently configured in SeverityTestBehavior.
    /// </summary>
    public sealed class FakeImageModerationService : IImageModerationService
    {
        public Task<ImageModerationResult> ModerateImageAsync(
            string imageDataUrl,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SeverityTestBehavior.ModerationResult);
        }
    }
}