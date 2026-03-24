using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

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
            await Task.Delay(200); // Will be replaced with MediatR query

            Notifications.Clear();
            Notifications.Add(new NotificationItemDto { Title = "Stok Uyarisi", Message = "Samsung Galaxy S24 Ultra stok seviyesi kritik (3 adet kaldi)", TimeAgo = "5 dk once", StatusColor = "#EF4444" });
            Notifications.Add(new NotificationItemDto { Title = "Yeni Siparis", Message = "Trendyol uzerinden 3 yeni siparis alindi", TimeAgo = "12 dk once", StatusColor = "#059669" });
            Notifications.Add(new NotificationItemDto { Title = "Kargo Teslim", Message = "Siparis #1042 basariyla teslim edildi", TimeAgo = "1 saat once", StatusColor = "#2563EB" });
            Notifications.Add(new NotificationItemDto { Title = "Fiyat Guncelleme", Message = "Hepsiburada fiyat senkronizasyonu tamamlandi (47 urun)", TimeAgo = "2 saat once", StatusColor = "#059669" });
            Notifications.Add(new NotificationItemDto { Title = "Sistem Bakimi", Message = "Planlanan bakim yarın 02:00-04:00 arasi yapilacak", TimeAgo = "3 saat once", StatusColor = "#F59E0B" });

            TotalCount = Notifications.Count;
            IsEmpty = TotalCount == 0;
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
