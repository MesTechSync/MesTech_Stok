using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace MesTech.Avalonia.Helpers;

/// <summary>
/// KÖK-5 FIX: Safe window owner resolution — prevents "Cannot show a window with a closed owner" crash.
/// WelcomeWindow closes → desktop.MainWindow = stale reference → dialog crash.
/// </summary>
public static class WindowHelper
{
    /// <summary>
    /// Returns the best available owner window for dialogs.
    /// Priority: active window → MainWindow (if visible) → any visible window → null.
    /// </summary>
    public static Window? GetActiveWindow()
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;

        // 1. Aktif pencere
        var active = desktop.Windows.FirstOrDefault(w => w.IsActive);
        if (active is not null)
            return active;

        // 2. MainWindow açıksa
        if (desktop.MainWindow is { IsVisible: true })
            return desktop.MainWindow;

        // 3. Son çare — herhangi bir açık pencere
        return desktop.Windows.FirstOrDefault(w => w.IsVisible);
    }
}
