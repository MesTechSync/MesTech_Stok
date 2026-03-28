using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

/// <summary>
/// Bing Spotlight-inspired WelcomeWindow with integrated login.
/// Full-screen background image rotation, top thumbnail bar, bottom-left branding,
/// bottom-right login card. Crossfade transitions every 8 seconds.
///
/// ╔═══════════════════════════════════════════════════════════╗
/// ║ KORUMALI DOSYA — Spotlight Welcome Code-Behind            ║
/// ║ Sahip: DEV2 | Timer + crossfade + keyboard logic          ║
/// ║ Kural: Timer interval, crossfade süresi, keyboard map     ║
/// ║ değiştirilebilir. Akış sırası (Open→Timer→Login→Close)    ║
/// ║ DEĞİŞTİRİLEMEZ. Yeni event → mevcut yapıya ekle.        ║
/// ╚═══════════════════════════════════════════════════════════╝
/// </summary>
public partial class WelcomeWindow : Window
{
    private DispatcherTimer? _clockTimer;
    private DispatcherTimer? _imageTimer;
    private DispatcherTimer? _transitionTimer;
    private SpotlightWelcomeViewModel? _vm;

    public WelcomeWindow()
    {
        InitializeComponent();

        Opened += OnWindowOpened;
        KeyDown += OnKeyHandler;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        _vm = DataContext as SpotlightWelcomeViewModel;
        if (_vm == null) return;

        // Subscribe to ViewModel events
        _vm.LoginCompleted += OnLoginCompleted;
        _vm.DemoLoginRequested += OnDemoLogin;
        _vm.CloseRequested += () => Close();

        // Clock timer — every second
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => _vm.UpdateClock();
        _clockTimer.Start();

        // Image rotation timer — every 8 seconds
        _imageTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        _imageTimer.Tick += OnImageTimerTick;
        _imageTimer.Start();

        // Focus username box
        if (UsernameBox != null)
        {
            if (string.IsNullOrEmpty(UsernameBox.Text))
                UsernameBox.Focus();
            else
                PasswordBox?.Focus();
        }
    }

    private async void OnImageTimerTick(object? sender, EventArgs e)
    {
        if (_vm == null) return;

        bool started = await _vm.StartNextImageTransitionAsync();
        if (!started) return;

        // After 1.2s crossfade completes, swap buffers (add 100ms safety margin)
        _transitionTimer?.Stop();
        _transitionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1350) };
        _transitionTimer.Tick += (_, _) =>
        {
            _transitionTimer?.Stop();
            _vm.CompleteTransition();
        };
        _transitionTimer.Start();
    }

    private void OnKeyHandler(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            // Trigger login
            _vm?.LoginCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnLoginCompleted(bool success)
    {
        if (!success) return;

        StopTimers();

        var app = (App)global::Avalonia.Application.Current!;
        var mainWindow = app.CreateMainWindow(_vm?.PendingNavigation);
        mainWindow.Show();
        Close();
    }

    private void OnDemoLogin()
    {
        StopTimers();

        var app = (App)global::Avalonia.Application.Current!;
        var mainWindow = app.CreateMainWindow();
        mainWindow.Show();
        Close();
    }

    private void StopTimers()
    {
        _clockTimer?.Stop();
        _imageTimer?.Stop();
        _transitionTimer?.Stop();
    }

    /// <summary>Window kapanırken timer + event temizliği [EL-01]</summary>
    protected override void OnClosed(EventArgs e)
    {
        StopTimers();

        if (_vm != null)
        {
            _vm.LoginCompleted -= OnLoginCompleted;
            _vm.DemoLoginRequested -= OnDemoLogin;
            _vm.Cleanup();
        }

        Opened -= OnWindowOpened;
        KeyDown -= OnKeyHandler;

        base.OnClosed(e);
    }
}
