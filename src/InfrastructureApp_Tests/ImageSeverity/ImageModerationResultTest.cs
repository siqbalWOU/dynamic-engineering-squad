using NUnit.Framework;
using InfrastructureApp.Services.ImageSeverity;

namespace InfrastructureApp_Tests.Services
{
    [TestFixture]
    public class ImageModerationResultTests
    {
        [Test]
        public void Passed_SetsCorrectValues()
        {
            var result = ImageModerationResult.Passed("Looks good");

            Assert.That(result.Performed, Is.True);
            Assert.That(result.IsViable, Is.True);
            Assert.That(result.Reason, Is.EqualTo("Looks good"));
        }

        [Test]
        public void Passed_WithNoReason_SetsReasonToNull()
        {
            var result = ImageModerationResult.Passed();

            Assert.That(result.Performed, Is.True);
            Assert.That(result.IsViable, Is.True);
            Assert.That(result.Reason, Is.Null);
        }

        [Test]
        public void Rejected_SetsCorrectValues()
        {
            var result = ImageModerationResult.Rejected("Inappropriate content");

            Assert.That(result.Performed, Is.True);
            Assert.That(result.IsViable, Is.False);
            Assert.That(result.Reason, Is.EqualTo("Inappropriate content"));
        }

        [Test]
        public void Failed_SetsCorrectValues()
        {
            var result = ImageModerationResult.Failed("Service error");

            Assert.That(result.Performed, Is.False);
            Assert.That(result.IsViable, Is.False);
            Assert.That(result.Reason, Is.EqualTo("Service error"));
        }

        [Test]
        public void Failed_WithNoReason_SetsReasonToNull()
        {
            var result = ImageModerationResult.Failed();

            Assert.That(result.Performed, Is.False);
            Assert.That(result.IsViable, Is.False);
            Assert.That(result.Reason, Is.Null);
        }

    }
    
    
}


