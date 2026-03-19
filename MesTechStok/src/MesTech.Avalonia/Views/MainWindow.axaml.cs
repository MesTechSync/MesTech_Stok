using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace MesTech.Avalonia.Views;

/// <summary>
/// MainWindow — ana kabuk. Toolbar + Sidebar + Content Area + StatusBar.
/// WPF ile ayni akis: saat, idle timer (3dk → WelcomeWindow), sidebar toggle.
/// </summary>
public partial class MainWindow : Window
{
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _idleTimer;
    private bool _sidebarExpanded = true;
    private DateTime _lastActivity = DateTime.Now;

    public MainWindow()
    {
        InitializeComponent();

        // Saat (toolbar, her 30 saniye)
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _clockTimer.Tick += (_, _) => UpdateToolbarClock();
        _clockTimer.Start();
        UpdateToolbarClock();

        // Idle timer — 3 dakika hareketsizlik → WelcomeWindow
        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _idleTimer.Tick += (_, _) => CheckIdle();
        _idleTimer.Start();

        // Mouse/klavye hareketlerini izle
        PointerMoved += (_, _) => _lastActivity = DateTime.Now;
        KeyDown += (_, _) => _lastActivity = DateTime.Now;
        PointerPressed += (_, _) => _lastActivity = DateTime.Now;
    }

    private void UpdateToolbarClock()
    {
        if (ToolbarClock != null)
            ToolbarClock.Text = DateTime.Now.ToString("HH:mm");
    }

    private void CheckIdle()
    {
        if ((DateTime.Now - _lastActivity).TotalMinutes >= 3)
        {
            _clockTimer.Stop();
            _idleTimer.Stop();
            var welcome = new WelcomeWindow();
            welcome.Show();
            Close();
        }
    }

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
        _clockTimer.Stop();
        _idleTimer.Stop();
        var welcome = new WelcomeWindow();
        welcome.Show();
        Close();
    }
}
