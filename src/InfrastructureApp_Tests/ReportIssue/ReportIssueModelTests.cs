using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using InfrastructureApp.Models;
using NUnit.Framework;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace InfrastructureApp_Tests.Models;

[TestFixture]
public class ReportIssueModelTests
{
    //Creates a list to hold validation errors, checks the object against its DataAnnotations.
    //it validates every property, not just “Required” on the object. Returns the list of validation results (empty list = passes).
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    //tests whether a description was given
    [Test]
    public void Description_IsRequired()
    {
        var report = new ReportIssue
        {
            Description = "",   // invalid
            Photo = null,       // invalid too, but we assert the description error exists
            Latitude = 0,
            Longitude = 0,
            UserId = "user-1",
            Status = "Pending"
        };

        var results = Validate(report);

        Assert.That(results, Has.Some.Matches<ValidationResult>(r =>
            r.ErrorMessage != null &&
            r.ErrorMessage.Contains("Please enter a description.")));
    }

    //tests description length
    [Test]
    public void Description_TooLong_Fails()
    {
        var report = new ReportIssue
        {
            Description = new string('a', 301), // limit set to 300 characters
            Photo = null,
            Latitude = 0,
            Longitude = 0,
            UserId = "user-1",
            Status = "Pending"
        };

        var results = Validate(report);

        Assert.That(results, Has.Some.Matches<ValidationResult>(r =>
            r.ErrorMessage != null &&
            r.ErrorMessage.Contains("300 characters")));
    }

    //tests latitude range
    [Test]
    public void Latitude_OutOfRange_Fails()
    {
        var report = new ReportIssue
        {
            Description = "Valid",
            Photo = null,
            Latitude = 91,   // invalid
            Longitude = 0,
            UserId = "user-1",
            Status = "Pending"
        };

        var results = Validate(report);

        Assert.That(results, Has.Some.Matches<ValidationResult>(r =>
            r.ErrorMessage != null &&
            r.ErrorMessage.Contains("Latitude")));
    }

    //tests longitude range
    [Test]
    public void Longitude_OutOfRange_Fails()
    {
        var report = new ReportIssue
        {
            Description = "Valid",
            Photo = null,
            Latitude = 0,
            Longitude = 181,   // invalid
            UserId = "user-1",
            Status = "Pending"
        };

        var results = Validate(report);

        Assert.That(results, Has.Some.Matches<ValidationResult>(r =>
            r.ErrorMessage != null &&
            r.ErrorMessage.Contains("Longitude")));
    }

    //if geolocation is not provided, test fails
    [Test]
    public void Location_IsRequired_WhenMissing_Fails()
    {
        var report = new ReportIssue
        {
            Description = "Valid",
            Photo = null,
            Latitude = null,
            Longitude = null,
            UserId = "user-1",
            Status = "Pending"
        };

        var results = Validate(report);

        Assert.That(results, Has.Some.Matches<ValidationResult>(r =>
            r.MemberNames.Contains(nameof(ReportIssue.Latitude)) ||
            r.MemberNames.Contains(nameof(ReportIssue.Longitude))));
    }

    //if geolocation is provided, test passes
    [Test]
    public void Location_WhenProvided_Passes()
    {
        var report = new ReportIssue
        {
            Description = "Valid",
            Photo = null,
            Latitude = 44.9m,
            Longitude = -123.0m,
            UserId = "user-1",
            Status = "Pending"
        };

        var results = Validate(report);

        Assert.That(results, Has.None.Matches<ValidationResult>(r =>
            r.MemberNames.Contains(nameof(ReportIssue.Latitude)) ||
            r.MemberNames.Contains(nameof(ReportIssue.Longitude))));
    }

