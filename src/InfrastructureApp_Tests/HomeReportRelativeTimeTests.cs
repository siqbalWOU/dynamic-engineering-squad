using InfrastructureApp.Models;
using NUnit.Framework;

//This file was created for SCRUM127. JAT
namespace InfrastructureApp_Tests
{
    [TestFixture]
    public class HomeReportRelativeTimeTests
    {
        // -------------------------------------------------------
        // TEST 1: Reports created less than one minute ago show "Just now"
        // -------------------------------------------------------
        [Test]
        public void BuildRelativeTime_LessThanOneMinuteAgo_ReturnsJustNow()
        {
            // Arrange: create a time 30 seconds ago
            var createdAt = DateTime.UtcNow.AddSeconds(-30);

            // Act: call method to get the relative time string
            var result = ReportIssue.BuildRelativeTime(createdAt, DateTime.UtcNow);

            // Assert: verify the result is exactly "Just now"
            Assert.That(result, Is.EqualTo("Just now"));
        }

        // -------------------------------------------------------
        // TEST 2: Future report timestamps show "Just now"
        // -------------------------------------------------------
        [Test]
        public void BuildRelativeTime_FutureTimestamp_ReturnsJustNow()
        {
            // Arrange: get current time and create a future time
            var currentTime = DateTime.UtcNow;
            var createdAt = currentTime.AddMinutes(5);

            // Act: call method with future timestamp
            var result = ReportIssue.BuildRelativeTime(createdAt, currentTime);

            // Assert: future times should still return "Just now"
            Assert.That(result, Is.EqualTo("Just now"));
        }

        // -------------------------------------------------------
        // TEST 3: Reports created minutes ago show "X minutes ago"
        // -------------------------------------------------------
        [Test]
        public void BuildRelativeTime_FiveMinutesAgo_ReturnsFiveMinutesAgo()
        {
            // Arrange: set time to 5 minutes in the past
            var currentTime = DateTime.UtcNow;
            var createdAt = currentTime.AddMinutes(-5);

            // Act: call method
            var result = ReportIssue.BuildRelativeTime(createdAt, currentTime);

            // Assert: check that it correctly formats "5 minutes ago"
            Assert.That(result, Is.EqualTo("5 minutes ago"));
        }

        // -------------------------------------------------------
        // TEST 4: Reports created hours ago show "X hours ago"
        // -------------------------------------------------------
        [Test]
        public void BuildRelativeTime_TwoHoursAgo_ReturnsTwoHoursAgo()
        {
            // Arrange: set time to 2 hours in the past
            var currentTime = DateTime.UtcNow;
            var createdAt = currentTime.AddHours(-2);

            // Act: call method
            var result = ReportIssue.BuildRelativeTime(createdAt, currentTime);

            // Assert: verify it returns "2 hours ago"
            Assert.That(result, Is.EqualTo("2 hours ago"));
        }

        // -------------------------------------------------------
        // TEST 5: Reports created days ago show "X days ago"
        // -------------------------------------------------------
        [Test]
        public void BuildRelativeTime_ThreeDaysAgo_ReturnsThreeDaysAgo()
        {
            // Arrange: set time to 3 days in the past
            var currentTime = DateTime.UtcNow;
            var createdAt = currentTime.AddDays(-3);

            // Act: call method
            var result = ReportIssue.BuildRelativeTime(createdAt, currentTime);

            // Assert: verify it returns "3 days ago"
            Assert.That(result, Is.EqualTo("3 days ago"));
        }
    }
}