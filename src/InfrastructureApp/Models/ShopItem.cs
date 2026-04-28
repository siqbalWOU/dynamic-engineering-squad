using System;

namespace InfrastructureApp.Models
{
    public class ShopItem
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;

        public int CostPoints { get; set; }

        public bool IsSinglePurchase { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
