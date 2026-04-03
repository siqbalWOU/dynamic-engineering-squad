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

namespace InfrastructureApp_Tests
{
    [TestFixture]
    public class ReportIssueServiceTests
    {
        private SqliteConnection _conn = null!;
        private DbContextOptions<ApplicationDbContext> _dbOptions = null!;
        private string _webRoot = null!;

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
            IImageHashService? imageHashOverride = null)
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

            return new ReportIssueService(db, repo, env, moderation, imageHash);
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

            var report = new ReportIssue
            {
                Description = "Pothole on Main",
                Latitude = 44.85m,
                Longitude = -123.23m,
                Photo = null
            };

            var userId = "user-1";

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

            var service = new ReportIssueService(db, repo, env, moderation, imageHash);

            var report = new ReportIssue
            {
                Description = "bad content",
                Photo = null
            };

            Assert.ThrowsAsync<ContentModerationRejectedException>(() => service.CreateAsync(report, "user-mod"));

            Assert.That(db.ReportIssue.Any(r => r.UserId == "user-mod"), Is.False);
            Assert.That(db.UserPoints.Any(p => p.UserId == "user-mod"), Is.False);
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
                => _db.ReportIssue.FirstOrDefaultAsync(r => r.Id == id);

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
        }
    }
}
