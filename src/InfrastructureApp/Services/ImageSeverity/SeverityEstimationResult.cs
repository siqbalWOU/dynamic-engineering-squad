//estimate severity success/fail

namespace InfrastructureApp.Services.ImageSeverity
{
    public sealed class SeverityEstimationResult
    {
        public bool Performed { get; init; }
        public string SeverityStatus { get; init; } = ImageSeverityStatuses.Pending;
        public string? Reason { get; init; }

        public static SeverityEstimationResult Success(string severityStatus, string? reason = null) => new()
        {
            Performed = true,
            SeverityStatus = severityStatus,
            Reason = reason
        };

        public static SeverityEstimationResult Failed(string? reason = null) => new()
        {
            Performed = false,
            SeverityStatus = ImageSeverityStatuses.Pending,
            Reason = reason
        };
    }
}