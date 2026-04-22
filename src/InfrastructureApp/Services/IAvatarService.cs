using InfrastructureApp.ViewModels.Account;
using InfrastructureApp.Models;
using System.Threading.Tasks;

namespace InfrastructureApp.Services
{
    
    public interface IAvatarService
    {
        ChooseAvatarViewModel BuildChooseAvatarViewModel(Users user, string? selectedKey = null, string? error = null);
        Task<(bool Success, string? ErrorMessage)> SaveAvatarAsync(Users user, string? selectedAvatarKey);

        Task<(bool Success, string? ErrorMessage)> SaveUploadedAvatarAsync(Users user, IFormFile file);

    }
}