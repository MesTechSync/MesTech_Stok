using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(MesTechStok.Avalonia.Tests.TestAppBuilder))]

namespace MesTechStok.Avalonia.Tests;

/// <summary>
/// Configures the Avalonia headless test application.
/// This builder is referenced by the [AvaloniaTestApplication] assembly attribute
/// and provides the AppBuilder that Avalonia.Headless.XUnit uses for [AvaloniaFact] tests.
/// </summary>
public sealed class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
