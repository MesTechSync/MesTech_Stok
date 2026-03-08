using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using FlaUIApp = FlaUI.Core.Application;

namespace MesTech.Tests.Integration.UI._Shared;

/// <summary>
/// Launches the MesTechStok Desktop app and provides the FlaUI automation handle.
/// Shared across all UI test classes via xUnit Collection Fixture.
/// </summary>
public class DesktopAppFixture : IDisposable
{
    private FlaUIApp? _app;
    private UIA3Automation? _automation;

    public UIA3Automation Automation => _automation ?? throw new InvalidOperationException("Fixture not initialized");
    public FlaUIApp App => _app ?? throw new InvalidOperationException("Fixture not initialized");
    public Window MainWindow { get; private set; } = null!;

    /// <summary>True if running in CI (no display) — UI tests should skip.</summary>
    public static bool IsCI => Environment.GetEnvironmentVariable("CI") != null
                            || Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null;

    public DesktopAppFixture()
    {
        if (IsCI) return;

        _automation = new UIA3Automation();

        var exePath = FindDesktopExe();
        if (exePath == null)
            throw new FileNotFoundException(
                "MesTechStok.Desktop.exe not found. Build the Desktop project first: dotnet build MesTechStok.Desktop");

        _app = FlaUIApp.Launch(new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = Path.GetDirectoryName(exePath)!,
            UseShellExecute = false,
        });

        MainWindow = WaitForMainWindow(timeout: TimeSpan.FromSeconds(30));
    }

    private Window WaitForMainWindow(TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            try
            {
                var windows = _app!.GetAllTopLevelWindows(_automation!);
                var main = windows.FirstOrDefault(w =>
                    w.Title?.Contains("MesTech", StringComparison.OrdinalIgnoreCase) == true);
                if (main != null)
                    return main;

                Thread.Sleep(500);
            }
            catch
            {
                Thread.Sleep(500);
            }
        }

        var anyWindow = _app!.GetAllTopLevelWindows(_automation!).FirstOrDefault();
        return anyWindow ?? throw new TimeoutException("No window appeared within 30s");
    }

    /// <summary>Refresh the MainWindow reference after navigation.</summary>
    public void RefreshMainWindow()
    {
        if (_app == null || _automation == null) return;
        var windows = _app.GetAllTopLevelWindows(_automation);
        var main = windows.FirstOrDefault(w =>
            w.Title?.Contains("MesTech", StringComparison.OrdinalIgnoreCase) == true);
        if (main != null) MainWindow = main;
    }

    /// <summary>Check if a modal error dialog is showing.</summary>
    public Window? FindErrorDialog()
    {
        if (_app == null || _automation == null) return null;
        var windows = _app.GetAllTopLevelWindows(_automation);
        return windows.FirstOrDefault(w =>
            w.Title?.Contains("Hata", StringComparison.OrdinalIgnoreCase) == true
            || w.Title?.Contains("Error", StringComparison.OrdinalIgnoreCase) == true
            || w.Title?.Contains("Exception", StringComparison.OrdinalIgnoreCase) == true
            || w.Title?.Contains("Kritik", StringComparison.OrdinalIgnoreCase) == true);
    }

    /// <summary>Dismiss any open modal dialog.</summary>
    public void DismissAnyDialog()
    {
        var dialog = FindErrorDialog();
        if (dialog == null) return;

        try
        {
            var okButton = dialog.FindFirstDescendant(cf =>
                cf.ByName("OK"))?.AsButton();
            okButton?.Click();
        }
        catch
        {
            try { dialog.Close(); } catch { }
        }
    }

    internal static string? FindDesktopExe()
    {
        var testDir = AppDomain.CurrentDomain.BaseDirectory;
        var root = testDir;
        for (int i = 0; i < 10; i++)
        {
            var parent = Directory.GetParent(root);
            if (parent == null) break;
            root = parent.FullName;
        }

        var candidates = new[]
        {
            Path.Combine(root, "MesTechStok.Desktop", "bin", "Debug", "net9.0-windows", "win-x64", "MesTechStok.Desktop.exe"),
            Path.Combine(root, "MesTechStok.Desktop", "bin", "Release", "net9.0-windows", "win-x64", "MesTechStok.Desktop.exe"),
            Path.Combine(root, "src", "MesTechStok.Desktop", "bin", "Debug", "net9.0-windows", "win-x64", "MesTechStok.Desktop.exe"),
            Path.Combine(root, "src", "MesTechStok.Desktop", "bin", "Release", "net9.0-windows", "win-x64", "MesTechStok.Desktop.exe"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    public void Dispose()
    {
        try { _app?.Close(); } catch { }
        try { _app?.Dispose(); } catch { }
        try { _automation?.Dispose(); } catch { }
    }
}

[CollectionDefinition("DesktopApp")]
public class DesktopAppCollection : ICollectionFixture<DesktopAppFixture> { }
