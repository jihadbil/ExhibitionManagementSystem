using CommunityToolkit.Mvvm.ComponentModel;

namespace ExhibitionManagementSystem.Desktop.Services.Theme
{
    public class ThemeService : ObservableObject, IThemeService
    {
        private AppTheme _currentTheme = AppTheme.Light;

        public AppTheme CurrentTheme => _currentTheme;

        public void SwitchTheme(AppTheme theme)
        {
            _currentTheme = theme;
            // يمكن إضافة تبديل القواميس البرمجية للوضع الداكن لاحقاً هنا
        }
    }
}
