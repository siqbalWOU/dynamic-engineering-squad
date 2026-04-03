//These tests check that:

// null stream throws the correct exception

// the same image gives the same SHA-256 and pHash

// different images give different SHA-256 values

// the service resets a seekable stream before reading it

// HammingDistance returns 0 for identical hashes

// HammingDistance returns the expected number of different bits

// HammingDistance is symmetric

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using InfrastructureApp.Services.ImageHashing;
using NUnit.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace InfrastructureApp_Tests.Services.ImageHashing
{
    [TestFixture]
    public class ImageHashServiceTests
    {
        private ImageHashService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _service = new ImageHashService();
        }

        // null stream throws the correct exception
        [Test]
        public void ComputeHashesAsync_NullStream_ThrowsArgumentNullException()
        {
            // Act + Assert
            var ex = Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _service.ComputeHashesAsync(null!));

            Assert.That(ex!.ParamName, Is.EqualTo("imageStream"));
        }

        // the same image gives the same SHA-256 and pHash
        [Test]
        public async Task ComputeHashesAsync_SameImageTwice_ReturnsSameSha256AndPHash()
        {
            // Arrange
            byte[] imageBytes = CreateSolidColorPngBytes(32, 32, new Rgba32(255, 0, 0));

            await using var stream1 = new MemoryStream(imageBytes);
            await using var stream2 = new MemoryStream(imageBytes);

            // Act
            ImageHashResult result1 = await _service.ComputeHashesAsync(stream1);
            ImageHashResult result2 = await _service.ComputeHashesAsync(stream2);

            // Assert
            Assert.That(result1.Sha256, Is.EqualTo(result2.Sha256));
            Assert.That(result1.PHash, Is.EqualTo(result2.PHash));
        }

        // different images give different SHA-256 values
        [Test]
        public async Task ComputeHashesAsync_DifferentImages_ReturnsDifferentSha256()
        {
            // Arrange
            byte[] image1 = CreateSolidColorPngBytes(32, 32, new Rgba32(255, 0, 0));
            byte[] image2 = CreateSolidColorPngBytes(32, 32, new Rgba32(0, 0, 255));

            await using var stream1 = new MemoryStream(image1);
            await using var stream2 = new MemoryStream(image2);

            // Act
            ImageHashResult result1 = await _service.ComputeHashesAsync(stream1);
            ImageHashResult result2 = await _service.ComputeHashesAsync(stream2);

            // Assert
            Assert.That(result1.Sha256, Is.Not.EqualTo(result2.Sha256));
        }

        // the service resets a seekable stream before reading it
        [Test]
        public async Task ComputeHashesAsync_WhenSeekableStreamIsNotAtBeginning_StillComputesHashes()
        {
            // Arrange
            byte[] imageBytes = CreateSolidColorPngBytes(32, 32, new Rgba32(0, 255, 0));
            await using var stream = new MemoryStream(imageBytes);

            // Move the stream position away from the start on purpose
            stream.Position = stream.Length;

            // Act
            ImageHashResult result = await _service.ComputeHashesAsync(stream);

            // Assert
            Assert.That(result.Sha256, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Sha256.Length, Is.EqualTo(64)); // SHA-256 hex string length
        }

        //returns a valid hash for original image
        [Test]
        public async Task ComputeHashesAsync_ValidImage_Returns64CharacterSha256()
        {
            // Arrange
            byte[] imageBytes = CreateSolidColorPngBytes(32, 32, new Rgba32(123, 45, 67));
            await using var stream = new MemoryStream(imageBytes);

            // Act
            ImageHashResult result = await _service.ComputeHashesAsync(stream);

            // Assert
            Assert.That(result.Sha256, Has.Length.EqualTo(64));
        }

        //returns a valid hash for original image//returns a valid hash for original image
        [Test]
        public async Task ComputeHashesAsync_ValidImage_ReturnsImageHashResult()
        {
            // Arrange
            byte[] imageBytes = CreateSolidColorPngBytes(32, 32, new Rgba32(10, 20, 30));
            await using var stream = new MemoryStream(imageBytes);

            // Act
            ImageHashResult result = await _service.ComputeHashesAsync(stream);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Sha256, Is.Not.Null.And.Not.Empty);
            Assert.That(result.PHash, Is.TypeOf<long>());
        }

        // HammingDistance returns 0 for identical hashes
        [Test]
        public void HammingDistance_SameHashes_ReturnsZero()
        {
            // Arrange
            long hash = 0b10101010;

            // Act
            int distance = _service.HammingDistance(hash, hash);

            // Assert
            Assert.That(distance, Is.EqualTo(0));
        }

        [Test]
        public void HammingDistance_DifferentHashes_ReturnsExpectedBitCount()
        {
            // Arrange
            long left  = 0b10101010;
            long right = 0b11110000;

            // XOR = 01011010, which has 4 bits set
            // 0b01011010 => 4 ones

            // Act
            int distance = _service.HammingDistance(left, right);

            // Assert
            Assert.That(distance, Is.EqualTo(4));
        }

        // HammingDistance is symmetric
        [Test]
        public void HammingDistance_IsSymmetric()
        {
            // Arrange
            long left = 123456789;
            long right = 987654321;

            // Act
            int d1 = _service.HammingDistance(left, right);
            int d2 = _service.HammingDistance(right, left);

            // Assert
            Assert.That(d1, Is.EqualTo(d2));
        }

        // HammingDistance returns the expected number of different bits
        [Test]
        public void HammingDistance_CompletelyOppositeBits_ReturnsExpectedCount()
        {
            // Arrange
            long left = 0L;
            long right = -1L; // all 64 bits set in two's complement

            // Act
            int distance = _service.HammingDistance(left, right);

            // Assert
            Assert.That(distance, Is.EqualTo(64));
        }

        private static byte[] CreateSolidColorPngBytes(int width, int height, Rgba32 color)
        {
            using var image = new Image<Rgba32>(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    image[x, y] = color;
                }
            }

            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return ms.ToArray();
        }
    }
}