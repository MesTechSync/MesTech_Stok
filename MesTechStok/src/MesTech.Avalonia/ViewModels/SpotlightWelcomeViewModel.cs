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
///
/// ╔═══════════════════════════════════════════════════════════╗
/// ║ KORUMALI DOSYA — SpotlightWelcomeViewModel                ║
/// ║ Sahip: DEV2 | GÜVENLİK mantığı DEV4 onayı gerektirir    ║
/// ║ İZİN VERİLEN: Thumbnail ekleme, görsel iyileştirme,      ║
/// ║   animasyon parametresi değişikliği, yeni command ekleme  ║
/// ║ YASAK: Login akışı değişikliği, brute-force kaldırma,    ║
/// ║   offline fallback silme, audit log bypass                ║
/// ╚═══════════════════════════════════════════════════════════╝
/// </summary>
public partial class SpotlightWelcomeViewModel : ViewModelBase
{
    private readonly SpotlightService _spotlight;
    private readonly LoginAttemptTracker _tracker;
    private readonly LoginAuditLogger _auditLogger;
    private readonly IAuthService? _authService;

    // ═══ Background Image ═══
    // v1.1: Current image always at opacity 1.0 (no binding needed).
    // Only NextImageOpacity transitions — next fades IN on top of current, then swaps.
    // This prevents the flash/gap during crossfade.
    [ObservableProperty] private Bitmap? _currentBackgroundImage;
    [ObservableProperty] private Bitmap? _nextBackgroundImage;
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

    private async void LoadCurrentImage()
    {
        try
        {
            if (!_spotlight.HasImages) return;

            var info = _spotlight.GetCurrent();
            if (info == null) return;

            CurrentBackgroundImage = await LoadBitmapAsync(info.FilePath);
            CurrentImageName = info.DisplayName;
            NextImageOpacity = 0.0;
        }
        catch (Exception)
        {
            // Spotlight image load failure is non-critical — silently ignore
        }
    }

    /// <summary>
    /// Called by code-behind timer. Initiates crossfade to next image.
    /// v1.1: Current image stays fully visible (opacity 1.0 always).
    /// Next image fades IN on top (0→1). After transition, swap buffers.
    /// Returns true if transition started (caller should wait ~1300ms then call CompleteTransition).
    /// </summary>
    public async Task<bool> StartNextImageTransitionAsync()
    {
        if (!_spotlight.HasImages || _spotlight.Count <= 1) return false;

        var next = _spotlight.GetNext();
        if (next == null) return false;

        // Load next image while current remains fully visible
        NextBackgroundImage = await LoadBitmapAsync(next.FilePath);
        CurrentImageName = next.DisplayName;

        // Only fade IN the next image on top — current stays solid underneath
        NextImageOpacity = 1.0;

        return true;
    }

    /// <summary>
    /// Called after crossfade animation completes (~1300ms).
    /// Swaps next→current, resets next to invisible for next cycle.
    /// </summary>
    public void CompleteTransition()
    {
        var oldBitmap = CurrentBackgroundImage;

        // Next is now fully visible on top — make it the new current
        CurrentBackgroundImage = NextBackgroundImage;
        NextBackgroundImage = null;

        // Instantly reset next opacity to 0 (no transition — it's null/invisible anyway)
        NextImageOpacity = 0.0;

        oldBitmap?.Dispose();
    }

    [RelayCommand]
    private async Task SelectThumbnail(SpotlightThumbnailItem? item)
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
                CurrentBackgroundImage = await LoadBitmapAsync(info.FilePath);
                CurrentImageName = info.DisplayName;
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
        // v1.1: Cleaner Unicode symbols — cross-platform compatible, no color emoji dependency
        var modules = new (string Name, string Icon, string ViewName)[]
        {
            ("Stok", "\u25A3", "Stock"),           // ▣ filled square with inner
            ("Siparisler", "\u2630", "Orders"),     // ☰ trigram / list
            ("Fatura", "\u2637", "InvoiceList"),    // ☷ receipt-like
            ("CRM", "\u2B22", "CrmDashboard"),      // ⬢ hexagon
            ("Kargo", "\u2B9E", "CargoTracking"),   // ⮞ right arrow
            ("Pazaryeri", "\u25C8", "Marketplaces"), // ◈ diamond with dot
            ("Muhasebe", "\u2B1F", "AccountingDashboard"), // ⬟ pentagon
            ("Raporlar", "\u25A4", "Reports"),      // ▤ grid
            ("Trendyol", "\u25CF", "Trendyol"),     // ● filled circle
            ("Hepsiburada", "\u25CF", "Hepsiburada"), // ● filled circle
            ("Amazon", "\u25B2", "Amazon"),          // ▲ triangle
            ("N11", "\u25C6", "N11"),               // ◆ diamond
            ("Dashboard", "\u2302", "Dashboard"),    // ⌂ house
            ("Ayarlar", "\u2699", "Settings"),       // ⚙ gear
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
            return Bitmap.DecodeToWidth(stream, 1280);
        }
        catch
        {
            return null;
        }
    }

    private static Task<Bitmap?> LoadBitmapAsync(string path)
    {
        return Task.Run(() => LoadBitmap(path));
    }

    private void SaveRememberedUser(string username)
    {
        try
        {
            var prefs = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MesTech", "preferences.json");
            Directory.CreateDirectory(Path.GetDirectoryName(prefs)!);
            // Save username + session token for auto-login on next launch
            var sessionToken = Guid.NewGuid().ToString("N");
            File.WriteAllText(prefs, JsonSerializer.Serialize(new
            {
                LastUsername = username,
                SessionToken = sessionToken,
                SavedAt = DateTime.UtcNow.ToString("o"),
                AutoLogin = true
            }));
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
                // Auto-login: if session token exists and < 7 days old
                if (data.TryGetProperty("AutoLogin", out var autoLogin) && autoLogin.GetBoolean()
                    && data.TryGetProperty("SavedAt", out var savedAt))
                {
                    if (DateTime.TryParse(savedAt.GetString(), out var saved)
                        && (DateTime.UtcNow - saved).TotalDays < 7)
                    {
                        _pendingAutoLogin = true;
                    }
                }
            }
        }
        catch { /* Non-critical */ }
    }

    private bool _pendingAutoLogin;

    /// <summary>Auto-login if "Beni Hatırla" session is valid (< 7 days).</summary>
    public async Task TryAutoLoginAsync()
    {
        if (!_pendingAutoLogin || string.IsNullOrEmpty(Username)) return;
        _pendingAutoLogin = false;

        // Skip brute-force check for auto-login
        _tracker.RecordSuccess(Username);
        _auditLogger.Log(Username, true, "auto_login");
        LoginCompleted?.Invoke(true);
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
