using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MesTech.Avalonia.Dialogs;
using MesTech.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Avalonia.Views;

/// <summary>
/// MainWindow — ana kabuk. Toolbar + Sidebar + Content Area + StatusBar.
/// Keyboard shortcuts, session yönetimi, idle dim/lock.
/// </summary>
public partial class MainWindow : Window
{
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _idleTimer;
    private readonly DesktopSessionManager _session;
    private bool _sidebarExpanded = true;
    private DateTime _lastActivity = DateTime.Now;

    public MainWindow()
    {
        InitializeComponent();

        _session = App.ServiceProvider?.GetService<DesktopSessionManager>()
                   ?? new DesktopSessionManager();

        // Saat (toolbar, her 30 saniye)
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _clockTimer.Tick += (_, _) => UpdateToolbarClock();
        _clockTimer.Start();
        UpdateToolbarClock();

        // Idle timer — 10sn aralıkla kontrol
        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _idleTimer.Tick += (_, _) => CheckIdle();
        _idleTimer.Start();

        // Mouse/klavye hareketlerini izle
        PointerMoved += OnPointerActivity;
        KeyDown += OnGlobalKeyDown;
        PointerPressed += OnPointerActivity;
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
            // 15dk idle → ekran kilidi (session korunur)
            LockScreen();
        }
        else if ((DateTime.Now - _lastActivity).TotalMinutes >= 3)
        {
            _clockTimer.Stop();
            _idleTimer.Stop();
            var welcome = new WelcomeWindow();
            welcome.Show();
            Close();
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
                    OpenCommandPalette();
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

    private async void OpenCommandPalette()
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
        // Sidebar menü öğelerine index ile erişim
        // Mevcut navigasyon sistemiyle entegre
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
        _clockTimer.Stop();
        _idleTimer.Stop();
        var welcome = new WelcomeWindow();
        welcome.Show();
        Close();
    }

    // ═══ MEVCUT İŞLEVLER ═══

    private void OnSidebarToggle(object? sender, RoutedEventArgs e)
    {
        _sidebarExpanded = !_sidebarExpanded;
        if (SidebarPanel != null)
            SidebarPanel.Width = _sidebarExpanded ? 240 : 60;
        if (SidebarTitle != null)
            SidebarTitle.IsVisible = _sidebarExpanded;
    }

    private void OnLogout(object? sender, RoutedEventArgs e)
    {
        _session.Clear();
        _clockTimer.Stop();
        _idleTimer.Stop();
        var welcome = new WelcomeWindow();
        welcome.Show();
        Close();
    }

    /// <summary>Window kapanırken timer + event temizliği [V4-B1]</summary>
    protected override void OnClosed(EventArgs e)
    {
        _clockTimer.Stop();
        _idleTimer.Stop();
        PointerMoved -= OnPointerActivity;
        PointerPressed -= OnPointerActivity;
        KeyDown -= OnGlobalKeyDown;
        base.OnClosed(e);
    }
}
