using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using MesTech.Avalonia.Views;
using Xunit;

namespace MesTech.Tests.Headless;

[Trait("Category", "Headless")]
[Trait("Layer", "UI")]
public class LoginFlowTests
{
    private const string ScreenshotDir = "screenshots";

    public LoginFlowTests()
    {
        Directory.CreateDirectory(ScreenshotDir);
    }

    [AvaloniaFact]
    public void WelcomeWindow_Should_Render_LoginCard()
    {
        // WelcomeWindow = Spotlight login ekrani
        var window = new WelcomeWindow
        {
            Width = 1280,
            Height = 720
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Screenshot 1: Login ekrani gorunuyor mu?
        var frame1 = window.CaptureRenderedFrame();
        Assert.NotNull(frame1);
        Assert.True(frame1!.PixelSize.Width > 0, "WelcomeWindow render BASARISIZ");
        frame1.Save(Path.Combine(ScreenshotDir, "login_01_welcome.png"));

        // UsernameBox var mi?
        var usernameBox = window.FindControl<TextBox>("UsernameBox");
        Assert.NotNull(usernameBox);

        window.Close();
    }

    [AvaloniaFact]
    public void WelcomeWindow_Should_Accept_KeyboardInput()
    {
        var window = new WelcomeWindow
        {
            Width = 1280,
            Height = 720
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Username input'a yaz
        var usernameBox = window.FindControl<TextBox>("UsernameBox");
        Assert.NotNull(usernameBox);
        usernameBox!.Focus();
        Dispatcher.UIThread.RunJobs();

        window.KeyTextInput("admin");
        Dispatcher.UIThread.RunJobs();

        // Yazilan metin kontrol
        Assert.Equal("admin", usernameBox.Text);

        // Screenshot: input dolu hali
        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        frame!.Save(Path.Combine(ScreenshotDir, "login_02_username_filled.png"));

        window.Close();
    }

    [AvaloniaFact]
    public void WelcomeWindow_Should_Have_LoginButton()
    {
        var window = new WelcomeWindow
        {
            Width = 1280,
            Height = 720
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Login butonu var mi?
        var loginButton = window.FindControl<Button>("LoginButton");
        Assert.NotNull(loginButton);

        // Buton tiklanabilir mi?
        Assert.True(loginButton!.IsEnabled, "LoginButton DISABLED — tiklanamaz");

        window.Close();
    }

    [AvaloniaFact]
    public void LoginAvaloniaView_Should_Show_FormElements()
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
        Assert.True(frame!.PixelSize.Width > 0, "LoginAvaloniaView render BASARISIZ");
        frame.Save(Path.Combine(ScreenshotDir, "login_03_standalone_view.png"));

        window.Close();
    }
}
