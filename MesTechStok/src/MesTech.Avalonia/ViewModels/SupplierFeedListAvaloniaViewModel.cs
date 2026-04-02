using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Dropshipping.Queries;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Tedarikci Feed Listesi ViewModel — feed DataGrid + yeni feed butonu.
/// </summary>
public partial class SupplierFeedListAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string searchText = string.Empty;

    private readonly List<SupplierFeedItemDto> _allFeeds = [];

    public ObservableCollection<SupplierFeedItemDto> Feeds { get; } = [];

    public SupplierFeedListAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allFeeds
            : _allFeeds.Where(f =>
                f.FeedName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                f.FeedType.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        Feeds.Clear();
        foreach (var f in filtered)
            Feeds.Add(f);

        TotalCount = Feeds.Count;
        IsEmpty = TotalCount == 0;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _mediator.Send(new GetFeedSourcesQuery()) ?? new();

            _allFeeds.Clear();
            foreach (var f in result.Items)
            {
                _allFeeds.Add(new SupplierFeedItemDto
                {
                    FeedName = f.Name,
                    FeedType = f.Format,
                    Status = f.IsActive ? (f.LastSyncStatus == "Failed" ? "Hatali" : "Aktif") : "Pasif",
                    LastSync = f.LastSyncAt?.ToString("dd.MM.yyyy HH:mm") ?? "—",
                    ProductCount = f.ProductCount
                });
            }

            ApplyFilter();
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
