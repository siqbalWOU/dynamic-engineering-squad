using System.Threading.Tasks;

namespace InfrastructureApp.Services
{
    public interface IFlagService
    {
        Task<(bool Success, string Message)> FlagReportAsync(int reportId, string userId, string category);
        Task<bool> HasUserFlaggedAsync(int reportId, string userId);
    }
}
