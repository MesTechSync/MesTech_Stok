using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace MesTechStok.Desktop.Services;

/// <summary>
/// Dark/Light tema geçişi yönetimi.
/// MaterialDesignInXAML PaletteHelper kullanarak tüm View'ları etkiler.
/// </summary>
public sealed class ThemeService : ObservableObject
{
    private bool _isDarkMode;

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (SetProperty(ref _isDarkMode, value))
                ApplyTheme(value);
        }
    }

    public ThemeService()
    {
        // Varsayılan: Light tema
        _isDarkMode = false;
    }

    private void ApplyTheme(bool isDark)
    {
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();

        theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);

        // MesTech marka renkleri korunur:
        theme.SetPrimaryColor(Color.FromRgb(0x15, 0x65, 0xC0)); // #1565C0
        theme.SetSecondaryColor(Color.FromRgb(0x00, 0x89, 0x7B)); // #00897B

        paletteHelper.SetTheme(theme);
    }

    public void Toggle() => IsDarkMode = !IsDarkMode;
}
