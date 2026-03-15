using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Themes.Fluent;

namespace MesTechStok.Avalonia.Tests;

/// <summary>
/// Minimal Avalonia Application for headless testing.
/// Uses FluentTheme (same as production MesTech.Avalonia.App) but without
/// DI/Infrastructure wiring — tests provide their own mocks.
/// </summary>
public class TestApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }
}
