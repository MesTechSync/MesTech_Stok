using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MesTech.Avalonia.Dialogs;
using MesTech.Avalonia.ViewModels;
using MesTech.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Avalonia.Views;

/// <summary>
/// MainWindow — ana kabuk. Toolbar + Sidebar + Content Area + StatusBar.
/// Keyboard shortcuts, session yönetimi, idle dim/lock.
/// P0 FIX: ReturnToWelcome() — DI-resolved ViewModel ile WelcomeWindow oluşturur.
/// </summary>
public partial class MainWindow : Window
{
    private static readonly TimeSpan ToolbarClockInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan IdleCheckInterval = TimeSpan.FromSeconds(10);

    private DispatcherTimer _clockTimer;
    private DispatcherTimer _idleTimer;
    private readonly DesktopSessionManager _session;
    private bool _sidebarExpanded; // default collapsed (icon-only)
    private DateTime _lastActivity = DateTime.Now;
    private MainWindowViewModel? _subscribedVm;

    public MainWindow()
    {
        InitializeComponent();

        _session = App.ServiceProvider?.GetService<DesktopSessionManager>()
                   ?? new DesktopSessionManager();

        // Saat (toolbar, her 30 saniye)
        _clockTimer = new DispatcherTimer { Interval = ToolbarClockInterval };
        _clockTimer.Tick += (_, _) => UpdateToolbarClock();
        _clockTimer.Start();
        UpdateToolbarClock();

        // Idle timer — 10sn aralıkla kontrol
        _idleTimer = new DispatcherTimer { Interval = IdleCheckInterval };
        _idleTimer.Tick += (_, _) => CheckIdle();
        _idleTimer.Start();

        // Mouse/klavye hareketlerini izle
        PointerMoved += OnPointerActivity;
        KeyDown += OnGlobalKeyDown;
        PointerPressed += OnPointerActivity;

        // Sidebar starts collapsed (icon-only) — apply initial state after layout
        Opened += (_, _) => ApplySidebarState();

        // DEV2-01: Highlight active sidebar button when navigation changes
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe previous VM to prevent double-subscription leak
        if (_subscribedVm is not null)
            _subscribedVm.PropertyChanged -= OnVmPropertyChanged;

        if (DataContext is MainWindowViewModel vm)
        {
            vm.PropertyChanged += OnVmPropertyChanged;
            _subscribedVm = vm;
        }
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(MainWindowViewModel.SelectedMenuItem) && sender is MainWindowViewModel vm)
            HighlightSidebarButton(vm.SelectedMenuItem);
    }

    public void SetCurrentUser(string username)
    {
        _session.SetSession(username, Guid.Empty);
    }

    private void OnPointerActivity(object? sender, EventArgs e) => RecordActivity();

    private void RecordActivity()
    {
        _lastActivity = DateTime.Now;
        _session.RecordActivity();
    }

    private void UpdateToolbarClock()
    {
        if (ToolbarClock != null)
            ToolbarClock.Text = DateTime.Now.ToString("HH:mm");
    }

    private void CheckIdle()
    {
        if (_session.ShouldLock)
        {
            // 60dk idle → ekran kilidi (session korunur)
            LockScreen();
        }
        else if (_session.IsIdle)
        {
            // 30dk idle → WelcomeWindow'a dön (session temizle)
            _session.Clear();
            ReturnToWelcome();
        }
    }

    // ═══ KEYBOARD SHORTCUTS ═══

    private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
    {
        RecordActivity();

        if (e.KeyModifiers == KeyModifiers.Control)
        {
            switch (e.Key)
            {
                case Key.K: // Command Palette
                    _ = OpenCommandPaletteAsync();
                    e.Handled = true;
                    break;

                case Key.B: // Sidebar toggle
                    OnSidebarToggle(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.L: // Ekran kilitle
                    LockScreen();
                    e.Handled = true;
                    break;

                // Ctrl+1..9 → Sidebar modüllerine hızlı erişim
                case Key.D1:
                case Key.D2:
                case Key.D3:
                case Key.D4:
                case Key.D5:
                case Key.D6:
                case Key.D7:
                case Key.D8:
                case Key.D9:
                    NavigateToModuleByIndex(e.Key - Key.D1);
                    e.Handled = true;
                    break;
            }
        }

        switch (e.Key)
        {
            case Key.F5: // Yenile
                RefreshCurrentView();
                e.Handled = true;
                break;

            case Key.F11: // Tam ekran toggle
                ToggleFullScreen();
                e.Handled = true;
                break;

            case Key.Escape: // Arama temizle
                ClearSearch();
                e.Handled = true;
                break;
        }
    }

    private async Task OpenCommandPaletteAsync()
    {
        try
        {
            var dialog = new CommandPaletteDialog();
            await dialog.ShowDialog(this);

            if (dialog.SelectedCommand != null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[CommandPalette] Selected: {dialog.SelectedCommand.Title} ({dialog.SelectedCommand.Category})");
                NavigateToCommand(dialog.SelectedCommand);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CommandPalette] Acma hatasi: {ex.Message}");
        }
    }

    private void NavigateToCommand(CommandItem command)
    {
        // Navigasyon komutları için mevcut sidebar/modül sistemiyle entegre
        switch (command.Category)
        {
            case "Sistem":
                HandleSystemCommand(command.Title);
                break;
            default:
                System.Diagnostics.Debug.WriteLine(
                    $"[CommandPalette] Navigate: {command.Title}");
                break;
        }
    }

    private void HandleSystemCommand(string title)
    {
        switch (title)
        {
            case "Tam Ekran": ToggleFullScreen(); break;
            case "Yenile": RefreshCurrentView(); break;
            case "Kilitle": LockScreen(); break;
            case "Sidebar Ac/Kapa": OnSidebarToggle(this, new RoutedEventArgs()); break;
        }
    }

    private void FocusSearchBox()
    {
        var searchBox = this.FindControl<TextBox>("SearchBox");
        searchBox?.Focus();
    }

    private void NavigateToModuleByIndex(int index)
    {
        System.Diagnostics.Debug.WriteLine($"[Shortcut] Navigate to module index: {index}");
    }

    private void RefreshCurrentView()
    {
        System.Diagnostics.Debug.WriteLine("[Shortcut] Refresh current view");
    }

    private void ToggleFullScreen()
    {
        WindowState = WindowState == WindowState.FullScreen
            ? WindowState.Normal
            : WindowState.FullScreen;
    }

    private void ClearSearch()
    {
        var searchBox = this.FindControl<TextBox>("SearchBox");
        if (searchBox != null)
            searchBox.Text = "";
    }

    private void LockScreen()
    {
        // Session'ı KORU — sadece ekranı kilitle
        ReturnToWelcome();
    }

    /// <summary>WelcomeWindow'a DI-resolved ViewModel ile geri dön.
    /// P0 FIX: DataContext olmadan oluşturulursa UI tamamen bozulur.</summary>
    private void ReturnToWelcome()
    {
        _clockTimer.Stop();
        _idleTimer.Stop();
        var welcomeVm = App.ServiceProvider!.GetRequiredService<SpotlightWelcomeViewModel>();
        var welcome = new WelcomeWindow { DataContext = welcomeVm };
        welcome.Show();
        Close();
    }

    // ═══ MEVCUT İŞLEVLER ═══

    private void OnSidebarToggle(object? sender, RoutedEventArgs e)
    {
        _sidebarExpanded = !_sidebarExpanded;
        ApplySidebarState();
    }

    private void ApplySidebarState()
    {
        if (SidebarPanel != null)
            SidebarPanel.Width = _sidebarExpanded ? 240 : 60;
        if (SidebarTitle != null)
            SidebarTitle.IsVisible = _sidebarExpanded;
        if (SidebarFooter != null)
            SidebarFooter.IsVisible = _sidebarExpanded;

        // Toggle section headers + button text labels visibility
        var sidebar = SidebarPanel?.FindControl<ScrollViewer>("SidebarScroll");
        if (sidebar?.Content is StackPanel stack)
        {
            foreach (var child in stack.Children)
            {
                // Hide section headers in collapsed mode
                if (child is TextBlock tb && tb.Classes.Contains("sidebar-section"))
                    tb.IsVisible = _sidebarExpanded;

                // Hide text labels inside buttons, keep icons visible
                if (child is Button btn && btn.Content is StackPanel sp && sp.Children.Count >= 2)
                {
                    if (sp.Children[1] is TextBlock label)
                        label.IsVisible = _sidebarExpanded;
                }
            }
        }
    }

    /// <summary>DEV2-01: Highlight active sidebar button by switching CSS class.</summary>
    private void HighlightSidebarButton(string viewName)
    {
        var sidebar = SidebarPanel?.FindControl<ScrollViewer>("SidebarScroll");
        if (sidebar?.Content is not StackPanel stack) return;

        foreach (var child in stack.Children)
        {
            if (child is not Button btn) continue;
            var param = btn.CommandParameter as string;
            if (param == viewName)
            {
                btn.Classes.Remove("sidebar-btn");
                if (!btn.Classes.Contains("sidebar-btn-active"))
                    btn.Classes.Add("sidebar-btn-active");
            }
            else
            {
                btn.Classes.Remove("sidebar-btn-active");
                if (!btn.Classes.Contains("sidebar-btn"))
                    btn.Classes.Add("sidebar-btn");
            }
        }
    }

    private void OnLogout(object? sender, RoutedEventArgs e)
    {
        _session.Clear();
        ReturnToWelcome();
    }

    /// <summary>Window kapanırken timer + event temizliği [V4-B1] [EL-02]</summary>
    protected override void OnClosed(EventArgs e)
    {
        _clockTimer.Stop();
        _clockTimer = null!;
        _idleTimer.Stop();
        _idleTimer = null!;
        PointerMoved -= OnPointerActivity;
        PointerPressed -= OnPointerActivity;
        KeyDown -= OnGlobalKeyDown;
        (DataContext as IDisposable)?.Dispose();
        base.OnClosed(e);
    }
}
