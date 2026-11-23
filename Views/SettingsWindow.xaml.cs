using System.Windows;
using SortingLabApp.Themes;

namespace SortingLabApp.Views
{
    public partial class SettingsWindow : Window
    {
        private bool _isInitializing = true;

        public SettingsWindow()
        {
            InitializeComponent();

            if (ThemeManager.IsDarkTheme)
            {
                RbDarkTheme.IsChecked = true;
            }
            else
            {
                RbLightTheme.IsChecked = true;
            }

            _isInitializing = false;
        }

        private void ThemeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            bool isDark = RbDarkTheme.IsChecked == true;
            ThemeManager.SetTheme(isDark);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
