using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace MesTechStok.Avalonia.Tests;

/// <summary>
/// Minimal Avalonia Application for headless testing.
/// Uses FluentTheme + production MesTechTheme resources so that
/// StaticResource references (CornerRadiusMedium, brushes, etc.) resolve
/// correctly during headless rendering of MainWindow and other views.
/// </summary>
public class TestApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());

        // Load the same resource dictionaries used by the production App.axaml
        // so that StaticResource lookups succeed in headless tests.
        var themeResources = new ResourceInclude(new System.Uri("avares://MesTech.Avalonia"))
        {
            Source = new System.Uri("avares://MesTech.Avalonia/Styles/MesTechTheme.axaml")
        };
        Resources.MergedDictionaries.Add(themeResources);

        var designTokens = new ResourceInclude(new System.Uri("avares://MesTech.Avalonia"))
        {
            Source = new System.Uri("avares://MesTech.Avalonia/Themes/MesTechDesignTokens.axaml")
        };
        Resources.MergedDictionaries.Add(designTokens);

        // Load component styles that views reference
        Styles.Add(new StyleInclude(new System.Uri("avares://MesTech.Avalonia"))
        {
            Source = new System.Uri("avares://MesTech.Avalonia/Themes/Controls/ButtonStyles.axaml")
        });
        Styles.Add(new StyleInclude(new System.Uri("avares://MesTech.Avalonia"))
        {
            Source = new System.Uri("avares://MesTech.Avalonia/Themes/Controls/CardStyles.axaml")
        });
        Styles.Add(new StyleInclude(new System.Uri("avares://MesTech.Avalonia"))
        {
            Source = new System.Uri("avares://MesTech.Avalonia/Themes/Controls/SidebarStyles.axaml")
        });
        Styles.Add(new StyleInclude(new System.Uri("avares://MesTech.Avalonia"))
        {
            Source = new System.Uri("avares://MesTech.Avalonia/Themes/MesTechComponentStyles.axaml")
        });
    }
}
