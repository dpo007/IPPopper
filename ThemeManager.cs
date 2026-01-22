using Microsoft.Win32;
using System.Windows;

namespace IPPopper
{
    /// <summary>
    /// Manages application theme based on Windows system dark mode setting.
    /// </summary>
    public static class ThemeManager
    {
        private const string RegistryPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegistryKey = "AppsUseLightTheme";

        /// <summary>
        /// Detects if Windows is in dark mode and applies the appropriate theme.
        /// </summary>
        public static void ApplySystemTheme()
        {
            bool isDarkMode = IsSystemDarkMode();
            ApplyTheme(isDarkMode);
        }

        private static bool _isDarkMode = IsSystemDarkMode();

        /// <summary>
        /// Applies the specified theme to the application.
        /// </summary>
        /// <param name="isDarkMode">True for dark theme, false for light theme.</param>
        public static void ApplyTheme(bool isDarkMode)
        {
            _isDarkMode = isDarkMode;
            ResourceDictionary theme = new ResourceDictionary
            {
                Source = new Uri(isDarkMode ? "pack://application:,,,/Themes/DarkTheme.xaml" : "pack://application:,,,/Themes/LightTheme.xaml")
            };

            // Remove existing theme
            foreach (ResourceDictionary? dict in System.Windows.Application.Current.Resources.MergedDictionaries.ToList())
            {
                if (dict.Source?.OriginalString.Contains("/Themes/") == true)
                {
                    System.Windows.Application.Current.Resources.MergedDictionaries.Remove(dict);
                }
            }

            // Add new theme
            System.Windows.Application.Current.Resources.MergedDictionaries.Add(theme);
        }

        /// <summary>
        /// Toggles between light and dark themes.
        /// </summary>
        public static void ToggleTheme()
        {
            ApplyTheme(!_isDarkMode);
        }

        /// <summary>
        /// Checks if the system is in dark mode.
        /// </summary>
        /// <returns>True if system is in dark mode, false otherwise.</returns>
        private static bool IsSystemDarkMode()
        {
            try
            {
                object? registryValue = Registry.GetValue(RegistryPath, RegistryKey, 1);
                if (registryValue is int value)
                {
                    // 0 = Dark mode, 1 = Light mode
                    return value == 0;
                }
            }
            catch
            {
                // If registry read fails, default to light theme
            }

            return false;
        }
    }
}
