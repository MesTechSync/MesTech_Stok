using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.System.UserNotifications.Queries.GetUserNotifications;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Bildirimler ViewModel — bildirim zaman cizelgesi.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class NotificationAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<NotificationItemDto> Notifications { get; } = [];

    public NotificationAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _mediator.Send(new GetUserNotificationsQuery(
                TenantId: Guid.Empty,
                UserId: Guid.Empty));

            Notifications.Clear();
            foreach (var n in result.Items)
            {
                Notifications.Add(new NotificationItemDto
                {
                    Title = n.Title,
                    Message = n.Message,
                    TimeAgo = FormatTimeAgo(n.CreatedAt),
                    StatusColor = n.IsRead ? "#94A3B8" : "#2563EB"
                });
            }

            TotalCount = result.TotalCount;
            IsEmpty = Notifications.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Bildirimler yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string FormatTimeAgo(DateTime createdAt)
    {
        var diff = DateTime.UtcNow - createdAt.ToUniversalTime();
        if (diff.TotalMinutes < 1) return "Az once";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} dk once";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} saat once";
        return $"{(int)diff.TotalDays} gun once";
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void MarkAllRead()
    {
        // Will be replaced with MediatR command
        foreach (var n in Notifications)
            n.StatusColor = "#94A3B8";
    }
}

public class NotificationItemDto
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#94A3B8";
}
