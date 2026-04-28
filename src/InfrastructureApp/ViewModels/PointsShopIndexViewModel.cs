using System;
using System.Collections.Generic;

namespace InfrastructureApp.ViewModels
{
    public class PointsShopIndexViewModel
    {
        public int CurrentPoints { get; init; }

        public IReadOnlyList<PointsShopItemViewModel> Items { get; init; } = Array.Empty<PointsShopItemViewModel>();
    }
}
