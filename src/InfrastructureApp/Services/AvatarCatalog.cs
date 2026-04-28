namespace InfrastructureApp.Services
{
    public static class AvatarCatalog
    {

        //My allowed avatars
        //Prevents someone from saving random strings (security)
        public static readonly IReadOnlyList<string> Keys = new List<string>
        {
            "boy_blue_hair",
            "boy_bluehair_darkskin",
            "boy_brownhair_mexican",
            "boy_green_hair",
            "boy_orangehair_darkskin",
            "boy_purple_hair",
            "girl_brown_hair",
            "girl_brownhair_mexican",
            "girl_darkskin_pinkhair",
            "girl_darkskin_purplehair",
            "girl_pink_hair",
            "girl_purple_hair"
        };

        public static string ToUrl(string? key)
        {
            if(string.IsNullOrWhiteSpace(key) || !Keys.Contains(key))
                return "/images/Avatar/boy_blue_hair.png";

            return $"/images/Avatar/{key}.png";
        }

        public static bool IsValid(string? key)
            => !string.IsNullOrWhiteSpace(key) && Keys.Contains(key);
    }
}