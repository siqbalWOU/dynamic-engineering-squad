using InfrastructureApp.Services.ImageSeverity;

namespace InfrastructureApp_Tests.TestDoubles
{
    public static class SeverityTestBehavior
    {
        public static ImageModerationResult ModerationResult { get; set; }
            = ImageModerationResult.Passed();

        public static SeverityEstimationResult SeverityResult { get; set; }
            = SeverityEstimationResult.Success(
                ImageSeverityStatuses.Low,
                "Default test reason.");

        public static void Reset()
        {
            ModerationResult = ImageModerationResult.Passed();
            SeverityResult = SeverityEstimationResult.Success(
                ImageSeverityStatuses.Low,
                "Default test reason.");
        }
    }
}