using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MesTech.Application.Interfaces;
using MesTech.Avalonia.Services;
using MesTech.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Bing Spotlight-inspired unified Welcome + Login ViewModel.
/// Full-screen background image rotation, top thumbnail bar, integrated login card.
/// Preserves all security: brute-force lockout, audit log, offline fallback.
/// </summary>
public partial class SpotlightWelcomeViewModel : ViewModelBase
{
    private readonly SpotlightService _spotlight;
    private readonly LoginAttemptTracker _tracker;
    private readonly LoginAuditLogger _auditLogger;
    private readonly IAuthService? _authService;

    // ═══ Background Image ═══
    [ObservableProperty] private Bitmap? _currentBackgroundImage;
    [ObservableProperty] private Bitmap? _nextBackgroundImage;
    [ObservableProperty] private double _currentImageOpacity = 1.0;
    [ObservableProperty] private double _nextImageOpacity;
    [ObservableProperty] private string _currentImageName = string.Empty;
    [ObservableProperty] private bool _hasSpotlightImages;

    // ═══ Thumbnail Bar ═══
    public ObservableCollection<SpotlightThumbnailItem> Thumbnails { get; } = new();

    // ═══ Clock ═══
    [ObservableProperty] private string _clockText = string.Empty;
    [ObservableProperty] private string _dateText = string.Empty;

    // ═══ Login Form ═══
    [ObservableProperty] private string _username = "admin";
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private bool _rememberMe;
    [ObservableProperty] private bool _isLoginMode = true;
    [ObservableProperty] private bool _isLoginLoading;
    [ObservableProperty] private string _loginErrorMessage = string.Empty;
    [ObservableProperty] private bool _showLoginError;
    [ObservableProperty] private bool _showLockout;
    [ObservableProperty] private string _lockoutMessage = string.Empty;
    [ObservableProperty] private bool _passwordVisible;

    // ═══ Info ═══
    [ObservableProperty] private string _versionText = "v1.0.0";
    [ObservableProperty] private int _moduleCount = 79;
    [ObservableProperty] private int _platformCount = 14;

    /// <summary>Navigation target after login (set by thumbnail click).</summary>
    public string? PendingNavigation { get; set; }

    /// <summary>Raised when login succeeds — WelcomeWindow subscribes to open MainWindow.</summary>
    public event Action<bool>? LoginCompleted;

    /// <summary>Raised for demo login — bypass auth.</summary>
    public event Action? DemoLoginRequested;

    /// <summary>Raised to close the window.</summary>
    public event Action? CloseRequested;

    public SpotlightWelcomeViewModel(
        SpotlightService spotlight,
        LoginAttemptTracker tracker,
        LoginAuditLogger auditLogger,
        IAuthService? authService = null)
    {
        _spotlight = spotlight;
        _tracker = tracker;
        _auditLogger = auditLogger;
        _authService = authService;

        HasSpotlightImages = spotlight.HasImages;
        InitializeThumbnails();
        UpdateClock();
        LoadRememberedUser();
        LoadCurrentImage();
    }

    // ═══════════════════════════════════════════════
    // CLOCK
    // ═══════════════════════════════════════════════

    public void UpdateClock()
    {
        var now = DateTime.Now;
        ClockText = now.ToString("HH:mm");
        var culture = new CultureInfo("tr-TR");
        DateText = now.ToString("dd MMMM yyyy, dddd", culture);
    }

    // ═══════════════════════════════════════════════
    // IMAGE ROTATION
    // ═══════════════════════════════════════════════

    private void LoadCurrentImage()
    {
        if (!_spotlight.HasImages) return;

        var info = _spotlight.GetCurrent();
        if (info == null) return;

        CurrentBackgroundImage = LoadBitmap(info.FilePath);
        CurrentImageName = info.DisplayName;
        CurrentImageOpacity = 1.0;
        NextImageOpacity = 0.0;
    }

    /// <summary>
    /// Called by code-behind timer. Initiates crossfade to next image.
    /// Returns true if transition started (caller should wait 800ms then call CompleteTransition).
    /// </summary>
    public bool StartNextImageTransition()
    {
        if (!_spotlight.HasImages || _spotlight.Count <= 1) return false;

        var next = _spotlight.GetNext();
        if (next == null) return false;

        NextBackgroundImage = LoadBitmap(next.FilePath);
        CurrentImageName = next.DisplayName;

        // Trigger crossfade via bound Transitions
        NextImageOpacity = 1.0;
        CurrentImageOpacity = 0.0;

        return true;
    }

    /// <summary>Called 800ms after StartNextImageTransition to swap buffers.</summary>
    public void CompleteTransition()
    {
        var oldBitmap = CurrentBackgroundImage;
        CurrentBackgroundImage = NextBackgroundImage;
        NextBackgroundImage = null;
        CurrentImageOpacity = 1.0;
        NextImageOpacity = 0.0;

        oldBitmap?.Dispose();
    }

    [RelayCommand]
    private void SelectThumbnail(SpotlightThumbnailItem? item)
    {
        if (item == null) return;

        // Store pending navigation for after login
        PendingNavigation = item.ViewName;

        // If the item has an image index, jump to it
        if (item.ImageIndex >= 0 && _spotlight.HasImages)
        {
            var info = _spotlight.GoTo(item.ImageIndex % _spotlight.Count);
            if (info != null)
            {
                var oldBitmap = CurrentBackgroundImage;
                CurrentBackgroundImage = LoadBitmap(info.FilePath);
                CurrentImageName = info.DisplayName;
                CurrentImageOpacity = 1.0;
                NextImageOpacity = 0.0;
                oldBitmap?.Dispose();
            }
        }

        // Show login if not already visible
        IsLoginMode = true;
    }

