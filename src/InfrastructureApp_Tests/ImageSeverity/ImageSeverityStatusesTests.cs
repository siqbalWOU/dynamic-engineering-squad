using NUnit.Framework;
using System.Linq;
using InfrastructureApp.Services.ImageSeverity;

namespace InfrastructureApp_Tests.Services.ImageSeverity
{
    [TestFixture]
    public class ImageSeverityStatusesTests
    {
        //verify constant values
        [Test]
        public void SeverityStatuses_HaveExpectedValues()
        {
            Assert.That(ImageSeverityStatuses.Pending, Is.EqualTo("Pending"));
            Assert.That(ImageSeverityStatuses.Low, Is.EqualTo("Low"));
            Assert.That(ImageSeverityStatuses.Medium, Is.EqualTo("Medium"));
            Assert.That(ImageSeverityStatuses.High, Is.EqualTo("High"));
            Assert.That(ImageSeverityStatuses.Critical, Is.EqualTo("Critical"));
        }

        //ensure all values are unique
        [Test]
        public void SeverityStatuses_AreUnique()
        {
            var values = new[]
            {
                ImageSeverityStatuses.Pending,
                ImageSeverityStatuses.Low,
                ImageSeverityStatuses.Medium,
                ImageSeverityStatuses.High,
                ImageSeverityStatuses.Critical
            };

            Assert.That(values.Distinct().Count(), Is.EqualTo(values.Length));
        }

        //no null/empty values
        [Test]
        public void SeverityStatuses_AreNotNullOrEmpty()
        {
            var values = new[]
            {
                ImageSeverityStatuses.Pending,
                ImageSeverityStatuses.Low,
                ImageSeverityStatuses.Medium,
                ImageSeverityStatuses.High,
                ImageSeverityStatuses.Critical
            };

            Assert.That(values, Has.None.Null.And.None.Empty);
        }
    }
}