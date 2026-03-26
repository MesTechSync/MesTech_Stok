using Avalonia.Styling;

namespace MesTech.Avalonia.Services;

/// <summary>
/// Runtime theme switching with persistence to user preferences file.
/// Supports Light, Dark, and System-following modes.
/// </summary>
public interface IThemeService
{
    string CurrentTheme { get; }
    void SetTheme(string theme); // "Light", "Dark", "System"
    void LoadSavedTheme();
    event EventHandler<string>? ThemeChanged;
}

public class ThemeService : IThemeService
{
    private static readonly string PrefsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MesTech", "theme.txt");

    private string _currentTheme = "Light";

    public string CurrentTheme => _currentTheme;

    public event EventHandler<string>? ThemeChanged;

    public void SetTheme(string theme)
    {
        _currentTheme = theme switch
        {
            "Dark"   => "Dark",
            "System" => "System",
            _        => "Light"
        };

        ApplyToAvalonia(_currentTheme);
        PersistTheme(_currentTheme);
        ThemeChanged?.Invoke(this, _currentTheme);
    }

    public void LoadSavedTheme()
    {
        try
        {
            if (File.Exists(PrefsPath))
            {
                var saved = File.ReadAllText(PrefsPath).Trim();
                _currentTheme = saved switch
                {
                    "Dark"   => "Dark",
                    "System" => "System",
                    _        => "Light"
                };
            }
        }
        catch
        {
            _currentTheme = "Light";
        }

        ApplyToAvalonia(_currentTheme);
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static void ApplyToAvalonia(string theme)
    {
        if (global::Avalonia.Application.Current is null) return;

        global::Avalonia.Application.Current.RequestedThemeVariant = theme switch
        {
            "Dark"   => ThemeVariant.Dark,
            "System" => ThemeVariant.Default,
            _        => ThemeVariant.Light
        };
    }

    private static void PersistTheme(string theme)
    {
        try
        {
            var dir = Path.GetDirectoryName(PrefsPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(PrefsPath, theme);
        }
        catch
        {
            // Non-fatal — preference save failure does not break runtime
        }
    }
}
