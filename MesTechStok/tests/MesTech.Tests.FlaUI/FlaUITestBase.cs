using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Conditions;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using Xunit.Abstractions;

namespace MesTech.Tests.UIAutomation;

/// <summary>
/// FlaUI E2E test base class — uygulama başlat, login yap, temizle.
/// Her test sınıfı bunu extend eder. IAsyncLifetime ile app lifecycle.
/// </summary>
public abstract class FlaUITestBase : IAsyncLifetime
{
    private const string AppRelativePath = @"src\MesTech.Avalonia\bin\Debug\net9.0\MesTech.Avalonia.exe";
    protected const string Username = "admin";
    protected const string Password = "Admin123!";

    protected UIA3Automation Automation { get; private set; } = null!;
    protected Application App { get; private set; } = null!;
    protected Window MainWindow { get; private set; } = null!;
    protected ConditionFactory CF => Automation.ConditionFactory;
    protected ITestOutputHelper Output { get; }
    protected string ScreenshotDir { get; private set; } = null!;

    protected FlaUITestBase(ITestOutputHelper output) => Output = output;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var exePath = Path.Combine(repoRoot, AppRelativePath);

        if (!File.Exists(exePath))
            throw new FileNotFoundException($"Avalonia exe bulunamadı. Önce build edin: {exePath}");

        ScreenshotDir = Path.Combine(repoRoot, "tests", "MesTech.Tests.FlaUI", "screenshots");
        Directory.CreateDirectory(ScreenshotDir);

        Automation = new UIA3Automation();
        App = Application.Launch(exePath);

        // Splash + DI init bekle
        await Task.Delay(6000);

        // WelcomeWindow bul
        var welcomeWindow = WaitForWindow("Giris Ekrani", 15000)
            ?? WaitForWindow("MesTech", 5000);

        if (welcomeWindow is null)
        {
            var windows = App.GetAllTopLevelWindows(Automation);
            welcomeWindow = windows.Length > 0 ? windows[0] : null;
        }

        if (welcomeWindow is null)
            throw new InvalidOperationException("Uygulama penceresi bulunamadı");

        TakeScreenshot(welcomeWindow, "00_welcome");

        // Login
        DoLogin(welcomeWindow);
        await Task.Delay(4000); // MainWindow yüklenmesini bekle

        // MainWindow bul
        MainWindow = WaitForWindow("MesTech", 10000)
            ?? App.GetAllTopLevelWindows(Automation).FirstOrDefault()
            ?? welcomeWindow;

        TakeScreenshot(MainWindow, "01_after_login");
        Output.WriteLine($"Login OK — Window: {MainWindow.Title}");
    }

    public Task DisposeAsync()
    {
        try { App?.Close(); } catch { /* ignore */ }
        try { App?.Dispose(); } catch { /* ignore */ }
        try { Automation?.Dispose(); } catch { /* ignore */ }
        return Task.CompletedTask;
    }

    protected void TakeScreenshot(Window window, string name)
    {
        try
        {
            var capture = Capture.Screen();
            capture.ToFile(Path.Combine(ScreenshotDir, $"{name}.png"));
        }
        catch (Exception ex)
        {
            Output.WriteLine($"Screenshot failed: {ex.Message}");
        }
    }

    protected bool ClickSidebarMenu(string menuName)
    {
        try
        {
            var allButtons = MainWindow.FindAllDescendants(
                CF.ByControlType(FlaUI.Core.Definitions.ControlType.Button));

            foreach (var b in allButtons)
            {
                string name;
                try { name = b.Name ?? ""; } catch { continue; }

                if (name.Equals(menuName, StringComparison.OrdinalIgnoreCase))
                {
                    try { b.AsButton().Click(); return true; }
                    catch
                    {
                        try
                        {
                            b.Patterns.ScrollItem.PatternOrDefault?.ScrollIntoView();
                            Thread.Sleep(300);
                            b.AsButton().Click();
                            return true;
                        }
                        catch { continue; }
                    }
                }
            }

            // Fallback: ByName
            try
            {
                var btn = MainWindow.FindFirstDescendant(CF.ByName(menuName))?.AsButton();
                if (btn is not null) { btn.Click(); return true; }
            }
            catch { /* Avalonia UIA limitation */ }
        }
        catch { /* COM error guard */ }
        return false;
    }

    protected string? FindErrorText()
    {
        try
        {
            var allTexts = MainWindow.FindAllDescendants(
                CF.ByControlType(FlaUI.Core.Definitions.ControlType.Text));

            foreach (var t in allTexts)
            {
                string text;
                try { text = t.Name ?? ""; } catch { continue; }

                if ((text.Contains("Hata", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("Error", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("Exception", StringComparison.OrdinalIgnoreCase))
                    && !text.Contains("0 Hata") && !text.Contains("0 Error")
                    && text.Length < 200)
                {
                    return text.Trim();
                }
            }
        }
        catch { /* Avalonia UIA */ }
        return null;
    }

    private void DoLogin(Window window)
    {
        AutomationElement? usernameEl = null, passwordEl = null, loginEl = null;

        try { usernameEl = window.FindFirstDescendant(CF.ByAutomationId("UsernameBox")); } catch { }
        usernameEl ??= window.FindFirstDescendant(CF.ByName("UsernameBox"))
            ?? window.FindFirstDescendant(CF.ByName("Kullanıcı Adı"));

        try { passwordEl = window.FindFirstDescendant(CF.ByAutomationId("PasswordBox")); } catch { }
        passwordEl ??= window.FindFirstDescendant(CF.ByName("PasswordBox"))
            ?? window.FindFirstDescendant(CF.ByName("Şifre"));

        try { loginEl = window.FindFirstDescendant(CF.ByAutomationId("LoginButton")); } catch { }
        loginEl ??= window.FindFirstDescendant(CF.ByName("LoginButton"))
            ?? window.FindFirstDescendant(CF.ByName("GİRİŞ YAP"));

        if (usernameEl is null || loginEl is null)
            throw new InvalidOperationException("Login form bulunamadı");

        usernameEl.AsTextBox().Click();
        Thread.Sleep(200);
        Keyboard.Type(Username);
        Thread.Sleep(200);

        if (passwordEl is not null)
        {
            passwordEl.AsTextBox().Click();
            Thread.Sleep(200);
            Keyboard.Type(Password);
            Thread.Sleep(200);
        }

        loginEl.AsButton().Click();
        Thread.Sleep(2000);
    }

    private Window? WaitForWindow(string titleContains, int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                foreach (var w in App.GetAllTopLevelWindows(Automation))
                    if ((w.Title ?? "").Contains(titleContains, StringComparison.OrdinalIgnoreCase))
                        return w;
            }
            catch { /* not ready */ }
            Thread.Sleep(500);
        }
        return null;
    }

    private static string FindRepoRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir, "src", "MesTech.Avalonia")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return @"E:\MesTech\MesTech\MesTech_Stok\MesTechStok";
    }
}
