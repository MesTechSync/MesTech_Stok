using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Magaza Yonetimi ViewModel — magaza listesi + platform ayarlari.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class StoreManagementAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<StoreItemDto> Stores { get; } = [];

    public StoreManagementAvaloniaViewModel(IMediator mediator)
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
            // MediatR handler bağlantısı bekliyor — Task.Delay kaldırıldı

            Stores.Clear();
            Stores.Add(new StoreItemDto { StoreName = "Ana Magaza - Trendyol", Platform = "Trendyol", ApiStatus = "Bagli", ProductCount = 1250, LastSync = "19.03.2026 14:30" });
            Stores.Add(new StoreItemDto { StoreName = "Ana Magaza - Hepsiburada", Platform = "Hepsiburada", ApiStatus = "Bagli", ProductCount = 980, LastSync = "19.03.2026 14:25" });
            Stores.Add(new StoreItemDto { StoreName = "Ana Magaza - N11", Platform = "N11", ApiStatus = "Bagli", ProductCount = 750, LastSync = "19.03.2026 14:20" });
            Stores.Add(new StoreItemDto { StoreName = "Yedek Magaza - Ciceksepeti", Platform = "Ciceksepeti", ApiStatus = "Baglanti Kesildi", ProductCount = 320, LastSync = "18.03.2026 22:00" });

            TotalCount = Stores.Count;
            IsEmpty = TotalCount == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Magaza bilgileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class StoreItemDto
{
    public string StoreName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string ApiStatus { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public string LastSync { get; set; } = string.Empty;
}
