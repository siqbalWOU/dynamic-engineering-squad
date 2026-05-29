using System;
using System.Linq;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services;
using InfrastructureApp_Tests.Helpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Services
{
    [TestFixture]
    public class NearbyIssueServiceTests
    {
        private LinkGenerator _links = null!;

        [SetUp]
        public void SetUp()
        {
            _links = Substitute.For<LinkGenerator>();
        }

        private static ApplicationDbContext BuildDb(out SqliteConnection conn)
        {
            conn = new SqliteConnection("Filename=:memory:");
            conn.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(conn)
                .Options;

            var db = new ApplicationDbContext(options);
            db.Database.EnsureCreated();

            return db;
        }

        private static InfrastructureApp.Models.ReportIssue Issue(
            int id,
            decimal? lat,
            decimal? lng,
            string userId,
            string status = "Approved",
            DateTime? createdAt = null)
        {
            return new InfrastructureApp.Models.ReportIssue
            {
                Id = id,
                Status = status,
                CreatedAt = createdAt ?? DateTime.UtcNow,
                Latitude = lat,
                Longitude = lng,
                UserId = userId
            };
        }

        [Test]
        public async Task GetNearbyIssuesAsync_FiltersOutReportsWithNullCoordinates()
        {
            using var db = BuildDb(out var conn);
            using var connection = conn;

            var service = new NearbyIssueService(db, _links);
            await ReportIssueTestDataHelper.EnsureTestUserAsync(db, "nearby-user");

            db.ReportIssue.AddRange(
                Issue(1, null, -123.23m, "nearby-user"),
                Issue(2, 44.84m, null, "nearby-user"),
                Issue(3, 44.84m, -123.23m, "nearby-user"));
            await db.SaveChangesAsync();

            var results = await service.GetNearbyIssuesAsync(44.84, -123.23, 5);

            Assert.That(results.Select(r => r.Id), Is.EquivalentTo(new[] { 3 }));
        }

        [Test]
        public async Task GetNearbyIssuesAsync_RadiusDefaultsTo5_WhenRadiusIsInvalid()
        {
            using var db = BuildDb(out var conn);
            using var connection = conn;

            var service = new NearbyIssueService(db, _links);
            await ReportIssueTestDataHelper.EnsureTestUserAsync(db, "nearby-user");

            db.ReportIssue.Add(Issue(1, 44.84m, -123.23m, "nearby-user"));
            await db.SaveChangesAsync();

            var resultsInvalidLow = await service.GetNearbyIssuesAsync(44.84, -123.23, 0);
            var resultsInvalidHigh = await service.GetNearbyIssuesAsync(44.84, -123.23, 500);
            var resultsDefault = await service.GetNearbyIssuesAsync(44.84, -123.23, 5);

            Assert.That(resultsInvalidLow.Select(x => x.Id), Is.EquivalentTo(resultsDefault.Select(x => x.Id)));
            Assert.That(resultsInvalidHigh.Select(x => x.Id), Is.EquivalentTo(resultsDefault.Select(x => x.Id)));
        }

        [Test]
        public async Task GetNearbyIssuesAsync_ComputesDistanceMiles_AndIncludesOnlyWithinRadius()
        {
            using var db = BuildDb(out var conn);
            using var connection = conn;

            var service = new NearbyIssueService(db, _links);
            await ReportIssueTestDataHelper.EnsureTestUserAsync(db, "nearby-user");

            const double qLat = 44.84;
            const double qLng = -123.23;

            db.ReportIssue.Add(Issue(1, (decimal)qLat, (decimal)qLng, "nearby-user"));
            db.ReportIssue.Add(Issue(2, 45.04m, -123.23m, "nearby-user"));
            await db.SaveChangesAsync();

            var within1Mile = await service.GetNearbyIssuesAsync(qLat, qLng, 1);
            Assert.That(within1Mile.Select(r => r.Id), Is.EquivalentTo(new[] { 1 }));
            Assert.That(within1Mile.Single().DistanceMiles, Is.Not.Null);
            Assert.That(within1Mile.Single().DistanceMiles!.Value, Is.LessThan(0.05));

            var within20Miles = await service.GetNearbyIssuesAsync(qLat, qLng, 20);
            Assert.That(within20Miles.Select(r => r.Id), Is.EquivalentTo(new[] { 1, 2 }));
            Assert.That(within20Miles.First(r => r.Id == 2).DistanceMiles, Is.GreaterThan(10.0));
        }

        [Test]
        public async Task GetNearbyIssuesAsync_SortsResultsByDistanceAscending()
        {
            using var db = BuildDb(out var conn);
            using var connection = conn;

            var service = new NearbyIssueService(db, _links);
            await ReportIssueTestDataHelper.EnsureTestUserAsync(db, "nearby-user");

            const double qLat = 44.84;
            const double qLng = -123.23;

            db.ReportIssue.AddRange(
                Issue(1, 44.841m, -123.23m, "nearby-user"),
                Issue(2, 44.850m, -123.23m, "nearby-user"),
                Issue(3, 44.900m, -123.23m, "nearby-user"));
            await db.SaveChangesAsync();

            var results = await service.GetNearbyIssuesAsync(qLat, qLng, 50);

            var distances = results.Select(r => r.DistanceMiles ?? double.MaxValue).ToList();
            Assert.That(distances, Is.EqualTo(distances.OrderBy(d => d).ToList()));
        }

        [Test]
        public async Task GetNearbyIssuesAsync_ReturnsAtMost300Results()
        {
            using var db = BuildDb(out var conn);
            using var connection = conn;

            var service = new NearbyIssueService(db, _links);
            await ReportIssueTestDataHelper.EnsureTestUserAsync(db, "nearby-user");

            for (int i = 1; i <= 350; i++)
            {
                db.ReportIssue.Add(Issue(i, 44.84m, -123.23m, "nearby-user"));
            }
            await db.SaveChangesAsync();

            var results = await service.GetNearbyIssuesAsync(44.84, -123.23, 5);

            Assert.That(results.Count, Is.EqualTo(300));
        }

        [Test]
        public async Task GetNearbyIssuesAsync_MapsFieldsCorrectlyIntoDto()
        {
            using var db = BuildDb(out var conn);
            using var connection = conn;

            var service = new NearbyIssueService(db, _links);
            await ReportIssueTestDataHelper.EnsureTestUserAsync(db, "nearby-user");

            var created = new DateTime(2026, 2, 24, 12, 0, 0, DateTimeKind.Utc);

            db.ReportIssue.Add(Issue(
                id: 99,
                lat: 44.84m,
                lng: -123.23m,
                userId: "nearby-user",
                status: "Approved",
                createdAt: created));
            await db.SaveChangesAsync();

            var results = await service.GetNearbyIssuesAsync(44.84, -123.23, 5);
            var dto = results.Single();

            Assert.That(dto.Id, Is.EqualTo(99));
            Assert.That(dto.Status, Is.EqualTo("Approved"));
            Assert.That(dto.CreatedAt, Is.EqualTo(created));
            Assert.That(dto.Latitude, Is.EqualTo(44.84).Within(0.000001));
            Assert.That(dto.Longitude, Is.EqualTo(-123.23).Within(0.000001));
            Assert.That(dto.DistanceMiles, Is.Not.Null);
            Assert.That(dto.DetailsUrl, Is.EqualTo("/ReportIssue/Details/99"));
        }

        [Test]
        public async Task GetNearbyIssuesAsync_DoesNotTrackEntities_FromQuery_AsNoTrackingIntended()
        {
            using var db = BuildDb(out var conn);
            using var connection = conn;

            var service = new NearbyIssueService(db, _links);
            await ReportIssueTestDataHelper.EnsureTestUserAsync(db, "nearby-user");

            db.ReportIssue.Add(Issue(1, 44.84m, -123.23m, "nearby-user"));
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();

            _ = await service.GetNearbyIssuesAsync(44.84, -123.23, 5);

            Assert.That(db.ChangeTracker.Entries().Count(), Is.EqualTo(0));
        }
    }
}
