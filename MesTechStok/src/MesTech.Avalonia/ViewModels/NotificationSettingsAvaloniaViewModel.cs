using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Notification Settings ViewModel — channel selection, category matrix, quiet hours, digest mode.
/// İ-11 Görev 4A: Dedicated notification settings screen with mock data.
/// </summary>
public partial class NotificationSettingsAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isSaved;
    [ObservableProperty] private string testResult = string.Empty;
    [ObservableProperty] private bool isTestSent;

    // Channel toggles
    [ObservableProperty] private bool isInAppEnabled = true;
    [ObservableProperty] private bool isEmailEnabled;
    [ObservableProperty] private bool isTelegramEnabled;
    [ObservableProperty] private bool isWhatsAppEnabled;

    // Email settings
    [ObservableProperty] private string emailAddress = string.Empty;

    // Quiet hours
    [ObservableProperty] private bool isQuietHoursEnabled;
    [ObservableProperty] private TimeSpan quietStart = new(22, 0, 0);
    [ObservableProperty] private TimeSpan quietEnd = new(8, 0, 0);

    // Digest mode
    [ObservableProperty] private bool isInstantMode = true;
    [ObservableProperty] private bool isDailyDigestMode;
    [ObservableProperty] private TimeSpan digestTime = new(9, 0, 0);

    // Category notification matrix
    public ObservableCollection<NotificationCategoryItem> Categories { get; } = new();

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(200); // Simulate loading

            Categories.Clear();
            Categories.Add(new NotificationCategoryItem("Siparis Olusturuldu", true, true, false, false));
            Categories.Add(new NotificationCategoryItem("Siparis Iptal", true, true, true, false));
            Categories.Add(new NotificationCategoryItem("Dusuk Stok Uyarisi", true, true, true, true));
            Categories.Add(new NotificationCategoryItem("Stok Tukendi", true, true, true, true));
            Categories.Add(new NotificationCategoryItem("Fiyat Degisikligi", true, false, false, false));
            Categories.Add(new NotificationCategoryItem("Kargo Guncelleme", true, true, false, false));
            Categories.Add(new NotificationCategoryItem("Iade Talebi", true, true, true, false));
            Categories.Add(new NotificationCategoryItem("Platform Senkron Hatasi", true, true, true, true));
            Categories.Add(new NotificationCategoryItem("Fatura Olusturuldu", true, false, false, false));
            Categories.Add(new NotificationCategoryItem("Sistem Bakimi", true, true, true, true));
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Bildirim ayarlari yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsLoading = true;
        IsSaved = false;
        HasError = false;
        try
        {
            await Task.Delay(400); // Simulate save
            IsSaved = true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Ayarlar kaydedilemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SendTestNotificationAsync()
    {
        IsTestSent = false;
        TestResult = string.Empty;
        IsLoading = true;
        try
        {
            await Task.Delay(800); // Simulate sending
            TestResult = "Test bildirimi basariyla gonderildi!";
            IsTestSent = true;
        }
        catch (Exception ex)
        {
            TestResult = $"Test gonderilemedi: {ex.Message}";
            IsTestSent = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

/// <summary>
/// Represents a notification category with per-channel toggle state.
/// </summary>
public partial class NotificationCategoryItem : ObservableObject
{
    [ObservableProperty] private string categoryName;
    [ObservableProperty] private bool inApp;
    [ObservableProperty] private bool email;
    [ObservableProperty] private bool telegram;
    [ObservableProperty] private bool whatsApp;

    public NotificationCategoryItem(string name, bool inApp, bool email, bool telegram, bool whatsApp)
    {
        categoryName = name;
        this.inApp = inApp;
        this.email = email;
        this.telegram = telegram;
        this.whatsApp = whatsApp;
    }
}
