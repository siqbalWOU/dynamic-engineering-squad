using System;
using System.Collections.Generic;

namespace InfrastructureApp.ViewModels.Minigames
{
    public class MinigamesIndexViewModel
    {
        public int CurrentPoints { get; init; }

        public IReadOnlyList<MinigameCardViewModel> Games { get; init; } = Array.Empty<MinigameCardViewModel>();
    }
}
