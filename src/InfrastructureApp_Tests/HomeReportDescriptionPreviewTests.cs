using InfrastructureApp.Models;
using NUnit.Framework;

namespace InfrastructureApp_Tests;

[TestFixture]
public class HomeReportDescriptionPreviewTests
{
    // -------------------------------------------------------
    // TEST 1: Short descriptions should remain unchanged
    // -------------------------------------------------------
    [Test]
    public void BuildDescriptionPreview_ShortDescription_ReturnsUnchanged()
    {
        // Arrange: short description
        var description = "Small pothole on Main Street";

        // Act: call helper
        var preview = ReportIssue.BuildDescriptionPreview(description, previewLength: 60);

        // Assert: should be unchanged
        Assert.That(preview, Is.EqualTo(description));
    }

    // -------------------------------------------------------
    // TEST 2: Description exactly at limit should not change
    // -------------------------------------------------------
    [Test]
    public void BuildDescriptionPreview_DescriptionAtLimit_ReturnsUnchanged()
    {
        // Arrange: exactly 60 characters
        var description = new string('a', 60);

        // Act: call helper
        var preview = ReportIssue.BuildDescriptionPreview(description, previewLength: 60);

        // Assert: should not be trimmed
        Assert.That(preview, Is.EqualTo(description));
    }

    // -------------------------------------------------------
    // TEST 3: Long descriptions should be truncated with "..."
    // -------------------------------------------------------
    [Test]
    public void BuildDescriptionPreview_LongDescription_TruncatesWithEllipsis()
    {
        // Arrange: longer than limit
        var description = new string('a', 61);

        // Act: call helper
        var preview = ReportIssue.BuildDescriptionPreview(description, previewLength: 60);

        // Assert: should be shortened + "..."
        Assert.That(preview, Is.EqualTo(new string('a', 60) + "..."));
    }

    // -------------------------------------------------------
    // TEST 4: Null description should return empty string
    // -------------------------------------------------------
    [Test]
    public void BuildDescriptionPreview_NullDescription_ReturnsEmptyString()
    {
        // Act: pass null
        var preview = ReportIssue.BuildDescriptionPreview(null, previewLength: 60);

        // Assert: should return empty string
        Assert.That(preview, Is.EqualTo(string.Empty));
    }

    // -------------------------------------------------------
    // TEST 5: Whitespace description should return empty string
    // -------------------------------------------------------
    [Test]
    public void BuildDescriptionPreview_WhitespaceDescription_ReturnsEmptyString()
    {
        // Arrange: whitespace only
        // Act: call helper
        var preview = ReportIssue.BuildDescriptionPreview("   ", previewLength: 60);

        // Assert: should return empty string
        Assert.That(preview, Is.EqualTo(string.Empty));
    }
}