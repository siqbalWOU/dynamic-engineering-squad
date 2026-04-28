//this shows why an image moderation was passed, rejected, failed. 
namespace InfrastructureApp.Services.ImageSeverity
{
    public sealed class ImageModerationResult
    {
        public bool Performed { get; init; }
        public bool IsViable { get; init; }
        public string? Reason { get; init; }

        public static ImageModerationResult Passed(string? reason = null) => new()
        {
            Performed = true,
            IsViable = true,
            Reason = reason
        };

        public static ImageModerationResult Rejected(string? reason = null) => new()
        {
            Performed = true,
            IsViable = false,
            Reason = reason
        };

        public static ImageModerationResult Failed(string? reason = null) => new()
        {
            Performed = false,
            IsViable = false,
            Reason = reason
        };
    }
}