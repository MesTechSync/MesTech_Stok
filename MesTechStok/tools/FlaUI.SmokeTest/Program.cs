using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Conditions;
using FlaUI.Core.Input;
using FlaUI.UIA3;

namespace FlaUI.SmokeTest;

/// <summary>
/// MesTech Avalonia E2E Smoke Test — FlaUI UIA3.
/// Gerçek pencere açar, login yapar, sidebar menüleri tıklar, screenshot alır.
/// Kullanım: dotnet run --project tools/FlaUI.SmokeTest/
/// </summary>
public static class Program
{
    private const string AppPath = @"src\MesTech.Avalonia\bin\Debug\net9.0\MesTech.Avalonia.exe";
    private const string Username = "admin";
    private const string Password = "Admin123!";
    private const int WaitMs = 2000;
    private const int NavigateWaitMs = 1500;

    // İlk 17 ana menü (en kritik ekranlar)
    private static readonly string[] MenuItems =
    [
        "Dashboard", "Products", "Orders", "Stock", "StockMovement",
        "InvoiceList", "Marketplaces", "Trendyol", "Hepsiburada",
        "Reports", "Customers", "CargoTracking", "Settings",
        "Health", "AccountingDashboard", "Inventory", "Category"
    ];

    private static string _screenshotDir = null!;
    private static readonly List<string> Errors = [];
    private static readonly List<string> Successes = [];

    public static int Main(string[] args)
    {
        var baseDir = FindRepoRoot();
        if (baseDir is null)
        {
            Console.Error.WriteLine("ERROR: MesTechStok repo root bulunamadı.");
            return 1;
        }

        var exePath = Path.Combine(baseDir, AppPath);
        if (!File.Exists(exePath))
        {
            Console.Error.WriteLine($"ERROR: Avalonia exe bulunamadı: {exePath}");
            Console.Error.WriteLine("Önce: dotnet build src/MesTech.Avalonia/");
            return 1;
        }

        _screenshotDir = Path.Combine(baseDir, "tools", "FlaUI.SmokeTest", "screenshots");
        Directory.CreateDirectory(_screenshotDir);

        Console.WriteLine($"=== MesTech FlaUI Smoke Test ===");
        Console.WriteLine($"Exe: {exePath}");
        Console.WriteLine($"Screenshots: {_screenshotDir}");
        Console.WriteLine();

        using var automation = new UIA3Automation();
        Application? app = null;

        try
        {
            // 1. Uygulamayı başlat
            Console.Write("Uygulama başlatılıyor... ");
            app = Application.Launch(exePath);
            Thread.Sleep(5000); // Splash + DI init bekle
            Console.WriteLine("OK");

            // 2. WelcomeWindow'u bul
            var mainWindow = WaitForWindow(app, automation, "Giris Ekrani", 15000)
                ?? WaitForWindow(app, automation, "MesTech", 5000);

            if (mainWindow is null)
            {
                // Pencere listesini dene
                var windows = app.GetAllTopLevelWindows(automation);
                if (windows.Length > 0)
                    mainWindow = windows[0];
            }

            if (mainWindow is null)
            {
                Errors.Add("FATAL: Uygulama penceresi bulunamadı");
                PrintReport();
                return 1;
            }

            TakeScreenshot(mainWindow, "00_welcome");
            Console.WriteLine($"Pencere bulundu: {mainWindow.Title}");

            // 3. Login
            Console.Write("Login yapılıyor... ");
            var loginResult = DoLogin(mainWindow, automation);
            if (!loginResult)
            {
                Errors.Add("FATAL: Login başarısız — UsernameBox veya LoginButton bulunamadı");
                TakeScreenshot(mainWindow, "00_login_failed");
                PrintReport();
                return 1;
            }

            Thread.Sleep(3000); // MainWindow yüklenmesini bekle

            // Login sonrası pencere değişmiş olabilir
            var appWindow = WaitForWindow(app, automation, "MesTech", 10000);
            if (appWindow is null)
            {
                var windows = app.GetAllTopLevelWindows(automation);
                appWindow = windows.Length > 0 ? windows[0] : mainWindow;
            }

            TakeScreenshot(appWindow, "01_after_login");
            Console.WriteLine("OK");

            // 4. Sidebar menülerini tıkla
            Console.WriteLine($"\n=== {MenuItems.Length} Menü Taranıyor ===\n");

            for (int i = 0; i < MenuItems.Length; i++)
            {
                var menuName = MenuItems[i];
                var idx = $"{i + 2:D2}";

                try
                {
                    Console.Write($"  [{idx}] {menuName,-25} ");

                    var clicked = ClickSidebarButton(appWindow, automation, menuName);
                    if (!clicked)
                    {
                        Errors.Add($"{menuName}: Sidebar butonu bulunamadı");
                        Console.WriteLine("SKIP (buton yok)");
                        continue;
                    }

                    Thread.Sleep(NavigateWaitMs);

                    // Screenshot al
                    TakeScreenshot(appWindow, $"{idx}_{menuName}");

                    // Hata mesajı kontrol et
                    var errorMsg = FindErrorMessage(appWindow, automation);
                    if (errorMsg is not null)
                    {
                        Errors.Add($"{menuName}: {errorMsg}");
                        Console.WriteLine($"ERROR: {errorMsg}");
                    }
                    else
                    {
                        Successes.Add(menuName);
                        Console.WriteLine("OK");
                    }
                }
                catch (Exception ex)
                {
                    Errors.Add($"{menuName}: Exception — {ex.Message}");
                    Console.WriteLine($"EXCEPTION: {ex.Message}");
                    TakeScreenshot(appWindow, $"{idx}_{menuName}_error");
                }
            }
        }
        catch (Exception ex)
        {
            Errors.Add($"FATAL: {ex.Message}");
            Console.Error.WriteLine($"\nFATAL ERROR: {ex.Message}");
        }
        finally
        {
            try { app?.Close(); } catch { /* ignore */ }
            try { app?.Dispose(); } catch { /* ignore */ }
        }

        PrintReport();
        return Errors.Count == 0 ? 0 : 1;
    }

