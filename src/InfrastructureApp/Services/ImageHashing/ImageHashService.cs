using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace InfrastructureApp.Services.ImageHashing
{
    public sealed class ImageHashService : IImageHashService
    {
        // We resize the image to 32x32 before running the DCT (Discrete Cosine Transform).
        // This is the size we resize every image to before computing the perceptual hash.
        // 32x32 is a common preprocessing size for pHash because it is small enough
        // to be efficient, but large enough to preserve general visual structure.
        private const int PHashImageSize = 32;

        public async Task<ImageHashResult> ComputeHashesAsync(Stream imageStream, CancellationToken ct = default)
        {
            // Guard clause:
            // If no stream is provided, stop immediately with a clear exception.
            if (imageStream == null)
                throw new ArgumentNullException(nameof(imageStream));

            // We copy the uploaded stream into memory once so we can:
            // 1) compute SHA-256
            // 2) load the same bytes into ImageSharp for pHash
            // Instead of re-reading the original stream multiple times,
            // we copy it once and reuse the bytes.
            await using var memory = new MemoryStream();

            // If the original stream supports seeking, reset it to the start.
            // This helps avoid issues if some earlier code already read part of the stream.
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }

            // Copy the uploaded image stream into memory asynchronously.
            await imageStream.CopyToAsync(memory, ct);

            // Convert the in-memory stream into a byte array.
            byte[] imageBytes = memory.ToArray();

            // Compute an exact-file hash (same bytes = same SHA-256).
            string sha256 = ComputeSha256(imageBytes);

            // Compute a perceptual hash (similar-looking images = similar pHash values).
            long pHash = ComputePerceptualHash(imageBytes);

            // Return both hash values packaged together in an ImageHashResult.
            return new ImageHashResult(sha256, pHash);
        }

        public int HammingDistance(long leftHash, long rightHash)
        {
            // XOR compares the two hashes bit-by-bit.
            // Any bit that differs becomes 1 in the XOR result.
            ulong diff = unchecked((ulong)(leftHash ^ rightHash));

            int count = 0;

            // Brian Kernighan's algorithm for counting set bits:
            // diff &= (diff - 1) removes the lowest set 1-bit each loop.
            // So the number of loops = number of different bits.
            while (diff != 0)
            {
                diff &= (diff - 1);
                count++;
            }

            // The result is the Hamming distance:
            // how many bits differ between the two hashes.
            return count;
        }

        private static string ComputeSha256(byte[] imageBytes)
        {
            // Create a SHA-256 hashing object.
            using var sha = SHA256.Create();

             // Compute the 32-byte SHA-256 digest from the raw image bytes.
            byte[] hashBytes = sha.ComputeHash(imageBytes);

            // Convert the raw hash bytes into a readable hexadecimal string.
            // Store as uppercase hex text.
            return Convert.ToHexString(hashBytes);
        }

        private static long ComputePerceptualHash(byte[] imageBytes)
        {
            // Load the image from the byte array using ImageSharp.
            using Image<Rgba32> image = Image.Load<Rgba32>(imageBytes);

            // Normalize the image before hashing.
            // This reduces irrelevant differences between files.
            //
            // Resize to 32x32:
            // - makes every image the same dimensions
            //
            // Convert to grayscale:
            // - removes color differences
            // - pHash focuses more on structure than exact color
            image.Mutate(ctx =>
            {
                ctx.Resize(PHashImageSize, PHashImageSize);
                ctx.Grayscale();
            });

            // Convert the resized grayscale image into a 2D double array.
            // Since it's grayscale, R/G/B are effectively the same instead of 0..255.
            // We subtract 128 to center brightness values around 0,
            // which is common for DCT-based transforms.
            // Build a 32x32 brightness matrix.
            double[,] pixels = new double[PHashImageSize, PHashImageSize];

            for (int y = 0; y < PHashImageSize; y++)
            {
                for (int x = 0; x < PHashImageSize; x++)
                {
                    Rgba32 pixel = image[x, y];

                    // Since image is grayscale now, R is enough.
                    pixels[x, y] = pixel.R - 128.0;
                }
            }

            // Apply a 2D Discrete Cosine Transform (DCT) to the pixel matrix.
            //
            // DCT changes the data from the spatial domain (pixel positions)
            // into the frequency domain.
            //
            // In simple terms:
            // - low frequencies capture broad shapes / overall structure
            // - high frequencies capture fine detail / noise
            double[,] dct = Apply2DDct(pixels);

            // Collect the top-left 8x8 portion of the DCT matrix.
            // This region contains the low-frequency information,
            // which is more stable for perceptual hashing.
            List<double> lowFrequencyValues = new();

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    // Skip the DC coefficient at (0,0).
                    // That value represents overall average brightness,
                    // and it can dominate the others.
                    if (x == 0 && y == 0)
                        continue;

                    lowFrequencyValues.Add(dct[x, y]);
                }
            }

            // Compute the median of the low-frequency coefficients.
            // This median becomes the threshold for turning coefficients into bits.
            double median = ComputeMedian(lowFrequencyValues);

            // Build a 64-bit hash from the 8x8 low-frequency block.
            //
            // If a coefficient > median, set that bit to 1.
            // Otherwise leave it as 0.
            //
            // This captures the relative pattern of the image
            // instead of exact numeric pixel values.
            ulong hash = 0;
            int bitIndex = 0;

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    bool bitIsSet;

                    if (x == 0 && y == 0)
                    {
                        // We force the DC coefficient bit to 0.
                        // This keeps the implementation simple and avoids
                        // brightness dominating the hash.
                        bitIsSet = false;
                    }
                    else
                    {
                        // Compare this coefficient to the median threshold.
                        bitIsSet = dct[x, y] > median;
                    }

                    if (bitIsSet)
                    {
                        // If this bit should be 1, set the corresponding position
                        // inside the 64-bit unsigned integer.
                        hash |= 1UL << bitIndex;
                    }

                    bitIndex++;
                }
            }

            // SQL Server bigint maps naturally to long.
            // SQL Server stores bigint as signed 64-bit (long),
            // so we reinterpret the ulong bits as long.
            return unchecked((long)hash);
        }

        private static double[,] Apply2DDct(double[,] input)
        {
            int n = PHashImageSize;

            // Output matrix will hold the DCT coefficients.
            double[,] output = new double[n, n];

            // u and v represent output frequency coordinates.
            for (int u = 0; u < n; u++)
            {
                for (int v = 0; v < n; v++)
                {
                    double sum = 0.0;

                    // x and y iterate through the input pixel matrix.
                    // This computes the standard 2D DCT formula.
                    for (int x = 0; x < n; x++)
                    {
                        for (int y = 0; y < n; y++)
                        {
                            sum += input[x, y]
                                * Math.Cos(((2 * x + 1) * u * Math.PI) / (2 * n))
                                * Math.Cos(((2 * y + 1) * v * Math.PI) / (2 * n));
                        }
                    }

                    // Normalization factors:
                    // The first row/column of DCT uses a different scale factor.
                    double alphaU = (u == 0) ? Math.Sqrt(1.0 / n) : Math.Sqrt(2.0 / n);
                    double alphaV = (v == 0) ? Math.Sqrt(1.0 / n) : Math.Sqrt(2.0 / n);

                    // Store the normalized DCT coefficient.
                    output[u, v] = alphaU * alphaV * sum;
                }
            }

            return output;
        }

        private static double ComputeMedian(List<double> values)
        {
            // Defensive check: median requires at least one value.
            if (values.Count == 0)
                throw new InvalidOperationException("Cannot compute median of an empty list.");

            // Sort the values from smallest to largest.
            var ordered = values.OrderBy(v => v).ToList();
            int middle = ordered.Count / 2;

            // If there is an even number of values,
            // median = average of the two middle values.
            if (ordered.Count % 2 == 0)
            {
                return (ordered[middle - 1] + ordered[middle]) / 2.0;
            }

            // If odd, median is just the middle value.
            return ordered[middle];
        }
    }
}