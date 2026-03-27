using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace MesTech.Avalonia.Views;

/// <summary>
/// WelcomeWindow — ekran koruyucu. WPF ile ayni akis:
/// gradient arka plan, canli saat (tr-TR), yanip sonen basla mesaji.
/// Tiklama veya klavye → LoginWindow gecisi.
/// </summary>
public partial class WelcomeWindow : Window
{
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _blinkTimer;
    private bool _blinkState = true;

    public WelcomeWindow()
    {
        InitializeComponent();

        // Saat guncelleme (her saniye)
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();

        // Baslama mesaji yanip sonme (her 1.5 saniye)
        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        _blinkTimer.Tick += (_, _) =>
        {
            _blinkState = !_blinkState;
            if (StartHintText != null)
                StartHintText.Opacity = _blinkState ? 1.0 : 0.3;
        };
        _blinkTimer.Start();

        // G090: Buton click handler'ları
        if (LoginButton != null)
            LoginButton.Click += (_, _) => NavigateToLogin();
        if (DemoButton != null)
            DemoButton.Click += (_, _) => NavigateToDemo();
        if (RegisterButton != null)
            RegisterButton.Click += (_, _) => NavigateToLogin(); // Kayıt akışı → Login (şimdilik)

        // Esc ile cikis, herhangi tuş → Login
        KeyDown += OnKeyHandler;
    }

    private void OnKeyHandler(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
        else
            NavigateToLogin();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        if (ClockText != null)
            ClockText.Text = now.ToString("HH:mm");
        if (DateText != null)
        {
            var culture = new CultureInfo("tr-TR");
            DateText.Text = now.ToString("dd MMMM yyyy, dddd", culture);
        }
    }

    private void NavigateToLogin()
    {
        _clockTimer.Stop();
        _blinkTimer.Stop();

        var loginWindow = new LoginWindow();
        loginWindow.Show();
        Close();
    }

    private void NavigateToDemo()
    {
        _clockTimer.Stop();
        _blinkTimer.Stop();

        // Demo: doğrudan MainWindow'a geç (login bypass)
        var app = (App)global::Avalonia.Application.Current!;
        var mainWindow = app.CreateMainWindow();
        mainWindow.Show();
        Close();
    }

    /// <summary>Window kapanırken event + timer temizliği [EL-01]</summary>
    protected override void OnClosed(EventArgs e)
    {
        _clockTimer.Stop();
        _blinkTimer.Stop();
        KeyDown -= OnKeyHandler;
        base.OnClosed(e);
    }
}
