using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services
{
    public class PointsShopService : IPointsShopService
    {
        private readonly ApplicationDbContext _db;

        public PointsShopService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PointsShopSnapshot> GetShopAsync(string userId)
        {
            await EnsureSeedDataAsync();

            var currentPoints = await _db.UserPoints
                .AsNoTracking()
                .Where(up => up.UserId == userId)
                .Select(up => (int?)up.CurrentPoints)
                .FirstOrDefaultAsync() ?? 0;

            var ownedSinglePurchaseIds = await _db.UserShopItemPurchases
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .Select(p => p.ShopItemId)
                .Distinct()
                .ToListAsync();

            var ownedSet = ownedSinglePurchaseIds.ToHashSet();

            var activeItems = await _db.ShopItems
                .AsNoTracking()
                .Where(i => i.IsActive)
                .OrderBy(i => i.CostPoints)
                .ThenBy(i => i.Name)
                .ToListAsync();

            var items = activeItems
                .Where(i => !PointsShopCatalog.ShouldHideFromShop(i.Name))
                .Select(i =>
                {
                    var isOwned = i.IsSinglePurchase && ownedSet.Contains(i.Id);
                    var background = PointsShopCatalog.GetDashboardBackgroundByName(i.Name);
                    var activitySummaryBackground = PointsShopCatalog.GetActivitySummaryBackgroundByName(i.Name);
                    var border = PointsShopCatalog.GetDashboardBorderByName(i.Name);
                    var activitySummaryBorder = PointsShopCatalog.GetActivitySummaryBorderByName(i.Name);

                    return new PointsShopItemSummary
                    {
                        Id = i.Id,
                        Name = i.Name,
                        Description = i.Description,
                        CostPoints = i.CostPoints,
                        IsSinglePurchase = i.IsSinglePurchase,
                        IsOwned = isOwned,
                        CanPurchase = !isOwned && currentPoints >= i.CostPoints,
                        CategoryLabel = background?.CategoryLabel ?? activitySummaryBackground?.CategoryLabel ?? border?.CategoryLabel ?? activitySummaryBorder?.CategoryLabel ?? "Shop Item",
                        PreviewImageUrl = background?.ImageUrl ?? activitySummaryBackground?.ImageUrl,
                        PreviewCssClass = border?.PreviewCssClass ?? activitySummaryBorder?.PreviewCssClass
                    };
                })
                .ToList();

            return new PointsShopSnapshot
            {
                CurrentPoints = currentPoints,
                Items = items.AsReadOnly()
            };
        }

        public async Task<PointsShopPurchaseResult> PurchaseAsync(string userId, int shopItemId)
        {
            await EnsureSeedDataAsync();

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var item = await _db.ShopItems
                    .FirstOrDefaultAsync(i => i.Id == shopItemId);

                if (item == null || !item.IsActive)
                {
                    var missingBalance = await GetCurrentPointsAsync(userId);
                    return PointsShopPurchaseResult.Failure(
                        "That shop item is unavailable.",
                        missingBalance,
                        shopItemId);
                }

                if (PointsShopCatalog.ShouldHideFromShop(item.Name))
                {
                    var hiddenBalance = await GetCurrentPointsAsync(userId);
                    return PointsShopPurchaseResult.Failure(
                        "That shop item is unavailable.",
                        hiddenBalance,
                        shopItemId);
                }

                if (item.IsSinglePurchase)
                {
                    var alreadyOwned = await _db.UserShopItemPurchases
                        .AnyAsync(p => p.UserId == userId && p.ShopItemId == shopItemId);

                    if (alreadyOwned)
                    {
                        var ownedBalance = await GetCurrentPointsAsync(userId);
                        return PointsShopPurchaseResult.Failure(
                            "You already own that item.",
                            ownedBalance,
                            shopItemId);
                    }
                }

                var userPoints = await _db.UserPoints
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (userPoints == null)
                {
                    userPoints = new UserPoints
                    {
                        UserId = userId,
                        CurrentPoints = 0,
                        LifetimePoints = 0,
                        LastUpdated = DateTime.UtcNow
                    };
                    _db.UserPoints.Add(userPoints);
                }

                if (userPoints.CurrentPoints < item.CostPoints)
                {
                    return PointsShopPurchaseResult.Failure(
                        "You do not have enough points for that item.",
                        userPoints.CurrentPoints,
                        shopItemId);
                }

                userPoints.CurrentPoints -= item.CostPoints;
                userPoints.LastUpdated = DateTime.UtcNow;

                _db.UserShopItemPurchases.Add(new UserShopItemPurchase
                {
                    UserId = userId,
                    ShopItemId = item.Id,
                    CostPoints = item.CostPoints,
                    PurchasedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return PointsShopPurchaseResult.Success(
                    $"Purchased {item.Name} for {item.CostPoints} points.",
                    userPoints.CurrentPoints,
                    item.Id);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task<int> GetCurrentPointsAsync(string userId)
        {
            return await _db.UserPoints
                .Where(up => up.UserId == userId)
                .Select(up => (int?)up.CurrentPoints)
                .FirstOrDefaultAsync() ?? 0;
        }

        private async Task EnsureSeedDataAsync()
        {
            var existingNames = await _db.ShopItems
                .AsNoTracking()
                .Select(item => item.Name)
                .ToListAsync();

            var existingNameSet = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var seenSeedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var missingItems = PointsShopCatalog.GetStarterItems()
                .Where(item => seenSeedNames.Add(item.Name))
                .Where(item => !existingNameSet.Contains(item.Name))
                .ToList();

            if (missingItems.Count == 0)
            {
                return;
            }

            _db.ShopItems.AddRange(missingItems);
            await _db.SaveChangesAsync();
        }
    }
}
