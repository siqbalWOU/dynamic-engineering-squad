using InfrastructureApp.ViewModels;
using NUnit.Framework;

namespace InfrastructureApp_Tests.Reports
{
    // NUnit coverage for Latest Reports shortened description previews. (SCRUM-159)
    [TestFixture]
    public class LatestReportsDescriptionPreviewTests
    {
        // TEST 1: Short descriptions return unchanged.
        [Test]
        public void GetDescriptionPreview_WhenDescriptionIsShort_ReturnsUnchangedDescription()
        {
            // Arrange
            var viewModel = new LatestReportsViewModel();
            var description = "Small pothole near the corner.";

            // Act
            var preview = viewModel.GetDescriptionPreview(description);

            // Assert
            Assert.That(preview, Is.EqualTo(description));
        }

        // TEST 2: Long descriptions are shortened to the configured preview length plus ellipsis.
        [Test]
        public void GetDescriptionPreview_WhenDescriptionIsLong_ReturnsShortenedPreview()
        {
            // Arrange
            var viewModel = new LatestReportsViewModel();
            var description = new string('A', 141);
            var expectedPreview = new string('A', 140) + "...";

            // Act
            var preview = viewModel.GetDescriptionPreview(description);

            // Assert
            Assert.That(preview, Is.EqualTo(expectedPreview));
            Assert.That(preview.Length, Is.EqualTo(143));
        }

        // TEST 3: Shortened descriptions end with an ellipsis.
        [Test]
        public void GetDescriptionPreview_WhenDescriptionIsShortened_EndsWithEllipsis()
        {
            // Arrange
            var viewModel = new LatestReportsViewModel();
            var description = new string('B', 160);

            // Act
            var preview = viewModel.GetDescriptionPreview(description);

            // Assert
            Assert.That(preview, Does.EndWith("..."));
        }

        // TEST 4: Null descriptions return an empty string.
        [Test]
        public void GetDescriptionPreview_WhenDescriptionIsNull_ReturnsEmptyString()
        {
            // Arrange
            var viewModel = new LatestReportsViewModel();

            // Act
            var preview = viewModel.GetDescriptionPreview(null);

            // Assert
            Assert.That(preview, Is.EqualTo(string.Empty));
        }

        // TEST 5: Whitespace descriptions return unchanged.
        [Test]
        public void GetDescriptionPreview_WhenDescriptionIsWhitespace_ReturnsWhitespaceUnchanged()
        {
            // Arrange
            var viewModel = new LatestReportsViewModel();
            var description = "   ";

            // Act
            var preview = viewModel.GetDescriptionPreview(description);

            // Assert
            Assert.That(preview, Is.EqualTo(description));
        }

        // TEST 6: The helper does not change the original full description.
        [Test]
        public void GetDescriptionPreview_WhenDescriptionIsLong_DoesNotChangeOriginalDescription()
        {
            // Arrange
            var viewModel = new LatestReportsViewModel();
            var description = "This is the original full description that should remain available for the Latest Reports modal. " +
                "It is intentionally long enough to require a shortened row preview while preserving the complete text.";
            var originalDescription = description;

            // Act
            var preview = viewModel.GetDescriptionPreview(description);

            // Assert
            Assert.That(description, Is.EqualTo(originalDescription));
            Assert.That(preview, Is.Not.EqualTo(originalDescription));
            Assert.That(originalDescription.Length, Is.GreaterThan(preview.Length));
        }
    }
}
