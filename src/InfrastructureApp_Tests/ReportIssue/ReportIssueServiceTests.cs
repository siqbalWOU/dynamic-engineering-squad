/* test for business logic that sits between your controller and EF Core */

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using InfrastructureApp.Services.ContentModeration;
using NUnit.Framework;
using System.Threading;
using InfrastructureApp.Services.ImageHashing;
using System.Collections.Generic;
using InfrastructureApp.Services.ImageSeverity;

namespace InfrastructureApp_Tests
{
    [TestFixture]
    public class ReportIssueServiceTests
    {
        private SqliteConnection _conn = null!;
        private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
        private string _webRoot = null!;
        private IImageModerationService _imageModerationService = null!;
        private IImageSeverityEstimationService _imageSeverityEstimationService = null!;
        private IHttpContextAccessor _httpContextAccessor = null!;

        [SetUp]
        public void SetUp()
        {
            // SQLite in-memory: keep connection open for lifetime of the test
            _conn = new SqliteConnection("Filename=:memory:");
            _conn.Open();

            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_conn)
                .Options;

            // temp web root for file save tests
            _webRoot = Path.Combine(Path.GetTempPath(), "InfraAppTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_webRoot);

            //setup for image moderation
            _imageModerationService = Substitute.For<IImageModerationService>();
            _imageSeverityEstimationService = Substitute.For<IImageSeverityEstimationService>();
            _httpContextAccessor = Substitute.For<IHttpContextAccessor>();

            _imageModerationService
                .ModerateImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(ImageModerationResult.Failed("Test default")));

            _imageSeverityEstimationService
                .EstimateSeverityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(SeverityEstimationResult.Failed("Test default")));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost:5001");
            _httpContextAccessor.HttpContext.Returns(httpContext);

