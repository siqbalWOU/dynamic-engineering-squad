using InfrastructureApp.Models;
using InfrastructureApp.ViewModels.Account;
using Microsoft.AspNetCore.Identity;

namespace InfrastructureApp.Services
{
    public class AvatarService : IAvatarService
    
    {
        private readonly UserManager<Users> _userManager;
        private readonly IWebHostEnvironment _env;

        public AvatarService(UserManager<Users> userManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _env = env;
        }

        public ChooseAvatarViewModel BuildChooseAvatarViewModel(Users user, string? selectedKey = null, string? error = null)
        {
            var current = selectedKey ?? user.AvatarKey;

            return new ChooseAvatarViewModel
            {
                SelectedAvatarKey = current,
                ErrorMessage = error,
                Options = AvatarCatalog.Keys.Select(k => new AvatarOptionViewModel
                {
                    Key = k,
                    Url = AvatarCatalog.ToUrl(k),
                    IsSelected = string.Equals(k, current, StringComparison.OrdinalIgnoreCase)
                }).ToList()
            };
        }

        public async Task<(bool Success, string? ErrorMessage)> SaveAvatarAsync(Users user, string? selectedAvatarKey)
        {
            if (!AvatarCatalog.IsValid(selectedAvatarKey))
                return (false, "Please select an avatar.");

            user.AvatarKey = selectedAvatarKey;
            user.AvatarUrl = null; 

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return (false, "Could not save your avatar. Please try again.");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> SaveUploadedAvatarAsync(Users user, IFormFile file)
        {
            const long maxBytes = 5 * 1024 * 1024;
            var allowedTypes = new[] { "image/jpeg", "image/png" };

            if (file == null || file.Length == 0)
                return (false, "Please select an image file to upload.");
            if (!allowedTypes.Contains(file.ContentType))
                return (false, "Only JPG and PNG files are accepted.");
            if (file.Length > maxBytes)
                return (false, "File exceeds the 5 MB size limit.");

            //Derives the file extension from the validated content type
            //and avoid trusting the original filename from the client
            var ext      = file.ContentType == "image/png" ? ".png" : ".jpg";

            //Creates a unique filename using a GUID 
            //and prevents collisions if two users upload iles with the same name
            var fileName = $"{Guid.NewGuid()}{ext}";

            //Builds the full folder path on disk
            //Contentrootpath is the project root, so this resolves to 
            //C:\InfrastructureApp\wwwroot\uploads\avatars\
            var folder   = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "avatars");

            //Creates the folder if it doesn't exist yet
            Directory.CreateDirectory(folder);

            //Combines the folder path with the filename to get the full save path
            var savePath = Path.Combine(folder, fileName);

            //Creates the file on disk and copies the uploaded bytes into it
            await using var stream = System.IO.File.Create(savePath);
            await file.CopyToAsync(stream);

            //Saves the web-accessible URL path to the database
            //this is what the browser uses to display the image
            //note it uses forward slashes, not the OS file path
            user.AvatarUrl = $"/uploads/avatars/{fileName}";
            user.AvatarKey = null;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return (false, "Could not save your photo. Please try again.");

            return (true, null);
        }
    }
}