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

        // Tiklama veya klavye → Login'e gec
        PointerPressed += OnInteraction;
        KeyDown += OnInteraction;
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

    private void OnInteraction(object? sender, EventArgs e)
    {
        _clockTimer.Stop();
        _blinkTimer.Stop();

        var loginWindow = new LoginWindow();
        loginWindow.Show();
        Close();
    }
}
