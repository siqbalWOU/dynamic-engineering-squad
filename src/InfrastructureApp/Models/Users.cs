using Microsoft.AspNetCore.Identity;
using InfrastructureApp.Services;


namespace InfrastructureApp.Models
{
    public class Users : IdentityUser
    {
        public string? AvatarKey { get; set; } // e.g. "avatar01", "avatar02"
        public List<string>? Roles { get; set; }
    }
}