    private static bool DoLogin(Window window, UIA3Automation automation)
    {
        var cf = automation.ConditionFactory;

        // Avalonia UIA3: AutomationId desteksiz olabilir — try-catch ile dene
        AutomationElement? usernameEl = null, passwordEl = null, loginEl = null;

        try { usernameEl = window.FindFirstDescendant(cf.ByAutomationId("UsernameBox")); } catch { /* ignore */ }
        usernameEl ??= FindByName(window, cf, "UsernameBox")
            ?? FindByName(window, cf, "Kullanıcı Adı");
        var usernameBox = usernameEl?.AsTextBox();

        try { passwordEl = window.FindFirstDescendant(cf.ByAutomationId("PasswordBox")); } catch { /* ignore */ }
        passwordEl ??= FindByName(window, cf, "PasswordBox")
            ?? FindByName(window, cf, "Şifre");
        var passwordBox = passwordEl?.AsTextBox();

        try { loginEl = window.FindFirstDescendant(cf.ByAutomationId("LoginButton")); } catch { /* ignore */ }
        loginEl ??= FindByName(window, cf, "LoginButton")
            ?? FindByName(window, cf, "Giriş Yap")
            ?? FindByName(window, cf, "GİRİŞ YAP");
        var loginBtn = loginEl?.AsButton();

        if (usernameBox is null || loginBtn is null)
        {
            Console.WriteLine($"\nDEBUG: usernameBox={usernameBox is not null}, passwordBox={passwordBox is not null}, loginBtn={loginBtn is not null}");
            // Tüm elemanları dump et
            DumpElements(window, automation);
            return false;
        }

        usernameBox.Click();
        Thread.Sleep(200);
        Keyboard.Type(Username);
        Thread.Sleep(200);

        if (passwordBox is not null)
        {
            passwordBox.Click();
            Thread.Sleep(200);
            Keyboard.Type(Password);
            Thread.Sleep(200);
        }

        loginBtn.Click();
        Thread.Sleep(WaitMs);
        return true;
    }

