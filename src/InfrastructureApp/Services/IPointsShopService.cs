using InfrastructureApp.Models;
using System.Threading.Tasks;

namespace InfrastructureApp.Services
{
    public interface IPointsShopService
    {
        Task<PointsShopSnapshot> GetShopAsync(string userId);

        Task<PointsShopPurchaseResult> PurchaseAsync(string userId, int shopItemId);
    }
}
