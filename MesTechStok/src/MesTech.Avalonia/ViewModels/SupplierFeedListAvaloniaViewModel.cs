using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Tedarikci Feed Listesi ViewModel — feed DataGrid + yeni feed butonu.
/// </summary>
public partial class SupplierFeedListAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<SupplierFeedItemDto> Feeds { get; } = [];

    public SupplierFeedListAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(250);

            Feeds.Clear();
            Feeds.Add(new SupplierFeedItemDto { FeedName = "ABC Elektronik XML", FeedType = "XML", Status = "Aktif", LastSync = "19.03.2026 14:00", ProductCount = 1280 });
            Feeds.Add(new SupplierFeedItemDto { FeedName = "XYZ Bilisim CSV", FeedType = "CSV", Status = "Aktif", LastSync = "19.03.2026 13:30", ProductCount = 845 });
            Feeds.Add(new SupplierFeedItemDto { FeedName = "Guney Aksesuar API", FeedType = "API", Status = "Hatali", LastSync = "18.03.2026 22:00", ProductCount = 320 });
            Feeds.Add(new SupplierFeedItemDto { FeedName = "Delta Depo Excel", FeedType = "Excel", Status = "Aktif", LastSync = "19.03.2026 12:00", ProductCount = 560 });
            Feeds.Add(new SupplierFeedItemDto { FeedName = "Mega Toptan XML", FeedType = "XML", Status = "Pasif", LastSync = "15.03.2026 10:00", ProductCount = 2100 });

            TotalCount = Feeds.Count;
            IsEmpty = TotalCount == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Feed listesi yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class SupplierFeedItemDto
{
    public string FeedName { get; set; } = string.Empty;
    public string FeedType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string LastSync { get; set; } = string.Empty;
    public int ProductCount { get; set; }
}