    private static bool ClickSidebarButton(Window window, UIA3Automation automation, string commandParameter)
    {
        // Avalonia UIA3: AutomationId desteklenmiyor — tüm butonları tara, Name ile eşleştir
        try
        {
            var cf = automation.ConditionFactory;
            var allButtons = window.FindAllDescendants(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button));

            foreach (var b in allButtons)
            {
                string name;
                try { name = b.Name ?? ""; } catch { continue; }

                if (name.Equals(commandParameter, StringComparison.OrdinalIgnoreCase)
                    || name.Replace(" ", "").Equals(commandParameter, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        b.AsButton().Click();
                        return true;
                    }
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

            // Fallback: ByName ile dene (hata yakalanır)
            try
            {
                var btn = window.FindFirstDescendant(cf.ByName(commandParameter))?.AsButton();
                if (btn is not null)
                {
                    btn.Click();
                    return true;
                }
            }
            catch { /* AutomationId not supported — ignore */ }
        }
        catch { /* Avalonia UIA limitation */ }

        return false;
    }

    private static string? FindErrorMessage(Window window, UIA3Automation automation)
    {
        try
        {
            var cf = automation.ConditionFactory;
            var allTexts = window.FindAllDescendants(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text));
            foreach (var t in allTexts)
            {
                string text;
                try { text = t.Name ?? ""; } catch { continue; }

                if (text.Contains("Hata", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("Error", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("Exception", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("Bağlantı", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("Yüklenemedi", StringComparison.OrdinalIgnoreCase))
                {
                    if (text.Contains("0 Hata") || text.Contains("0 Error") || text.Length > 200)
                        continue;
                    return text.Trim();
                }
            }
        }
        catch { /* Avalonia UIA limitation */ }
        return null;
    }

    private static AutomationElement? FindByName(Window window, ConditionFactory cf, string name)
    {
        return window.FindFirstDescendant(cf.ByName(name));
    }

    private static Window? WaitForWindow(Application app, UIA3Automation automation, string titleContains, int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                var windows = app.GetAllTopLevelWindows(automation);
                foreach (var w in windows)
                {
                    if ((w.Title ?? "").Contains(titleContains, StringComparison.OrdinalIgnoreCase))
                        return w;
                }
            }
            catch { /* window not ready */ }
            Thread.Sleep(500);
        }
        return null;
    }

    private static void TakeScreenshot(Window window, string name)
    {
        try
        {
            var capture = Capture.Screen();
            var path = Path.Combine(_screenshotDir, $"{name}.png");
            capture.ToFile(path);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  Screenshot failed: {ex.Message}");
        }
    }

    private static void DumpElements(Window window, UIA3Automation automation)
    {
        Console.WriteLine("\n  --- UI Element Dump (ilk 30) ---");
        var all = window.FindAllDescendants();
        var count = 0;
        foreach (var el in all)
        {
            if (count++ >= 30) break;
            Console.WriteLine($"  Type={el.ControlType}, Name={el.Name}, AutomationId={el.AutomationId}");
        }
        Console.WriteLine("  --- End Dump ---\n");
    }

    private static string? FindRepoRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "MesTechStok.sln"))
                || File.Exists(Path.Combine(dir, "MesTech.Avalonia.csproj"))
                || Directory.Exists(Path.Combine(dir, "src", "MesTech.Avalonia")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        // Fallback
        var candidate = @"E:\MesTech\MesTech\MesTech_Stok\MesTechStok";
        return Directory.Exists(candidate) ? candidate : null;
    }

    private static void PrintReport()
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("=== FlaUI SMOKE TEST RAPORU ===");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"Tarih     : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Başarılı  : {Successes.Count}/{MenuItems.Length}");
        Console.WriteLine($"Hata      : {Errors.Count}");
        Console.WriteLine($"Screenshots: {_screenshotDir}");
        Console.WriteLine();

        if (Successes.Count > 0)
        {
            Console.WriteLine("✓ BAŞARILI EKRANLAR:");
            foreach (var s in Successes)
                Console.WriteLine($"  ✓ {s}");
        }

        if (Errors.Count > 0)
        {
            Console.WriteLine("\n✗ HATALAR:");
            foreach (var e in Errors)
                Console.WriteLine($"  ✗ {e}");
        }

        Console.WriteLine(new string('=', 60));
    }
}
