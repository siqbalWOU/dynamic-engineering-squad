using NUnit.Framework;
using InfrastructureApp.Services.ImageSeverity;

namespace InfrastructureApp_Tests.Services.ImageSeverity
{
    [TestFixture]
    public class SeverityEstimationResultTests
    {
        [Test]
        public void Success_SetsCorrectValues()
        {
            var result = SeverityEstimationResult.Success(
                ImageSeverityStatuses.High,
                "Large pothole with deep cracking");

            Assert.That(result.Performed, Is.True);
            Assert.That(result.SeverityStatus, Is.EqualTo(ImageSeverityStatuses.High));
            Assert.That(result.Reason, Is.EqualTo("Large pothole with deep cracking"));
        }

        [Test]
        public void Success_WithNoReason_SetsReasonToNull()
        {
            var result = SeverityEstimationResult.Success(ImageSeverityStatuses.Medium);

            Assert.That(result.Performed, Is.True);
            Assert.That(result.SeverityStatus, Is.EqualTo(ImageSeverityStatuses.Medium));
            Assert.That(result.Reason, Is.Null);
        }

        [Test]
        public void Failed_SetsPerformedFalse_AndPendingStatus()
        {
            var result = SeverityEstimationResult.Failed("Estimator unavailable");

            Assert.That(result.Performed, Is.False);
            Assert.That(result.SeverityStatus, Is.EqualTo(ImageSeverityStatuses.Pending));
            Assert.That(result.Reason, Is.EqualTo("Estimator unavailable"));
        }

        [Test]
        public void Failed_WithNoReason_SetsReasonToNull_AndPendingStatus()
        {
            var result = SeverityEstimationResult.Failed();

            Assert.That(result.Performed, Is.False);
            Assert.That(result.SeverityStatus, Is.EqualTo(ImageSeverityStatuses.Pending));
            Assert.That(result.Reason, Is.Null);
        }

        [Test]
        public void NewInstance_DefaultsSeverityStatusToPending()
        {
            var result = new SeverityEstimationResult();

            Assert.That(result.Performed, Is.False);
            Assert.That(result.SeverityStatus, Is.EqualTo(ImageSeverityStatuses.Pending));
            Assert.That(result.Reason, Is.Null);
        }
    }
}