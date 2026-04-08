using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;
using MesTech.Avalonia.Converters;
using ResourceInclude = global::Avalonia.Markup.Xaml.Styling.ResourceInclude;
using StyleInclude = global::Avalonia.Markup.Xaml.Styling.StyleInclude;

[assembly: AvaloniaTestApplication(typeof(MesTech.Tests.Headless.TestAppBuilder))]

namespace MesTech.Tests.Headless;

/// <summary>
/// Headless test icin Avalonia Application.
/// Production App.axaml'in kaynaklarini (converter, theme token) yukler.
/// DI/Host oluşturulmaz — sadece render icin gerekli olanlar.
/// </summary>
public class HeadlessTestApp : global::Avalonia.Application
{
    public override void Initialize()
    {
        var baseUri = new Uri("avares://MesTech.Tests.Headless");

        Styles.Add(new FluentTheme());

        // DataGrid Fluent theme
        try
        {
            Styles.Add(new StyleInclude(baseUri)
            {
                Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml")
            });
        }
        catch { /* DataGrid theme opsiyonel */ }

        // Production App.axaml kaynaklari — converter'lar
        Resources.Add("EqualConverter", new EqualConverter());
        Resources.Add("GreaterThanConverter", new GreaterThanConverter());
        Resources.Add("BetweenConverter", new BetweenConverter());
        Resources.Add("StatusToColorConverter", new StatusToColorConverter());
        Resources.Add("StockLevelToBrushConverter", new StockLevelToBrushConverter());

        // MesTech tema ve design token'larini yukle
        try
        {
            Resources.MergedDictionaries.Add(new ResourceInclude(baseUri)
            {
                Source = new Uri("avares://MesTech.Avalonia/Styles/MesTechTheme.axaml")
            });
            Resources.MergedDictionaries.Add(new ResourceInclude(baseUri)
            {
                Source = new Uri("avares://MesTech.Avalonia/Themes/MesTechDesignTokens.axaml")
            });
        }
        catch { /* Tema dosyalari bulunamazsa devam et */ }

        // MesTech component stillerini yukle
        string[] styleFiles = [
            "Themes/Controls/ButtonStyles.axaml",
            "Themes/Controls/CardStyles.axaml",
            "Themes/Controls/DataGridStyles.axaml",
            "Themes/Controls/InputStyles.axaml",
            "Themes/Controls/SidebarStyles.axaml",
            "Themes/Controls/DialogStyles.axaml",
            "Themes/MesTechComponentStyles.axaml",
            "Themes/Controls/TypographyStyles.axaml",
            "Themes/Controls/AccessibilityStyles.axaml",
            "Themes/MesTechIcons.axaml"
        ];

        foreach (var styleFile in styleFiles)
        {
            try
            {
                Styles.Add(new StyleInclude(baseUri)
                {
                    Source = new Uri($"avares://MesTech.Avalonia/{styleFile}")
                });
            }
            catch { /* Eksik stil dosyasi atlaniyor */ }
        }
    }
}

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<HeadlessTestApp>()
            .UseSkia()                // Skia renderer + IFontManagerImpl kaydi
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false // false = gercek Skia render, CaptureRenderedFrame icin ZORUNLU
            });
}
