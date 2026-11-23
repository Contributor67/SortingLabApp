using System;
using System.Windows;

namespace SortingLabApp.Themes
{
    public static class ThemeManager
    {
        public static bool IsDarkTheme { get; private set; } = true;

        public static event Action? ThemeChanged;

        public static void SetTheme(bool isDark)
        {
            IsDarkTheme = isDark;
            var themeUri = isDark 
                ? new Uri("Themes/DarkTheme.xaml", UriKind.Relative) 
                : new Uri("Themes/LightTheme.xaml", UriKind.Relative);

            var newDict = new ResourceDictionary { Source = themeUri };

            var app = Application.Current;
            if (app != null)
            {
                // Заменяем первый словарь темы или добавляем
                var merged = app.Resources.MergedDictionaries;
                if (merged.Count > 0)
                {
                    merged[0] = newDict;
                }
                else
                {
                    merged.Add(newDict);
                }
            }

            ThemeChanged?.Invoke();
        }

        public static void ToggleTheme()
        {
            SetTheme(!IsDarkTheme);
        }
    }
}
