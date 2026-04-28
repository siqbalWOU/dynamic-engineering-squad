//how serious is the infrastructure damage?

using System.Threading;
using System.Threading.Tasks;

namespace InfrastructureApp.Services.ImageSeverity
{
    public interface IImageSeverityEstimationService
    {
        Task<SeverityEstimationResult> EstimateSeverityAsync(
            string imageUrl,
            CancellationToken ct = default);
    }
}