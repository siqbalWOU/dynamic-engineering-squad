using System.Threading;
using System.Threading.Tasks;
using InfrastructureApp.Services.ImageSeverity;

namespace InfrastructureApp_Tests.TestDoubles
{
    /// <summary>
    /// Test double for image severity estimation.
    /// Returns whatever result is currently configured in SeverityTestBehavior.
    /// </summary>
    public sealed class FakeImageSeverityEstimationService : IImageSeverityEstimationService
    {
        public Task<SeverityEstimationResult> EstimateSeverityAsync(
            string imageDataUrl,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SeverityTestBehavior.SeverityResult);
        }
    }
}