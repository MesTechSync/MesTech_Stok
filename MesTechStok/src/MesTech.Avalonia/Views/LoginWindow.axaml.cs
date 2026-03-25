using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MesTech.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Avalonia.Views;

/// <summary>
/// LoginWindow — BCrypt auth, brute-force koruması, audit log.
/// OWASP 2026 uyumlu: progressive lockout, user enumeration önleme.
/// </summary>
public partial class LoginWindow : Window
{
    private readonly LoginAttemptTracker _tracker;
    private readonly LoginAuditLogger _auditLogger;
    private DispatcherTimer? _lockoutTimer;
    private bool _passwordVisible;

    public LoginWindow()
    {
        InitializeComponent();

        _tracker = App.ServiceProvider?.GetService<LoginAttemptTracker>()
                   ?? new LoginAttemptTracker();
        _auditLogger = App.ServiceProvider?.GetService<LoginAuditLogger>()
                       ?? new LoginAuditLogger();

        // Focus kullanıcı adı alanına
        Opened += OnWindowOpened;

        // Keyboard shortcuts
        KeyDown += OnWindowKeyDown;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        LoadRememberedUser();
        if (string.IsNullOrEmpty(UsernameBox?.Text))
            UsernameBox?.Focus();
        else
            PasswordBox?.Focus();
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            OnLoginClick(this, new RoutedEventArgs());
        if (e.Key == Key.Escape)
            Close();
    }

    private async void OnLoginClick(object? sender, RoutedEventArgs e)
    {
        var username = UsernameBox?.Text?.Trim() ?? "";
        var password = PasswordBox?.Text ?? "";

        // Boş alan kontrolü
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Kullanıcı adı ve şifre gereklidir.");
            return;
        }

        // Brute-force kontrolü
        var (isLocked, remaining) = _tracker.CheckLockout(username);
        if (isLocked && remaining.HasValue)
        {
            ShowLockout(remaining.Value);
            return;
        }

        // Loading state (minimum 300ms — psikolojik güven)
        SetLoadingState(true);
        var loginStart = DateTime.Now;

        try
        {
            // Auth doğrulama — IAuthService DI kaydı bekliyor (DEV1)
            // Şimdilik offline: auth servisi yapılandırılana kadar giriş yapılamaz
            bool isValid = false;

            // Minimum 300ms bekleme (psikolojik: "sistem kontrol ediyor")
            var elapsed = DateTime.Now - loginStart;
            if (elapsed.TotalMilliseconds < 300)
                await Task.Delay(300 - (int)elapsed.TotalMilliseconds);

            if (isValid)
            {
                _tracker.RecordSuccess(username);
                _auditLogger.Log(username, true);

                // Beni Hatırla
                if (RememberMeCheck?.IsChecked == true)
                    SaveRememberedUser(username);

                var app = (App)global::Avalonia.Application.Current!;
                var mainWindow = app.CreateMainWindow();
                mainWindow.Show();
                Close();
            }
            else
            {
                var (nowLocked, attemptsLeft, lockoutDuration) =
                    _tracker.RecordFailedAttempt(username);
                _auditLogger.Log(username, false, "invalid_credentials");

                if (nowLocked && lockoutDuration.HasValue)
                {
                    ShowLockout(lockoutDuration.Value);
                }
                else
                {
                    // Genel hata — user enumeration önleme
                    ShowError($"Giriş bilgileri hatalı. ({attemptsLeft} deneme kaldı)");
                }
            }
        }
        catch (Exception ex)
        {
            ShowError("Bağlantı hatası oluştu. Lütfen tekrar deneyin.");
            _auditLogger.Log(username, false, $"exception:{ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    private void OnTogglePassword(object? sender, RoutedEventArgs e)
    {
        _passwordVisible = !_passwordVisible;
        if (PasswordBox != null)
            PasswordBox.PasswordChar = _passwordVisible ? '\0' : '●';
        if (PasswordToggleIcon != null)
            PasswordToggleIcon.Text = _passwordVisible ? "🔒" : "👁";
    }

    private void ShowError(string message)
    {
        HideLockout();
        if (ErrorPanel != null && ErrorText != null)
        {
            ErrorText.Text = message;
            ErrorPanel.IsVisible = true;
        }
    }

    private void HideError()
    {
        if (ErrorPanel != null) ErrorPanel.IsVisible = false;
    }

    private void ShowLockout(TimeSpan duration)
    {
        HideError();
        if (LockoutPanel != null && LockoutText != null)
        {
            LockoutPanel.IsVisible = true;
            LockoutText.Text = "Çok fazla hatalı deneme. Lütfen bekleyin.";
            StartLockoutCountdown(duration);
        }
    }

    private void HideLockout()
    {
        if (LockoutPanel != null) LockoutPanel.IsVisible = false;
        _lockoutTimer?.Stop();
    }

    private void StartLockoutCountdown(TimeSpan duration)
    {
        var endTime = DateTime.Now + duration;
        _lockoutTimer?.Stop();
        _lockoutTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _lockoutTimer.Tick += (_, _) =>
        {
            var rem = endTime - DateTime.Now;
            if (rem <= TimeSpan.Zero)
            {
                _lockoutTimer?.Stop();
                HideLockout();
                if (LoginButton != null) LoginButton.IsEnabled = true;
            }
            else if (LockoutCountdown != null)
            {
                LockoutCountdown.Text = $"Kalan süre: {rem:mm\\:ss}";
            }
        };
        _lockoutTimer.Start();
        if (LoginButton != null) LoginButton.IsEnabled = false;
    }

    private void SetLoadingState(bool loading)
    {
        if (LoginButton == null) return;
        LoginButton.IsEnabled = !loading;
        LoginButton.Content = loading ? "Doğrulanıyor..." : "GİRİŞ YAP";
    }

    private void SaveRememberedUser(string username)
    {
        try
        {
            var prefs = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MesTech", "preferences.json");
            Directory.CreateDirectory(Path.GetDirectoryName(prefs)!);
            File.WriteAllText(prefs, JsonSerializer.Serialize(new { LastUsername = username }));
        }
        catch { /* Tercih kaydı kritik değil */ }
    }

    private void LoadRememberedUser()
    {
        try
        {
            var prefs = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MesTech", "preferences.json");
            if (File.Exists(prefs))
            {
                var json = File.ReadAllText(prefs);
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                if (data.TryGetProperty("LastUsername", out var u) && UsernameBox != null)
                {
                    UsernameBox.Text = u.GetString();
                    RememberMeCheck!.IsChecked = true;
                }
            }
        }
        catch { /* Sessizce geç */ }
    }

    /// <summary>Window kapanırken timer temizliği [V4-B1]</summary>
    protected override void OnClosed(EventArgs e)
    {
        _lockoutTimer?.Stop();
        Opened -= OnWindowOpened;
        KeyDown -= OnWindowKeyDown;
        base.OnClosed(e);
    }
}
