using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Theme
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string CssFile { get; set; }
        public string PreviewImage { get; set; }
        public bool IsDefault { get; set; }
    }

    public static class ThemeConstants
    {
        public const string CLASSIC_THEME = "classic";
        public const string MODERN_THEME = "modern";
        public const string DARK_THEME = "dark";
        public const string BLUE_SILVER_THEME = "blue-silver";
        public const string THEME_COOKIE_NAME = "SelectedTheme";
        
        public static readonly List<Theme> AvailableThemes = new List<Theme>
        {
            new Theme
            {
                Id = CLASSIC_THEME,
                Name = "classic",
                DisplayName = "Classic Theme",
                Description = "Original theme with traditional styling",
                CssFile = "/css/themes/classic-theme.css",
                PreviewImage = "/images/themes/classic-preview.png",
                IsDefault = true
            },
            new Theme
            {
                Id = MODERN_THEME,
                Name = "modern",
                DisplayName = "Modern Theme",
                Description = "Stylish and modern design with enhanced UX",
                CssFile = "/css/themes/modern-theme.css",
                PreviewImage = "/images/themes/modern-preview.png",
                IsDefault = false
            },
            new Theme
            {
                Id = DARK_THEME,
                Name = "dark",
                DisplayName = "Dark Theme",
                Description = "Elegant dark mode with sophisticated styling",
                CssFile = "/css/themes/dark-theme.css",
                PreviewImage = "/images/themes/dark-preview.png",
                IsDefault = false
            },
            new Theme
            {
                Id = BLUE_SILVER_THEME,
                Name = "blue-silver",
                DisplayName = "Blue Silver Theme",
                Description = "Professional corporate design with blue and silver accents",
                CssFile = "/css/themes/blue-silver-theme.css",
                PreviewImage = "/images/themes/blue-silver-preview.png",
                IsDefault = false
            }
        };
    }
}