using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace InfrastructureApp.Services.ImageHashing
{
    // This small record lets us return both hashes together.
    public sealed record ImageHashResult(string Sha256, long PHash);

    public interface IImageHashService
    {
        // Computes both SHA-256 and pHash from the uploaded image stream.
        Task<ImageHashResult> ComputeHashesAsync(Stream imageStream, CancellationToken ct = default);

        // Compares two pHashes.
        // Smaller number means more visually similar.
        int HammingDistance(long leftHash, long rightHash);
    }
}