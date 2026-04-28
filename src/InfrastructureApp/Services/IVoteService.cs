namespace InfrastructureApp.Services
{
    public interface IVoteService
    {
        // Adds a vote if the user hasn't voted yet; removes it if they have.
        // Returns the updated vote count and whether the user's vote is now active.
        Task<(int voteCount, bool userHasVoted)> ToggleVoteAsync(int reportId, string userId);

        // Returns the current vote count and whether the given user has voted.
        Task<(int voteCount, bool userHasVoted)> GetVoteStatusAsync(int reportId, string? userId);
    }
}
