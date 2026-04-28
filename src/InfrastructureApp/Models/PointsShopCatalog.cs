using System;
using System.Collections.Generic;
using System.Linq;

namespace InfrastructureApp.Models
{
    public sealed class DashboardBackgroundDefinition
    {
        public required string Key { get; init; }

        public required string Name { get; init; }

        public required string Description { get; init; }

        public required string ImageUrl { get; init; }

        public string CategoryLabel { get; init; } = "Dashboard Background";

        public int CostPoints { get; init; } = 10;
    }

    public sealed class DashboardBorderDefinition
    {
        public required string Key { get; init; }

        public required string Name { get; init; }

        public required string Description { get; init; }

        public required string CssClass { get; init; }

        public required string PreviewCssClass { get; init; }

        public string CategoryLabel { get; init; } = "Dashboard Border";

        public int CostPoints { get; init; } = 10;
    }

    public sealed class ActivitySummaryBackgroundDefinition
    {
        public required string Key { get; init; }

        public required string Name { get; init; }

        public required string Description { get; init; }

        public required string ImageUrl { get; init; }

        public string CategoryLabel { get; init; } = "Activity Summary Background";

        public int CostPoints { get; init; } = 10;
    }

    public sealed class ActivitySummaryBorderDefinition
    {
        public required string Key { get; init; }

        public required string Name { get; init; }

        public required string Description { get; init; }

        public required string CssClass { get; init; }

        public required string PreviewCssClass { get; init; }

        public string CategoryLabel { get; init; } = "Activity Summary Border";

        public int CostPoints { get; init; } = 10;
    }

    public static class PointsShopCatalog
    {
        private static readonly IReadOnlyDictionary<string, string> LegacyItemNameMap =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Dashboard Background Image"] = "Safety Wave Background"
            };

