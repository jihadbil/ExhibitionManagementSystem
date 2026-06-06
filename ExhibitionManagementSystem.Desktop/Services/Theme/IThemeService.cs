namespace ExhibitionManagementSystem.Desktop.Services.Theme
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    public interface IThemeService
    {
        AppTheme CurrentTheme { get; }
        void SwitchTheme(AppTheme theme);
    }
}