            // create schema
            using var db = new ApplicationDbContext(_dbOptions);
            db.Database.EnsureCreated();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_webRoot))
                    Directory.Delete(_webRoot, recursive: true);
            }
            catch { /* ignore cleanup failures */ }

            _conn?.Dispose();
        }

        private ApplicationDbContext NewDb() => new ApplicationDbContext(_dbOptions);

        private static async Task AddUserAsync(ApplicationDbContext db, string userId, string? userName = null)
        {
            if (await db.Users.AnyAsync(u => u.Id == userId))
            {
                return;
            }

            db.Users.Add(new Users
            {
                Id = userId,
                UserName = userName ?? userId,
                NormalizedUserName = (userName ?? userId).ToUpperInvariant(),
                Email = $"{userId}@test.local",
                NormalizedEmail = $"{userId}@test.local".ToUpperInvariant()
            });

            await db.SaveChangesAsync();
        }

        private static IFormFile MakeFormFile(byte[] bytes, string fileName, string contentType = "image/png")
        {
            var ms = new MemoryStream(bytes);
            return new FormFile(ms, 0, bytes.Length, "Photo", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        //makeService now includes description moderation and image hash
        private ReportIssueService MakeService(
        ApplicationDbContext db,
        IContentModerationService? moderationOverride = null,
        IImageHashService? imageHashOverride = null,
        IImageModerationService? imageModerationOverride = null,
        IImageSeverityEstimationService? imageSeverityOverride = null,
        IHttpContextAccessor? httpContextAccessorOverride = null)
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.WebRootPath.Returns(_webRoot);

        IReportIssueRepository repo = new TestReportIssueRepository(db);

        IContentModerationService moderation;
        if (moderationOverride != null)
        {
            moderation = moderationOverride;
        }
        else
        {
            moderation = Substitute.For<IContentModerationService>();
            moderation.CheckAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(
                    new ContentModerationResult(
                        Performed: true,
                        IsAllowed: true,
                        Flagged: false)));
        }

        IImageHashService imageHash;
        if (imageHashOverride != null)
        {
            imageHash = imageHashOverride;
        }
        else
        {
            imageHash = Substitute.For<IImageHashService>();
            imageHash.ComputeHashesAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new ImageHashResult(
                    "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                    123456789L)));

            imageHash.HammingDistance(Arg.Any<long>(), Arg.Any<long>())
                .Returns(32);
        }

        var imageModeration = imageModerationOverride ?? _imageModerationService;
        var imageSeverity = imageSeverityOverride ?? _imageSeverityEstimationService;
        var httpContextAccessor = httpContextAccessorOverride ?? _httpContextAccessor;

        return new ReportIssueService(
            db,
            repo,
            env,
            moderation,
            imageHash,
            imageModeration,
            imageSeverity,
            httpContextAccessor);
    }

        // -------
        // Tests
        // -------

        //creates user report and assigns points
        [Test]
        public async Task CreateAsync_NoExistingUserPoints_CreatesReport_Adds10Points()
        {
            using var db = NewDb();
            var service = MakeService(db);
            var userId = "user-1";
            await AddUserAsync(db, userId);

            var report = new ReportIssue
            {
                Description = "Pothole on Main",
                Latitude = 44.85m,
                Longitude = -123.23m,
                Photo = null
            };

            var (reportId, status) = await service.CreateAsync(report, userId);
            Assert.That(status, Is.EqualTo("Approved"));

            // verify report saved (DbSet name is ReportIssue)
            var savedReport = await db.ReportIssue.FirstOrDefaultAsync(r => r.Id == reportId);
            Assert.That(savedReport, Is.Not.Null);
            Assert.That(savedReport!.UserId, Is.EqualTo(userId));
            Assert.That(savedReport.Description, Is.EqualTo("Pothole on Main"));
            Assert.That(savedReport.Status, Is.EqualTo("Approved"));
            Assert.That(savedReport.ImageUrl, Is.Null);

            // verify points created + updated
            var points = await db.UserPoints.FirstOrDefaultAsync(p => p.UserId == userId);
            Assert.That(points, Is.Not.Null);
            Assert.That(points!.CurrentPoints, Is.EqualTo(10));
            Assert.That(points.LifetimePoints, Is.EqualTo(10));
        }

        //if user already has points, it should add +10 points
        [Test]
        public async Task CreateAsync_ExistingUserPoints_IncrementsBy10()
        {
            using var db = NewDb();
            await AddUserAsync(db, "user-2");

            db.UserPoints.Add(new UserPoints
            {
                UserId = "user-2",
                CurrentPoints = 5,
                LifetimePoints = 50,
                LastUpdated = DateTime.UtcNow.AddDays(-1)
            });
            await db.SaveChangesAsync();

            var service = MakeService(db);

            var report = new ReportIssue
            {
                Description = "Streetlight out",
                Photo = null
            };

            var (reportId, status) = await service.CreateAsync(report, "user-2");

            Assert.That(reportId, Is.GreaterThan(0));
            Assert.That(status, Is.EqualTo("Approved"));

            var points = await db.UserPoints.SingleAsync(p => p.UserId == "user-2");
            Assert.That(points.CurrentPoints, Is.EqualTo(15));
            Assert.That(points.LifetimePoints, Is.EqualTo(60));
        }

        //when valid image is provided, file is saved in wwwroot/uploads/issues and imageurl is set
        [Test]
        public async Task CreateAsync_WithValidPhoto_SavesFile_SetsImageUrl_AndHashes()
        {
            using var db = NewDb();
            await AddUserAsync(db, "user-3");

            var imageHash = Substitute.For<IImageHashService>();
            imageHash.ComputeHashesAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new ImageHashResult(
                    Sha256: "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                    PHash: 987654321L)));

            imageHash.HammingDistance(Arg.Any<long>(), Arg.Any<long>())
                .Returns(32);

            var service = MakeService(db, imageHashOverride: imageHash);

            var bytes = new byte[] { 1, 2, 3, 4, 5 };
            var report = new ReportIssue
            {
                Description = "Cracked sidewalk",
                Photo = MakeFormFile(bytes, "sidewalk.png", "image/png")
            };

            var (reportId, status) = await service.CreateAsync(report, "user-3");
            Assert.That(status, Is.EqualTo("Approved"));

            var savedReport = await db.ReportIssue.SingleAsync(r => r.Id == reportId);

            Assert.That(savedReport.ImageUrl, Is.Not.Null);
            Assert.That(savedReport.ImageUrl, Does.StartWith("/uploads/issues/"));
            Assert.That(savedReport.ImageUrl, Does.EndWith(".png"));

            // NEW: verify hashes saved
            Assert.That(savedReport.ImageSha256,
                Is.EqualTo("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));
            Assert.That(savedReport.ImagePHash, Is.EqualTo(987654321L));

            var uploadsDir = Path.Combine(_webRoot, "uploads", "issues");
            Assert.That(Directory.Exists(uploadsDir), Is.True);

            var files = Directory.GetFiles(uploadsDir);
            Assert.That(files.Length, Is.EqualTo(1));
            Assert.That(Path.GetExtension(files[0]).ToLowerInvariant(), Is.EqualTo(".png"));
        }

        //this tests to see if images are exact duplicates
        [Test]
        public void CreateAsync_ExactDuplicateImageForSameUser_ThrowsDuplicateImageException()
        {
            using var db = NewDb();
            AddUserAsync(db, "user-dup").GetAwaiter().GetResult();

            db.ReportIssue.Add(new ReportIssue
            {
                Description = "Existing report",
                Status = "Approved",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UserId = "user-dup",
                Latitude = 44.85m,
                Longitude = -123.23m,
                ImageUrl = "/uploads/issues/existing.png",
                ImageSha256 = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
                ImagePHash = 111111111L
            });
            db.SaveChanges();

            var imageHash = Substitute.For<IImageHashService>();
            imageHash.ComputeHashesAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new ImageHashResult(
                    Sha256: "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
                    PHash: 999999999L)));

            imageHash.HammingDistance(Arg.Any<long>(), Arg.Any<long>())
                .Returns(32);

            var service = MakeService(db, imageHashOverride: imageHash);

            var report = new ReportIssue
            {
                Description = "Trying same image again",
                Photo = MakeFormFile(new byte[] { 9, 8, 7 }, "dup.png", "image/png"),
                Latitude = 44.85m,
                Longitude = -123.23m
            };

            var ex = Assert.ThrowsAsync<DuplicateImageException>(async () =>
                await service.CreateAsync(report, "user-dup"));

            Assert.That(ex!.Message, Does.Contain("already used this image"));

            Assert.That(db.ReportIssue.Count(r => r.UserId == "user-dup"), Is.EqualTo(1));
            Assert.That(db.UserPoints.Any(p => p.UserId == "user-dup"), Is.False);
        }

        //this tests the pHash and Hamming distance to see if images are similar
        [Test]
        public void CreateAsync_VisuallySimilarImageForSameUser_ThrowsDuplicateImageException()
        {
            using var db = NewDb();
            AddUserAsync(db, "user-phash").GetAwaiter().GetResult();

            db.ReportIssue.Add(new ReportIssue
            {
                Description = "Previous report",
                Status = "Approved",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UserId = "user-phash",
                Latitude = 44.85m,
                Longitude = -123.23m,
                ImageUrl = "/uploads/issues/old.png",
                ImageSha256 = "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd",
                ImagePHash = 555555555L
            });
            db.SaveChanges();

            var imageHash = Substitute.For<IImageHashService>();
            imageHash.ComputeHashesAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new ImageHashResult(
                    Sha256: "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee",
                    PHash: 777777777L)));

            // Force similarity by returning <= threshold (threshold is 8)
            imageHash.HammingDistance(777777777L, 555555555L).Returns(4);

            var service = MakeService(db, imageHashOverride: imageHash);

            var report = new ReportIssue
            {
                Description = "Looks too similar",
                Photo = MakeFormFile(new byte[] { 4, 5, 6 }, "similar.png", "image/png"),
                Latitude = 44.85m,
                Longitude = -123.23m
            };

            var ex = Assert.ThrowsAsync<DuplicateImageException>(async () =>
                await service.CreateAsync(report, "user-phash"));

            Assert.That(ex!.Message, Does.Contain("looks too similar"));

            Assert.That(db.ReportIssue.Count(r => r.UserId == "user-phash"), Is.EqualTo(1));
            Assert.That(db.UserPoints.Any(p => p.UserId == "user-phash"), Is.False);
        }

        //tests to see if duplicates are blocked by the same user
        [Test]
        public async Task CreateAsync_SameExactImageDifferentUser_IsAllowed()
        {
            using var db = NewDb();
            await AddUserAsync(db, "user-a");
            await AddUserAsync(db, "user-b");

            db.ReportIssue.Add(new ReportIssue
            {
                Description = "Existing report",
                Status = "Approved",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UserId = "user-a",
                Latitude = 44.85m,
                Longitude = -123.23m,
                ImageUrl = "/uploads/issues/existing.png",
                ImageSha256 = "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
                ImagePHash = 123123123L
            });
            db.SaveChanges();

            var imageHash = Substitute.For<IImageHashService>();
            imageHash.ComputeHashesAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new ImageHashResult(
                    Sha256: "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
                    PHash: 123123123L)));

            imageHash.HammingDistance(Arg.Any<long>(), Arg.Any<long>())
                .Returns(0);

            var service = MakeService(db, imageHashOverride: imageHash);

            var report = new ReportIssue
            {
                Description = "Same image but different user",
                Photo = MakeFormFile(new byte[] { 1, 1, 1 }, "same.png", "image/png"),
                Latitude = 44.85m,
                Longitude = -123.23m
            };

            var (reportId, status) = await service.CreateAsync(report, "user-b");

            Assert.That(reportId, Is.GreaterThan(0));
            Assert.That(status, Is.EqualTo("Approved"));

            var saved = await db.ReportIssue.SingleAsync(r => r.Id == reportId);
            Assert.That(saved.UserId, Is.EqualTo("user-b"));
        }

        //Reject unsupported formats (like GIF) and ensure it’s all-or-nothing (no partial saves).
        [Test]
        public void CreateAsync_InvalidExtension_Throws_AndDoesNotCreatePointsOrReport()
        {
            using var db = NewDb();
            AddUserAsync(db, "user-4").GetAwaiter().GetResult();
            var service = MakeService(db);

            var report = new ReportIssue
            {
                Description = "Bad file",
                Photo = MakeFormFile(new byte[] { 1, 2, 3 }, "evil.gif", "image/gif")
            };

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.CreateAsync(report, "user-4"));

            Assert.That(ex!.Message, Does.Contain("Only JPG, PNG, or WEBP"));

            // Should not have created points or reports
            Assert.That(db.UserPoints.Any(p => p.UserId == "user-4"), Is.False);
            Assert.That(db.ReportIssue.Any(r => r.UserId == "user-4"), Is.False);
        }

        //Reject large files (> 5MB) and again ensure no partial saves.
        [Test]
        public void CreateAsync_Over5MB_Throws_AndDoesNotCreatePointsOrReport()
        {
            using var db = NewDb();
            AddUserAsync(db, "user-5").GetAwaiter().GetResult();
            var service = MakeService(db);

            var big = new byte[5 * 1024 * 1024 + 1]; // 5MB + 1
            var report = new ReportIssue
            {
                Description = "Too big",
                Photo = MakeFormFile(big, "big.jpg", "image/jpeg")
            };

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.CreateAsync(report, "user-5"));

            Assert.That(ex!.Message, Does.Contain("5MB or smaller"));

            Assert.That(db.ReportIssue.Any(r => r.UserId == "user-5"), Is.False);
            Assert.That(db.UserPoints.Any(p => p.UserId == "user-5"), Is.False);
        }

        //tests moderation blocking if user submits vulgar description
        [Test]
        public void CreateAsync_WhenModerationRejects_ThrowsAndDoesNotSaveReportOrPoints()
        {
            using var db = NewDb();
            AddUserAsync(db, "user-mod").GetAwaiter().GetResult();

            var env = Substitute.For<IWebHostEnvironment>();
            env.WebRootPath.Returns(_webRoot);

            IReportIssueRepository repo = new TestReportIssueRepository(db);

            var moderation = Substitute.For<IContentModerationService>();
            moderation.CheckAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(new ContentModerationResult(Performed: true, IsAllowed: false, Flagged: true, Reason: "hate")));

            var imageHash = Substitute.For<IImageHashService>();
            imageHash.ComputeHashesAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new ImageHashResult(
                    Sha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                    PHash: 123456789L)));
            imageHash.HammingDistance(Arg.Any<long>(), Arg.Any<long>())
                .Returns(32);

            var service = new ReportIssueService(db, repo, env, moderation, imageHash, _imageModerationService, _imageSeverityEstimationService, _httpContextAccessor);

            var report = new ReportIssue
            {
                Description = "bad content",
                Photo = null
            };

            Assert.ThrowsAsync<ContentModerationRejectedException>(() => service.CreateAsync(report, "user-mod"));

            Assert.That(db.ReportIssue.Any(r => r.UserId == "user-mod"), Is.False);
            Assert.That(db.UserPoints.Any(p => p.UserId == "user-mod"), Is.False);
        }

        //verifies image moderation passes, severity succeeds, severity field saves
        [Test]
        public async Task CreateAsync_WhenImageModerationPasses_AndSeveritySucceeds_SavesSeverityFields()
        {
            using var db = NewDb();
            await AddUserAsync(db, "severity-user-1");

            var imageModeration = Substitute.For<IImageModerationService>();
            imageModeration
                .ModerateImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(ImageModerationResult.Passed("Image is valid for analysis")));

            var imageSeverity = Substitute.For<IImageSeverityEstimationService>();
            imageSeverity
                .EstimateSeverityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(
                    SeverityEstimationResult.Success("High", "Large pothole with deep visible cracking.")));

            var service = MakeService(
                db,
                imageModerationOverride: imageModeration,
                imageSeverityOverride: imageSeverity);

            var report = new ReportIssue
            {
                Description = "Road damage near curb",
                Photo = MakeFormFile(new byte[] { 1, 2, 3, 4, 5 }, "damage.png", "image/png"),
                Latitude = 44.85m,
                Longitude = -123.23m
            };

            var (reportId, status) = await service.CreateAsync(report, "severity-user-1");

            Assert.That(status, Is.EqualTo("Approved"));

            var saved = await db.ReportIssue.SingleAsync(r => r.Id == reportId);

            Assert.That(saved.SeverityStatus, Is.EqualTo("High"));
            Assert.That(saved.SeverityReason, Is.EqualTo("Large pothole with deep visible cracking."));

            await imageModeration.Received(1)
                .ModerateImageAsync(Arg.Is<string>(s => s.StartsWith("data:image/png;base64,")), Arg.Any<CancellationToken>());

            await imageSeverity.Received(1)
                .EstimateSeverityAsync(Arg.Is<string>(s => s.StartsWith("data:image/png;base64,")), Arg.Any<CancellationToken>());
        }


        //tests if image moderation passes but severity estimation fails, severity stays pending
        [Test]
        public async Task CreateAsync_WhenSeverityEstimationFails_LeavesSeverityAsPending()
        {
            using var db = NewDb();
            await AddUserAsync(db, "severity-user-2");

            var imageModeration = Substitute.For<IImageModerationService>();
            imageModeration
                .ModerateImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(ImageModerationResult.Passed("Image is valid for analysis")));

            var imageSeverity = Substitute.For<IImageSeverityEstimationService>();
            imageSeverity
                .EstimateSeverityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(
                    SeverityEstimationResult.Failed("Estimator unavailable")));

            var service = MakeService(
                db,
                imageModerationOverride: imageModeration,
                imageSeverityOverride: imageSeverity);

            var report = new ReportIssue
            {
                Description = "Crack in sidewalk",
                Photo = MakeFormFile(new byte[] { 9, 8, 7, 6 }, "sidewalk.png", "image/png"),
                Latitude = 44.85m,
                Longitude = -123.23m
            };

            var (reportId, status) = await service.CreateAsync(report, "severity-user-2");

            Assert.That(status, Is.EqualTo("Approved"));

            var saved = await db.ReportIssue.SingleAsync(r => r.Id == reportId);

            Assert.That(saved.SeverityStatus, Is.EqualTo(ImageSeverityStatuses.Pending));
            Assert.That(saved.SeverityReason, Is.Null);
        }


        //tests to see if the image moderation fails
        [Test]
        public async Task CreateAsync_WhenImageModerationRejects_ThrowsAndDoesNotSaveReportOrPoints()
        {
            using var db = NewDb();
            await AddUserAsync(db, "severity-user-3");

            var imageModeration = Substitute.For<IImageModerationService>();
            imageModeration
                .ModerateImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(
                    ImageModerationResult.Rejected("Inappropriate image content")));

            var imageSeverity = Substitute.For<IImageSeverityEstimationService>();

            var service = MakeService(
                db,
                imageModerationOverride: imageModeration,
                imageSeverityOverride: imageSeverity);

            var report = new ReportIssue
            {
                Description = "Uploaded bad image",
                Photo = MakeFormFile(new byte[] { 1, 1, 1, 1 }, "bad.png", "image/png"),
                Latitude = 44.85m,
                Longitude = -123.23m
            };

            var ex = Assert.ThrowsAsync<ContentModerationRejectedException>(async () =>
                await service.CreateAsync(report, "severity-user-3"));

            Assert.That(ex!.Message, Does.Contain("uploaded image contains inappropriate content"));

            Assert.That(db.ReportIssue.Any(r => r.UserId == "severity-user-3"), Is.False);
            Assert.That(db.UserPoints.Any(p => p.UserId == "severity-user-3"), Is.False);

            await imageSeverity.DidNotReceive()
                .EstimateSeverityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        //image moderation fails, severity estimation fails, pending status
        [Test]
        public async Task CreateAsync_WhenImageModerationFails_SkipsSeverityEstimation_AndLeavesPending()
        {
            using var db = NewDb();
            await AddUserAsync(db, "severity-user-4");

            var imageModeration = Substitute.For<IImageModerationService>();
            imageModeration
                .ModerateImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(
                    ImageModerationResult.Failed("Moderation service unavailable")));

            var imageSeverity = Substitute.For<IImageSeverityEstimationService>();

            var service = MakeService(
                db,
                imageModerationOverride: imageModeration,
                imageSeverityOverride: imageSeverity);

            var report = new ReportIssue
            {
                Description = "Damaged sign",
                Photo = MakeFormFile(new byte[] { 5, 4, 3, 2 }, "sign.png", "image/png"),
                Latitude = 44.85m,
                Longitude = -123.23m
            };

            var (reportId, status) = await service.CreateAsync(report, "severity-user-4");

            Assert.That(status, Is.EqualTo("Approved"));

            var saved = await db.ReportIssue.SingleAsync(r => r.Id == reportId);

            Assert.That(saved.SeverityStatus, Is.EqualTo(ImageSeverityStatuses.Pending));
            Assert.That(saved.SeverityReason, Is.Null);

            await imageSeverity.DidNotReceive()
                .EstimateSeverityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }


        //tests to see the base64 data URL behavior
        [Test]
        public async Task CreateAsync_WithJpgPhoto_BuildsJpegBase64DataUrl_ForModerationAndSeverity()
        {
            using var db = NewDb();
            await AddUserAsync(db, "severity-user-5");

            string? moderationArg = null;
            string? severityArg = null;

            var imageModeration = Substitute.For<IImageModerationService>();
            imageModeration
                .ModerateImageAsync(Arg.Do<string>(s => moderationArg = s), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(ImageModerationResult.Passed("OK")));

            var imageSeverity = Substitute.For<IImageSeverityEstimationService>();
            imageSeverity
                .EstimateSeverityAsync(Arg.Do<string>(s => severityArg = s), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(
                    SeverityEstimationResult.Success("Low", "Minor visible wear.")));

            var service = MakeService(
                db,
                imageModerationOverride: imageModeration,
                imageSeverityOverride: imageSeverity);

            var report = new ReportIssue
            {
                Description = "Minor issue",
                Photo = MakeFormFile(new byte[] { 10, 20, 30, 40 }, "photo.jpg", "image/jpeg"),
                Latitude = 44.85m,
                Longitude = -123.23m
            };

            await service.CreateAsync(report, "severity-user-5");

            Assert.That(moderationArg, Is.Not.Null);
            Assert.That(moderationArg, Does.StartWith("data:image/jpeg;base64,"));

            Assert.That(severityArg, Is.Not.Null);
            Assert.That(severityArg, Does.StartWith("data:image/jpeg;base64,"));
        }


        // ----------------
        // Test repository 
        // ----------------

        /* This is a lightweight repository that satisfies IReportIssueRepository interface, but directly uses EF Core:
        AddAsync adds to db.ReportIssue
        GetByIdAsync queries db.ReportIssue
        SaveChangesAsync calls db.SaveChangesAsync()
        It’s basically there so the service can be constructed without needing your real repository implementation (or to keep the tests focused on service logic). */
        private class TestReportIssueRepository : IReportIssueRepository
        {
            private readonly ApplicationDbContext _db;

            public TestReportIssueRepository(ApplicationDbContext db) => _db = db;

            public Task<ReportIssue?> GetByIdAsync(int id)
                => _db.ReportIssue.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == id);

            public Task AddAsync(ReportIssue report)
                => _db.ReportIssue.AddAsync(report).AsTask();

            public Task SaveChangesAsync()
                => _db.SaveChangesAsync();

            public async Task<List<ReportIssue>> GetLatestReportsAsync(bool isAdmin)
            {
                var query = _db.ReportIssue.AsQueryable();

                if (!isAdmin)
                    query = query.Where(r => r.Status == "Approved");

                return await query
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }

            // Feature83 + SCRUM-86 updated signature.
            // Test-only repository used by NUnit tests.
            // Simulates the real repository search + sorting behavior using in-memory test data.
            public async Task<List<ReportIssue>> SearchLatestReportsAsync(bool isAdmin, string? keyword, string? sort)
            {
                // Use in-memory reports from test database
                var query = _db.ReportIssue.AsQueryable();

                // Apply same visibility rule as real app (non-admin sees only Approved)
                if (!isAdmin)
                {
                    query = query.Where(r => r.Status == "Approved");
                }

                // Apply keyword search filter like real repository search
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.Trim(); // SCRUM-86 ADDED: handles accidental whitespace
                    query = query.Where(r => r.Description != null && r.Description.Contains(keyword));
                }

                // SCRUM-86 ADDED: apply newest/oldest sort (default newest)
                if (!string.IsNullOrWhiteSpace(sort) && sort.Trim().ToLower() == "oldest")
                {
                    query = query.OrderBy(r => r.CreatedAt);
                }
                else
                {
                    query = query.OrderByDescending(r => r.CreatedAt);
                }

                return await query.ToListAsync();
            }

            public async Task<List<ReportIssue>> GetResolvedReportsAsync()
            {
                return await _db.ReportIssue
                    .Where(r => r.Status == "Resolved")
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
        }
    }
}
