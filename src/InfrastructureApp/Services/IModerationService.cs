using System.Threading.Tasks;
using InfrastructureApp.ViewModels;

namespace InfrastructureApp.Services
{
    public interface IModerationService
    {
        Task<ModerationDashboardViewModel> GetDashboardViewModelAsync();
        Task<(bool Success, string Message)> DismissReportAsync(int reportId, string moderatorId);
        Task<(bool Success, string Message)> RemovePostAsync(int reportId, string moderatorId);
    }
}
