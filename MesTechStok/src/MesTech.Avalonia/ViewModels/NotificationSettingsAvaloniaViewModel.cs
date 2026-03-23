using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Notifications.Commands.SendNotification;
using MesTech.Application.Features.Notifications.Commands.UpdateNotificationSettings;
using MesTech.Application.Features.Notifications.Queries.GetNotificationSettings;
using MesTech.Domain.Enums;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Notification Settings ViewModel — channel selection, category matrix, quiet hours, digest mode.
/// MediatR ile gerçek bildirim ayarları yönetimi.
/// </summary>
public partial class NotificationSettingsAvaloniaViewModel : ObservableObject
{
    private readonly ISender _mediator;
    private Guid _tenantId;
    private Guid _userId;

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

    public NotificationSettingsAvaloniaViewModel(ISender mediator)
    {
        _mediator = mediator;
    }

    public void SetContext(Guid tenantId, Guid userId)
    {
        _tenantId = tenantId;
        _userId = userId;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            var settings = await _mediator.Send(new GetNotificationSettingsQuery(_tenantId, _userId));

            Categories.Clear();

            if (settings.Count > 0)
            {
                foreach (var setting in settings)
                {
                    var isEmail = setting.Channel == NotificationChannel.Email;
                    var isTelegram = setting.Channel == NotificationChannel.Telegram;
                    var isWhatsApp = setting.Channel == NotificationChannel.WhatsApp;

                    if (isEmail) IsEmailEnabled = setting.IsEnabled;
                    if (isTelegram) IsTelegramEnabled = setting.IsEnabled;
                    if (isWhatsApp) IsWhatsAppEnabled = setting.IsEnabled;

                    if (!string.IsNullOrWhiteSpace(setting.ChannelAddress) && isEmail)
                        EmailAddress = setting.ChannelAddress;
                }
            }

            // Default categories
            Categories.Add(new NotificationCategoryItem("Siparis Olusturuldu", true, IsEmailEnabled, IsTelegramEnabled, IsWhatsAppEnabled));
            Categories.Add(new NotificationCategoryItem("Siparis Iptal", true, IsEmailEnabled, IsTelegramEnabled, false));
            Categories.Add(new NotificationCategoryItem("Dusuk Stok Uyarisi", true, IsEmailEnabled, IsTelegramEnabled, IsWhatsAppEnabled));
            Categories.Add(new NotificationCategoryItem("Stok Tukendi", true, IsEmailEnabled, IsTelegramEnabled, IsWhatsAppEnabled));
            Categories.Add(new NotificationCategoryItem("Fiyat Degisikligi", true, false, false, false));
            Categories.Add(new NotificationCategoryItem("Kargo Guncelleme", true, IsEmailEnabled, false, false));
            Categories.Add(new NotificationCategoryItem("Iade Talebi", true, IsEmailEnabled, IsTelegramEnabled, false));
            Categories.Add(new NotificationCategoryItem("Platform Senkron Hatasi", true, IsEmailEnabled, IsTelegramEnabled, IsWhatsAppEnabled));
            Categories.Add(new NotificationCategoryItem("Fatura Olusturuldu", true, false, false, false));
            Categories.Add(new NotificationCategoryItem("Sistem Bakimi", true, IsEmailEnabled, IsTelegramEnabled, IsWhatsAppEnabled));
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
            if (IsEmailEnabled)
            {
                await _mediator.Send(new UpdateNotificationSettingsCommand(
                    _tenantId, _userId, NotificationChannel.Email, EmailAddress,
                    true, true, true, 10, true, true, true, false, false, true, true, true,
                    IsQuietHoursEnabled ? QuietStart.ToString(@"hh\:mm") : null,
                    IsQuietHoursEnabled ? QuietEnd.ToString(@"hh\:mm") : null,
                    "tr", !IsInstantMode,
                    !IsInstantMode ? DigestTime.ToString(@"hh\:mm") : null));
            }

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
            await _mediator.Send(new SendNotificationCommand(
                _tenantId, "Push", "test", "test_notification",
                "Bu bir test bildirimidir — MesTech bildirim sistemi calisiyor."));
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
