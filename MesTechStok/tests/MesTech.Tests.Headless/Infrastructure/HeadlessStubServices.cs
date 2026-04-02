using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using MesTech.Avalonia.Services;

namespace MesTech.Tests.Headless.Infrastructure;

/// <summary>
/// Headless test stub'ları — Avalonia-specific servislerin no-op implementasyonları.
/// Gerçek pencere/dialog yok — tüm UI işlemleri sessizce başarılı döner.
/// </summary>

// ─── IDialogService ────────────────────────────────────────────────
public sealed class HeadlessDialogService : IDialogService
{
    public Task ShowInfoAsync(string message, string title) => Task.CompletedTask;
    public Task<bool> ShowConfirmAsync(string message, string title) => Task.FromResult(true);
}

// ─── IThemeService ─────────────────────────────────────────────────
public sealed class HeadlessThemeService : IThemeService
{
    public string CurrentTheme => "Light";
    public event EventHandler<string>? ThemeChanged;

    public void SetTheme(string theme) { /* no-op */ }
    public void LoadSavedTheme() { /* no-op — default Light */ }
}

// ─── IFilePickerService ────────────────────────────────────────────
public sealed class HeadlessFilePickerService : IFilePickerService
{
    public Task<string?> PickFileAsync(string title, IReadOnlyList<FilePickerFileType> fileTypes)
        => Task.FromResult<string?>(null); // dosya seçimi yok — null döner
}

// ─── INavigationService ────────────────────────────────────────────
public sealed class HeadlessNavigationService : INavigationService
{
    public Task NavigateToAsync(string viewName) => Task.CompletedTask;
    public Task NavigateToAsync(string viewName, IDictionary<string, object?>? parameters) => Task.CompletedTask;
}

// ─── IViewModelFactory ─────────────────────────────────────────────
public sealed class HeadlessViewModelFactory : IViewModelFactory
{
    private readonly IServiceProvider _sp;

    public HeadlessViewModelFactory(IServiceProvider sp) => _sp = sp;

    public ObservableObject? Create(string viewName)
    {
        // ViewModel adı → tip eşleme — DI container'dan resolve
        var vmTypeName = $"{viewName}AvaloniaViewModel";
        var vmType = typeof(MesTech.Avalonia.App).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == vmTypeName || t.Name == $"{viewName}ViewModel");

        if (vmType == null) return null;

        try { return _sp.GetService(vmType) as ObservableObject; }
        catch { return null; }
    }
}

// ─── IFeatureGateService ───────────────────────────────────────────
public sealed class HeadlessFeatureGateService : IFeatureGateService
{
    public SubscriptionTier CurrentTier => SubscriptionTier.Ultra; // tüm özellikler açık
    public event EventHandler<SubscriptionTier>? TierChanged;

    public bool IsEnabled(string feature) => true; // headless'ta her şey aktif
    public void SetTier(SubscriptionTier tier) { /* no-op */ }
}

// ─── INotificationService ──────────────────────────────────────────
public sealed class HeadlessNotificationService : INotificationService
{
    public ObservableCollection<NotificationItem> Notifications { get; } = [];

    public void ShowSuccess(string message) { /* no-op */ }
    public void ShowError(string message) { /* no-op */ }
    public void ShowWarning(string message) { /* no-op */ }
    public void ShowInfo(string message) { /* no-op */ }
}
