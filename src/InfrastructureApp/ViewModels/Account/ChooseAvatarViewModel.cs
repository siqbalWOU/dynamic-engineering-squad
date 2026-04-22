namespace InfrastructureApp.ViewModels.Account
{
    public class ChooseAvatarViewModel
    {
        
        public List<AvatarOptionViewModel> Options { get; set; } = new();
        public string? SelectedAvatarKey { get; set; } //posted back
        public string? ErrorMessage { get; set; }

        public IFormFile? UploadedImage { get; set; }
        public bool UseUploadedImage { get; set; }

        public string? UploadedImagePreviewUrl { get; set; }
    }

    public class AvatarOptionViewModel
    {
        public string Key { get; set; } = "";
        public string Url { get; set; } = "";
        public bool IsSelected { get; set; }
    }
}