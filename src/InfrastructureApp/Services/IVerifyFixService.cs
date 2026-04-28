namespace InfrastructureApp.Services
{
    public interface IVerifyFixService
    {
        // Adds a verification if the user hasn't verified yet; removes it if they have.
        // Auto-transitions report status to "Verified Fixed" when threshold is reached.
        // Returns the updated verification count and whether the user's verification is now active.
        Task<(int verifyCount, bool userHasVerified)> ToggleVerificationAsync(int reportId, string userId);

        // Returns the current verification count and whether the given user has verified.
        Task<(int verifyCount, bool userHasVerified)> GetVerifyStatusAsync(int reportId, string? userId);

        // Returns verification counts keyed by reportId for a batch of reports.
        Task<Dictionary<int, int>> GetVerifyCountsAsync(IEnumerable<int> reportIds);
    }
}
