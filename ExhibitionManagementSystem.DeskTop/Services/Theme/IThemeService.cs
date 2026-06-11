namespace ExhibitionManagementSystem.DeskTop.Services.Theme;

public interface IThemeService
{
    bool IsDarkTheme { get; }
    void ToggleTheme();
}