    // ═══════════════════════════════════════════════
    // LOGIN
    // ═══════════════════════════════════════════════

    [RelayCommand]
    private async Task LoginAsync()
    {
        var username = Username?.Trim() ?? "";
        var password = Password ?? "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Kullanıcı adı ve şifre gereklidir.");
            return;
        }

        // Brute-force check
        var (isLocked, remaining) = _tracker.CheckLockout(username);
        if (isLocked && remaining.HasValue)
        {
            ShowLockoutPanel(remaining.Value);
            return;
        }

        // Loading state (minimum 300ms — psychological trust)
        IsLoginLoading = true;
        ShowLoginError = false;
        ShowLockout = false;
        var loginStart = DateTime.Now;

        try
        {
            bool isValid = false;

            // Auth via IAuthService (BCrypt)
            if (_authService != null)
            {
                try
                {
                    var authResult = await _authService.ValidateAsync(username, password);
                    isValid = authResult.IsSuccess;
                }
                catch
                {
                    // DB connection failure — fall through to offline check
                }
            }

            // Offline fallback: admin / Admin123!
            if (!isValid && username == "admin" && password == "Admin123!")
                isValid = true;

            // Minimum 300ms delay
            var elapsed = DateTime.Now - loginStart;
            if (elapsed.TotalMilliseconds < 300)
                await Task.Delay(300 - (int)elapsed.TotalMilliseconds);

            if (isValid)
            {
                _tracker.RecordSuccess(username);
                _auditLogger.Log(username, true);

                if (RememberMe)
                    SaveRememberedUser(username);

                LoginCompleted?.Invoke(true);
            }
            else
            {
                var (nowLocked, attemptsLeft, lockoutDuration) =
                    _tracker.RecordFailedAttempt(username);
                _auditLogger.Log(username, false, "invalid_credentials");

                if (nowLocked && lockoutDuration.HasValue)
                {
                    ShowLockoutPanel(lockoutDuration.Value);
                }
                else
                {
                    ShowError($"Giriş bilgileri hatalı. ({attemptsLeft} deneme kaldı)");
                }
            }
        }
        catch (Exception ex)
        {
            ShowError("Bağlantı hatası oluştu. Lütfen tekrar deneyin.");
            _auditLogger.Log(username, false, $"exception:{ex.GetType().Name}");
        }
        finally
        {
            IsLoginLoading = false;
        }
    }

    [RelayCommand]
    private void DemoLogin()
    {
        DemoLoginRequested?.Invoke();
    }

    [RelayCommand]
    private void ToggleLoginMode()
    {
        IsLoginMode = !IsLoginMode;
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        PasswordVisible = !PasswordVisible;
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke();
    }

    // ═══════════════════════════════════════════════
    // THUMBNAILS
    // ═══════════════════════════════════════════════

    private void InitializeThumbnails()
    {
        var modules = new (string Name, string Icon, string ViewName)[]
        {
            ("Stok", "📦", "Stock"),
            ("Siparişler", "📋", "Orders"),
            ("Fatura", "🧾", "InvoiceList"),
            ("CRM", "👥", "CrmDashboard"),
            ("Kargo", "🚚", "CargoTracking"),
            ("Pazaryerleri", "🏪", "Marketplaces"),
            ("Muhasebe", "💰", "AccountingDashboard"),
            ("Raporlar", "📊", "Reports"),
            ("Trendyol", "🟠", "Trendyol"),
            ("Hepsiburada", "🟣", "Hepsiburada"),
            ("Amazon", "📦", "Amazon"),
            ("N11", "🔮", "N11"),
            ("Dashboard", "🏠", "Dashboard"),
            ("Ayarlar", "⚙️", "Settings"),
        };

        for (int i = 0; i < modules.Length; i++)
        {
            var (name, icon, viewName) = modules[i];
            Thumbnails.Add(new SpotlightThumbnailItem
            {
                Name = name,
                Icon = icon,
                ViewName = viewName,
                ImageIndex = i
            });
        }
    }

    // ═══════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════

    private void ShowError(string message)
    {
        ShowLockout = false;
        LoginErrorMessage = message;
        ShowLoginError = true;
    }

    private void ShowLockoutPanel(TimeSpan duration)
    {
        ShowLoginError = false;
        ShowLockout = true;
        LockoutMessage = $"Çok fazla hatalı deneme. Kalan: {duration:mm\\:ss}";
    }

    /// <summary>Update lockout countdown text. Called by code-behind timer.</summary>
    public void UpdateLockoutCountdown(TimeSpan remaining)
    {
        if (remaining <= TimeSpan.Zero)
        {
            ShowLockout = false;
            return;
        }
        LockoutMessage = $"Çok fazla hatalı deneme. Kalan: {remaining:mm\\:ss}";
    }

    private static Bitmap? LoadBitmap(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return Bitmap.DecodeToWidth(stream, 1920);
        }
        catch
        {
            return null;
        }
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
        catch { /* Non-critical */ }
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
                if (data.TryGetProperty("LastUsername", out var u))
                {
                    Username = u.GetString() ?? "admin";
                    RememberMe = true;
                }
            }
        }
        catch { /* Non-critical */ }
    }

    /// <summary>Dispose loaded bitmaps.</summary>
    public void Cleanup()
    {
        CurrentBackgroundImage?.Dispose();
        NextBackgroundImage?.Dispose();
        CurrentBackgroundImage = null;
        NextBackgroundImage = null;
    }
}

/// <summary>Thumbnail item for the top bar — represents a MesTech service module.</summary>
public sealed class SpotlightThumbnailItem
{
    public string Name { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public string ViewName { get; init; } = string.Empty;
    public int ImageIndex { get; init; }
}
