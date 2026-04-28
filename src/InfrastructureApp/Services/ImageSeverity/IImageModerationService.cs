//is this image safe/usable to process

using System.Threading;
using System.Threading.Tasks;

namespace InfrastructureApp.Services.ImageSeverity
{
    public interface IImageModerationService
    {
        Task<ImageModerationResult> ModerateImageAsync(
            string imageUrl,
            CancellationToken ct = default);
    }
}