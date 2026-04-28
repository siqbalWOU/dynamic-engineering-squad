using System;

namespace InfrastructureApp.Models
{
    public class UserShopItemPurchase
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;

        public int ShopItemId { get; set; }

        public int CostPoints { get; set; }

        public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

        public ShopItem? ShopItem { get; set; }
    }
}
