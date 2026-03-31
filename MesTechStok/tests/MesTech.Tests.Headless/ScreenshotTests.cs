using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using MesTech.Avalonia.Views;
using Xunit;

namespace MesTech.Tests.Headless;

[Trait("Category", "Headless")]
[Trait("Layer", "UI")]
public class ScreenshotTests
{
    private const string ScreenshotDir = "screenshots";

    public ScreenshotTests()
    {
        Directory.CreateDirectory(ScreenshotDir);
    }

    [AvaloniaFact]
    public void LoginAvaloniaView_Should_Render()
    {
        var window = new Window
        {
            Width = 1280,
            Height = 720,
            Content = new LoginAvaloniaView()
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        Assert.True(frame!.PixelSize.Width > 0, "Screenshot genisligi 0 — render BASARISIZ");
        Assert.True(frame.PixelSize.Height > 0, "Screenshot yuksekligi 0 — render BASARISIZ");
        frame.Save(Path.Combine(ScreenshotDir, "01_LoginAvaloniaView.png"));

        window.Close();
    }

    [AvaloniaFact]
    public void DashboardAvaloniaView_Should_Render()
    {
        var window = new Window
        {
            Width = 1280,
            Height = 720,
            Content = new DashboardAvaloniaView()
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        Assert.True(frame!.PixelSize.Width > 0, "Dashboard render BASARISIZ — pixel width 0");
        frame.Save(Path.Combine(ScreenshotDir, "02_DashboardAvaloniaView.png"));

        window.Close();
    }

    [AvaloniaFact]
    public void ProductsAvaloniaView_Should_Render()
    {
        var window = new Window
        {
            Width = 1280,
            Height = 720,
            Content = new ProductsAvaloniaView()
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        Assert.True(frame!.PixelSize.Width > 0, "Products render BASARISIZ — pixel width 0");
        frame.Save(Path.Combine(ScreenshotDir, "03_ProductsAvaloniaView.png"));

        window.Close();
    }

    [AvaloniaFact]
    public void SettingsAvaloniaView_Should_Render()
    {
        var window = new Window
        {
            Width = 1280,
            Height = 720,
            Content = new SettingsAvaloniaView()
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        Assert.True(frame!.PixelSize.Width > 0, "Settings render BASARISIZ — pixel width 0");
        frame.Save(Path.Combine(ScreenshotDir, "04_SettingsAvaloniaView.png"));

        window.Close();
    }
}
