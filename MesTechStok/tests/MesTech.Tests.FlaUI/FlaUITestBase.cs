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
/// FlaUI Katman 2 test base — app launch, login, screenshot, sidebar click.
/// Her test sinifi tek app instance paylasir (IAsyncLifetime).
/// Screenshot convention: TEST-XX_EkranAdi_PASS.png / TEST-XX_EkranAdi_FAIL_HataTipi.png
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
    protected bool LoginSucceeded { get; private set; }

    protected FlaUITestBase(ITestOutputHelper output) => Output = output;

    public async Task InitializeAsync()
    {
        var repoRoot = FindRepoRoot();
        var exePath = Path.Combine(repoRoot, AppRelativePath);

        if (!File.Exists(exePath))
            throw new FileNotFoundException($"Avalonia exe bulunamadi: {exePath}");

        ScreenshotDir = Path.Combine(repoRoot, "tests", "MesTech.Tests.FlaUI", "Screenshots");
        Directory.CreateDirectory(ScreenshotDir);

        Automation = new UIA3Automation();
        App = Application.Launch(exePath);
        await Task.Delay(6000);

        var welcomeWindow = WaitForWindow("Giris Ekrani", 15000)
            ?? WaitForWindow("MesTech", 8000)
            ?? App.GetAllTopLevelWindows(Automation).FirstOrDefault();

        if (welcomeWindow is null)
            throw new InvalidOperationException("Uygulama penceresi bulunamadi");

        try
        {
            DoLogin(welcomeWindow);
            LoginSucceeded = true;
        }
        catch (Exception ex)
        {
            // Login form bulunamadı — "Beni Hatırla" ile otomatik login olmuş olabilir
            Output.WriteLine($"Login note: {ex.Message}");
            // MainWindow açıldıysa login başarılı sayılır
            var mw = WaitForWindow("MesTech", 5000);
            LoginSucceeded = mw is not null;
            if (LoginSucceeded)
                Output.WriteLine("Login: Auto-login (Beni Hatirla) — MainWindow acik");
        }

        await Task.Delay(4000);

        MainWindow = WaitForWindow("MesTech", 10000)
            ?? App.GetAllTopLevelWindows(Automation).FirstOrDefault()
            ?? welcomeWindow;
    }

    public Task DisposeAsync()
    {
        try { App?.Close(); } catch { }
        try { App?.Dispose(); } catch { }
        try { Automation?.Dispose(); } catch { }
        return Task.CompletedTask;
    }

    /// <summary>Screenshot al — PASS/FAIL convention.</summary>
    protected void Screenshot(string testId, string screenName, bool pass, string? failType = null)
    {
        try
        {
            var suffix = pass ? "PASS" : $"FAIL_{failType ?? "Unknown"}";
            var fileName = $"{testId}_{screenName}_{suffix}.png";
            Capture.Screen().ToFile(Path.Combine(ScreenshotDir, fileName));
            Output.WriteLine($"  Screenshot: {fileName}");
        }
        catch (Exception ex)
        {
            Output.WriteLine($"  Screenshot FAILED: {ex.Message}");
        }
    }

    /// <summary>Sidebar menü butonuna tıkla — Avalonia UIA3 uyumlu.
    /// Arama sırası: Button Name → TextBlock Text (child) → ToolTip → AutomationProperties.Name
    /// Avalonia butonların Name'i genelde TextBlock.Text veya ToolTip.Tip'ten gelir.</summary>
    protected bool ClickMenu(string menuName)
    {
        // İlk deneme — mevcut görünür elemanlarla
        if (TryClickMenuInternal(menuName)) return true;

        // Bulunamadı → sidebar'daki tüm Expander'ları aç ve tekrar dene
        ExpandAllSidebarGroups();
        Thread.Sleep(500);
        if (TryClickMenuInternal(menuName)) return true;

        // Hâlâ bulunamadı → sidebar'ı scroll edip tekrar dene
        ScrollSidebarToBottom();
        Thread.Sleep(500);
        return TryClickMenuInternal(menuName);
    }

    private void ExpandAllSidebarGroups()
    {
        try
        {
            var expanders = MainWindow.FindAllDescendants(
                CF.ByControlType(FlaUI.Core.Definitions.ControlType.Button));
            foreach (var exp in expanders)
            {
                try
                {
                    var name = exp.Name ?? "";
                    // Sidebar grup başlıkları genelde "▸" veya ">" icon + group name
                    if (exp.Patterns.ExpandCollapse.IsSupported)
                    {
                        var pattern = exp.Patterns.ExpandCollapse.Pattern;
                        if (pattern.ExpandCollapseState.Value == FlaUI.Core.Definitions.ExpandCollapseState.Collapsed)
                        {
                            pattern.Expand();
                            Thread.Sleep(200);
                        }
                    }
                }
                catch { /* Non-expandable button */ }
            }

            // Scroll sidebar to bottom to make all items visible
            try
            {
                var allScrollable = MainWindow.FindAllDescendants();
                foreach (var el in allScrollable)
                {
                    if (el.Patterns.Scroll.IsSupported)
                    {
                        try
                        {
                            el.Patterns.Scroll.Pattern.SetScrollPercent(-1, 100);
                            Thread.Sleep(100);
                            el.Patterns.Scroll.Pattern.SetScrollPercent(-1, 0);
                            Thread.Sleep(100);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }
        catch { }
    }

    private void ScrollSidebarToBottom()
    {
        try
        {
            // Mouse wheel ile sidebar'ı scroll et
            Mouse.Scroll(-20);
            Thread.Sleep(300);
        }
        catch { }
    }

    private bool TryClickMenuInternal(string menuName)
    {
        try
        {
            var all = MainWindow.FindAllDescendants();
            foreach (var el in all)
            {
                string name, helpText;
                try { name = el.Name ?? ""; } catch { continue; }
                try { helpText = el.HelpText ?? ""; } catch { helpText = ""; }

                // Eşleşme: Name veya HelpText (ToolTip) menuName içeriyor mu?
                var matches = name.Contains(menuName, StringComparison.OrdinalIgnoreCase)
                    || helpText.Contains(menuName, StringComparison.OrdinalIgnoreCase);

                if (!matches) continue;

                // Button ise doğrudan tıkla
                if (el.ControlType == FlaUI.Core.Definitions.ControlType.Button)
                {
                    try
                    {
                        el.Patterns.ScrollItem.PatternOrDefault?.ScrollIntoView();
                        Thread.Sleep(200);
                        el.AsButton().Click();
                        return true;
                    }
                    catch { continue; }
                }

                // TextBlock ise parent Button'ı bul
                if (el.ControlType == FlaUI.Core.Definitions.ControlType.Text)
                {
                    try
                    {
                        var parent = el.Parent;
                        while (parent is not null && parent.ControlType != FlaUI.Core.Definitions.ControlType.Button)
                            parent = parent.Parent;
                        if (parent is not null)
                        {
                            parent.Patterns.ScrollItem.PatternOrDefault?.ScrollIntoView();
                            Thread.Sleep(200);
                            parent.AsButton().Click();
                            return true;
                        }
                    }
                    catch { continue; }
                }
            }
        }
        catch (Exception ex)
        {
            Output.WriteLine($"  ClickMenu exception: {ex.Message}");
        }
        return false;
    }


    /// <summary>Ekranda belirli metni ara.</summary>
    protected bool ContainsText(string search)
    {
        try
        {
            var allTexts = MainWindow.FindAllDescendants(
                CF.ByControlType(FlaUI.Core.Definitions.ControlType.Text));
            foreach (var t in allTexts)
            {
                try
                {
                    if ((t.Name ?? "").Contains(search, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                catch { continue; }
            }
        }
        catch { }
        return false;
    }

    /// <summary>Ekranda hata metni ara — false positive filtreli.</summary>
    protected string? FindError(params string[] errorPatterns)
    {
        var defaults = new[] { "Hata", "Error", "Exception", "column does not exist",
            "relation does not exist", "second operation", "DateTime Kind" };
        var patterns = errorPatterns.Length > 0 ? errorPatterns : defaults;

        try
        {
            var allTexts = MainWindow.FindAllDescendants(
                CF.ByControlType(FlaUI.Core.Definitions.ControlType.Text));
            foreach (var t in allTexts)
            {
                string text;
                try { text = t.Name ?? ""; } catch { continue; }
                if (text.Length > 300 || text.Contains("0 Hata") || text.Contains("0 Error"))
                    continue;

                foreach (var p in patterns)
                {
                    if (text.Contains(p, StringComparison.OrdinalIgnoreCase))
                        return text.Trim();
                }
            }
        }
        catch { }
        return null;
    }

    /// <summary>Belirli ControlType sayısını say.</summary>
    protected int CountElements(FlaUI.Core.Definitions.ControlType controlType, string? nameContains = null)
    {
        try
        {
            var all = MainWindow.FindAllDescendants(CF.ByControlType(controlType));
            if (nameContains is null) return all.Length;
            return all.Count(e =>
            {
                try { return (e.Name ?? "").Contains(nameContains, StringComparison.OrdinalIgnoreCase); }
                catch { return false; }
            });
        }
        catch { return -1; }
    }

    private void DoLogin(Window window)
    {
        // Avalonia UIA: x:Name → AutomationId desteksiz. Watermark/TextBlock Text ile ara.
        AutomationElement? userEl = null, passEl = null, loginEl = null;

        // Username TextBox — Watermark "Kullanici adinizi girin" veya label "Kullanici Adi"
        var all = window.FindAllDescendants();
        foreach (var el in all)
        {
            string name, helpText;
            try { name = el.Name ?? ""; helpText = el.HelpText ?? ""; } catch { continue; }

            if (el.ControlType == FlaUI.Core.Definitions.ControlType.Edit
                || el.ControlType == FlaUI.Core.Definitions.ControlType.Document)
            {
                if (name.Contains("Kullanici", StringComparison.OrdinalIgnoreCase)
                    || helpText.Contains("Kullanici", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("username", StringComparison.OrdinalIgnoreCase))
                {
                    if (userEl is null) userEl = el;
                    else passEl = el; // İkinci Edit = password
                }
                else if (name.Contains("Sifre", StringComparison.OrdinalIgnoreCase)
                    || helpText.Contains("Sifre", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("password", StringComparison.OrdinalIgnoreCase))
                {
                    passEl = el;
                }
            }

            if (el.ControlType == FlaUI.Core.Definitions.ControlType.Button
                && (name.Contains("GIRIS", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Login", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Giriş", StringComparison.OrdinalIgnoreCase)))
            {
                loginEl = el;
            }
        }

        // Fallback: sırayla ilk 2 Edit elemanı = username, password
        if (userEl is null)
        {
            var edits = all.Where(e =>
            {
                try { return e.ControlType == FlaUI.Core.Definitions.ControlType.Edit; } catch { return false; }
            }).Take(2).ToArray();
            if (edits.Length >= 1) userEl = edits[0];
            if (edits.Length >= 2) passEl = edits[1];
        }

        // Fallback: "GIRIS YAP" text'ini içeren TextBlock'un parent Button'ı
        if (loginEl is null)
        {
            foreach (var el in all)
            {
                try
                {
                    if (el.ControlType == FlaUI.Core.Definitions.ControlType.Text
                        && (el.Name ?? "").Contains("GIRIS", StringComparison.OrdinalIgnoreCase))
                    {
                        var parent = el.Parent;
                        while (parent is not null && parent.ControlType != FlaUI.Core.Definitions.ControlType.Button)
                            parent = parent.Parent;
                        if (parent is not null) { loginEl = parent; break; }
                    }
                }
                catch { continue; }
            }
        }

        if (userEl is null || loginEl is null)
        {
            // Debug dump
            Output.WriteLine("LOGIN DEBUG — Element dump:");
            var count = 0;
            foreach (var el in all)
            {
                if (count++ > 50) break;
                try { Output.WriteLine($"  Type={el.ControlType} Name='{el.Name}' Help='{el.HelpText}'"); } catch { }
            }
            throw new InvalidOperationException($"Login form bulunamadi (user={userEl is not null}, login={loginEl is not null})");
        }

        userEl.AsTextBox().Click(); Thread.Sleep(300);
        Keyboard.Type(Username); Thread.Sleep(300);
        if (passEl is not null) { passEl.AsTextBox().Click(); Thread.Sleep(300); Keyboard.Type(Password); Thread.Sleep(300); }
        loginEl.AsButton().Click(); Thread.Sleep(2000);
    }

    protected Window? WaitForWindow(string titleContains, int timeoutMs)
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
            catch { }
            Thread.Sleep(500);
        }
        return null;
    }

    private static string FindRepoRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir, "src", "MesTech.Avalonia"))) return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return @"E:\MesTech\MesTech\MesTech_Stok\MesTechStok";
    }
}