        private static readonly IReadOnlySet<string> RetiredItemNames =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "Golden Reporter Title",
                "Map Pin Cosmetic",
                "Premium Profile Frame",
                "Dark Theme Badge",
                "Signal Glow Background"
            };

        public const string DashboardBackgroundImageItemName = "Safety Wave Background";

        public static readonly IReadOnlyList<DashboardBackgroundDefinition> DashboardBackgrounds =
            new[]
            {
                new DashboardBackgroundDefinition
                {
                    Key = "safety-wave",
                    Name = "Safety Wave Background",
                    Description = "Sweep your dashboard card with dark contour waves and safety orange-yellow highlights.",
                    ImageUrl = "/Images/dashboard-personal-info-bg.svg"
                },
                new DashboardBackgroundDefinition
                {
                    Key = "city-grid",
                    Name = "City Grid Background",
                    Description = "Add a cool blue street-grid backdrop with a dim skyline silhouette.",
                    ImageUrl = "/Images/dashboard-city-grid-bg.svg"
                },
                new DashboardBackgroundDefinition
                {
                    Key = "safety-stripe",
                    Name = "Safety Stripe Background",
                    Description = "Cover the card in bold diagonal caution stripes with a construction-zone feel.",
                    ImageUrl = "/Images/dashboard-safety-stripe-bg.svg"
                },
                new DashboardBackgroundDefinition
                {
                    Key = "blueprint",
                    Name = "Blueprint Background",
                    Description = "Give the card a technical blueprint layout with crisp drafting lines.",
                    ImageUrl = "/Images/dashboard-blueprint-bg.svg"
                },
                new DashboardBackgroundDefinition
                {
                    Key = "aurora-night",
                    Name = "Aurora Night Background",
                    Description = "Set the card against a dark night sky with a soft glowing aurora band.",
                    ImageUrl = "/Images/dashboard-aurora-night-bg.svg"
                }
            };

        public static readonly IReadOnlyList<ActivitySummaryBackgroundDefinition> ActivitySummaryBackgrounds =
            new[]
            {
                new ActivitySummaryBackgroundDefinition
                {
                    Key = "signal-grid",
                    Name = "Signal Grid Activity Background",
                    Description = "Layer the activity card with a radar-style grid and signal sweep accents.",
                    ImageUrl = "/Images/activity-summary-signal-grid-bg.svg"
                },
                new ActivitySummaryBackgroundDefinition
                {
                    Key = "amber-wave",
                    Name = "Amber Wave Activity Background",
                    Description = "Set the activity card behind warm amber contour waves and soft motion lines.",
                    ImageUrl = "/Images/activity-summary-amber-wave-bg.svg"
                },
                new ActivitySummaryBackgroundDefinition
                {
                    Key = "blueprint-panel",
                    Name = "Blueprint Panel Activity Background",
                    Description = "Give the activity card a compact blueprint panel with technical drafting details.",
                    ImageUrl = "/Images/activity-summary-blueprint-panel-bg.svg"
                },
                new ActivitySummaryBackgroundDefinition
                {
                    Key = "transit-lines",
                    Name = "Transit Lines Activity Background",
                    Description = "Add intersecting route lines and glowing waypoints behind the activity totals.",
                    ImageUrl = "/Images/activity-summary-transit-lines-bg.svg"
                },
                new ActivitySummaryBackgroundDefinition
                {
                    Key = "night-pulse",
                    Name = "Night Pulse Activity Background",
                    Description = "Wrap the activity card in a dark pulse field with cool blue and gold highlights.",
                    ImageUrl = "/Images/activity-summary-night-pulse-bg.svg"
                }
            };

        public static readonly IReadOnlyList<DashboardBorderDefinition> DashboardBorders =
            new[]
            {
                new DashboardBorderDefinition
                {
                    Key = "gold-ring",
                    Name = "Golden Border",
                    Description = "Frame the card with a bright caution-gold edge and a soft metallic glow.",
                    CssClass = "dashboard-personal-card--border-gold-ring",
                    PreviewCssClass = "dashboard-border-preview--gold-ring"
                },
                new DashboardBorderDefinition
                {
                    Key = "blue-glow",
                    Name = "Blue Glow Border",
                    Description = "Outline the card with a cool blue edge and a subtle electric glow.",
                    CssClass = "dashboard-personal-card--border-blue-glow",
                    PreviewCssClass = "dashboard-border-preview--blue-glow"
                },
                new DashboardBorderDefinition
                {
                    Key = "safety-stripe",
                    Name = "Safety Stripe Border",
                    Description = "Wrap the card in a hazard-striped border with orange and yellow warning bands.",
                    CssClass = "dashboard-personal-card--border-safety-stripe",
                    PreviewCssClass = "dashboard-border-preview--safety-stripe"
                },
                new DashboardBorderDefinition
                {
                    Key = "steel-frame",
                    Name = "Steel Frame Border",
                    Description = "Add a brushed steel frame with a clean industrial finish.",
                    CssClass = "dashboard-personal-card--border-steel-frame",
                    PreviewCssClass = "dashboard-border-preview--steel-frame"
                },
                new DashboardBorderDefinition
                {
                    Key = "ember-outline",
                    Name = "Ember Outline Border",
                    Description = "Surround the card with a warm ember-orange outline and faint heat glow.",
                    CssClass = "dashboard-personal-card--border-ember-outline",
                    PreviewCssClass = "dashboard-border-preview--ember-outline"
                }
            };

        public static readonly IReadOnlyList<ActivitySummaryBorderDefinition> ActivitySummaryBorders =
            new[]
            {
                new ActivitySummaryBorderDefinition
                {
                    Key = "signal-ring",
                    Name = "Signal Ring Activity Border",
                    Description = "Wrap the activity card with a cool signal-blue frame and subtle radar glow.",
                    CssClass = "dashboard-activity-card--border-signal-ring",
                    PreviewCssClass = "dashboard-border-preview--signal-ring"
                },
                new ActivitySummaryBorderDefinition
                {
                    Key = "amber-rail",
                    Name = "Amber Rail Activity Border",
                    Description = "Add a warm amber rail around the activity card with a clean industrial edge.",
                    CssClass = "dashboard-activity-card--border-amber-rail",
                    PreviewCssClass = "dashboard-border-preview--amber-rail"
                },
                new ActivitySummaryBorderDefinition
                {
                    Key = "route-stripe",
                    Name = "Route Stripe Activity Border",
                    Description = "Give the activity card a striped transit-style border with bold directional color.",
                    CssClass = "dashboard-activity-card--border-route-stripe",
                    PreviewCssClass = "dashboard-border-preview--route-stripe"
                },
                new ActivitySummaryBorderDefinition
                {
                    Key = "steel-panel",
                    Name = "Steel Panel Activity Border",
                    Description = "Frame the activity card with a rigid steel panel look and crisp metallic edge.",
                    CssClass = "dashboard-activity-card--border-steel-panel",
                    PreviewCssClass = "dashboard-border-preview--steel-panel"
                },
                new ActivitySummaryBorderDefinition
                {
                    Key = "night-pulse",
                    Name = "Night Pulse Activity Border",
                    Description = "Surround the activity card with a dark neon outline and faint pulse glow.",
                    CssClass = "dashboard-activity-card--border-night-pulse",
                    PreviewCssClass = "dashboard-border-preview--night-pulse"
                }
            };

        public static IReadOnlyList<ShopItem> GetStarterItems()
        {
            var backgroundItems = DashboardBackgrounds
                .Select(background => new ShopItem
                {
                    Name = background.Name,
                    Description = background.Description,
                    CostPoints = background.CostPoints,
                    IsSinglePurchase = true,
                    IsActive = true
                });

            var borderItems = DashboardBorders
                .Select(border => new ShopItem
                {
                    Name = border.Name,
                    Description = border.Description,
                    CostPoints = border.CostPoints,
                    IsSinglePurchase = true,
                    IsActive = true
                });

            var activitySummaryItems = ActivitySummaryBackgrounds
                .Select(background => new ShopItem
                {
                    Name = background.Name,
                    Description = background.Description,
                    CostPoints = background.CostPoints,
                    IsSinglePurchase = true,
                    IsActive = true
                });

            var activitySummaryBorderItems = ActivitySummaryBorders
                .Select(border => new ShopItem
                {
                    Name = border.Name,
                    Description = border.Description,
                    CostPoints = border.CostPoints,
                    IsSinglePurchase = true,
                    IsActive = true
                });

            return backgroundItems
                .Concat(activitySummaryItems)
                .Concat(borderItems)
                .Concat(activitySummaryBorderItems)
                .ToList()
                .AsReadOnly();
        }

        public static DashboardBackgroundDefinition? GetDashboardBackgroundByKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return DashboardBackgrounds.FirstOrDefault(background => background.Key == key);
        }

        public static DashboardBackgroundDefinition? GetDashboardBackgroundByName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var canonicalName = NormalizeItemName(name);
            return DashboardBackgrounds.FirstOrDefault(background => background.Name == canonicalName);
        }

        public static DashboardBorderDefinition? GetDashboardBorderByKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return DashboardBorders.FirstOrDefault(border => border.Key == key);
        }

        public static DashboardBorderDefinition? GetDashboardBorderByName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var canonicalName = NormalizeItemName(name);
            return DashboardBorders.FirstOrDefault(border => border.Name == canonicalName);
        }

        public static ActivitySummaryBackgroundDefinition? GetActivitySummaryBackgroundByKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return ActivitySummaryBackgrounds.FirstOrDefault(background => background.Key == key);
        }

        public static ActivitySummaryBackgroundDefinition? GetActivitySummaryBackgroundByName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var canonicalName = NormalizeItemName(name);
            return ActivitySummaryBackgrounds.FirstOrDefault(background => background.Name == canonicalName);
        }

        public static ActivitySummaryBorderDefinition? GetActivitySummaryBorderByKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return ActivitySummaryBorders.FirstOrDefault(border => border.Key == key);
        }

        public static ActivitySummaryBorderDefinition? GetActivitySummaryBorderByName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var canonicalName = NormalizeItemName(name);
            return ActivitySummaryBorders.FirstOrDefault(border => border.Name == canonicalName);
        }

        public static string NormalizeItemName(string name)
        {
            return LegacyItemNameMap.TryGetValue(name, out var canonicalName)
                ? canonicalName
                : name;
        }

        public static bool IsRetiredItemName(string name)
        {
            return RetiredItemNames.Contains(name);
        }

        public static bool ShouldHideFromShop(string name)
        {
            return IsRetiredItemName(name)
                || LegacyItemNameMap.ContainsKey(name);
        }
    }
}