    //tests whether a photo is uploaded when there is no camera image
    [Test]
    public void Photo_IsRequired_WhenNoCameraImageProvided()
    {
        var report = new ReportIssue
        {
            Description = "Valid",
            Photo = null,
            CameraImageUrl = null,
            Latitude = 0,
            Longitude = 0,
            UserId = "user-1",
            Status = "Pending"
        };

        var results = Validate(report);

        Assert.That(results, Has.Some.Matches<ValidationResult>(r =>
            r.MemberNames.Contains(nameof(ReportIssue.Photo)) &&
            r.ErrorMessage != null &&
            r.ErrorMessage.Contains("Please upload a photo of the damage.")));
    }

    //tests to make sure that photo is not required when camera image is present
    [Test]
    public void Photo_NotRequired_WhenCameraImageProvided()
    {
        var report = new ReportIssue
        {
            Description = "Valid",
            Photo = null,
            CameraImageUrl = "https://example.com/camera.jpg",
            Latitude = 0,
            Longitude = 0,
            UserId = "user-1",
            Status = "Pending"
        };

        var results = Validate(report);

        Assert.That(results, Has.None.Matches<ValidationResult>(r =>
            r.MemberNames.Contains(nameof(ReportIssue.Photo))));
    }

    //test whether a photo was provided
    [Test]
    public void Photo_WhenProvided_Passes()
    {
        //build fake uploaded file to test
        var bytes = new byte[] { 1, 2, 3 };
        using var stream = new MemoryStream(bytes);

        IFormFile file = new FormFile(stream, 0, bytes.Length, "Photo", "test.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        //tests that fake file
        var report = new ReportIssue
        {
            Description = "Valid",
            Photo = file,   // valid
            Latitude = 0,
            Longitude = 0,
            UserId = "user-1",
            Status = "Pending"
        };

        var results = Validate(report);

        Assert.That(results, Has.None.Matches<ValidationResult>(r =>
            r.MemberNames.Contains(nameof(ReportIssue.Photo))));
    }

    //test severity status default value
    [Test]
    public void SeverityStatus_DefaultValue_IsPending()
    {
        var report = new ReportIssue();

        Assert.That(report.SeverityStatus, Is.EqualTo("Pending"));
    }

    //test severity status too long
    [Test]
    public void SeverityStatus_TooLong_Fails()
    {
        var report = new ReportIssue
        {
            Description = "Valid description",
            Photo = null,
            CameraImageUrl = "https://example.com/camera.jpg",
            Latitude = 0,
            Longitude = 0,
            UserId = "user-1",
            Status = "Pending",
            SeverityStatus = new string('a', 21) // exceeds MaxLength(20)
        };

        var results = Validate(report);

        Assert.That(results, Has.Some.Matches<ValidationResult>(r =>
            r.MemberNames.Contains(nameof(ReportIssue.SeverityStatus))));
    }

    //test severity reason more than 1000 chars
    [Test]
    public void SeverityReason_TooLong_Fails()
    {
        var report = new ReportIssue
        {
            Description = "Valid description",
            Photo = null,
            CameraImageUrl = "https://example.com/camera.jpg",
            Latitude = 0,
            Longitude = 0,
            UserId = "user-1",
            Status = "Pending",
            SeverityStatus = "Pending",
            SeverityReason = new string('a', 1001) // exceeds MaxLength(1000)
        };

        var results = Validate(report);

        Assert.That(results, Has.Some.Matches<ValidationResult>(r =>
            r.MemberNames.Contains(nameof(ReportIssue.SeverityReason))));
    }

    //test severity status passes within max length reason
    [Test]
    public void SeverityFields_WithinMaxLength_Pass()
    {
        var report = new ReportIssue
        {
            Description = "Valid description",
            Photo = null,
            CameraImageUrl = "https://example.com/camera.jpg",
            Latitude = 0,
            Longitude = 0,
            UserId = "user-1",
            Status = "Pending",
            SeverityStatus = "Moderate",
            SeverityReason = "AI detected visible road damage with moderate severity."
        };

        var results = Validate(report);

        Assert.That(results, Has.None.Matches<ValidationResult>(r =>
            r.MemberNames.Contains(nameof(ReportIssue.SeverityStatus)) ||
            r.MemberNames.Contains(nameof(ReportIssue.SeverityReason))));
    }
}
