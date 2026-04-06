using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Dropshipping.Queries;
using MesTech.Domain.Common;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Dropshipping Havuz Listesi — D12-08.
/// GetDropshippingPoolsQuery + GetPoolProductsQuery ile veri çeker.
/// Havuz seçilince ürünleri DataGrid'de gösterir.
/// </summary>
public partial class DropshippingPoolAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    // Pools
    public ObservableCollection<DropshippingPoolDto> Pools { get; } = [];
    [ObservableProperty] private DropshippingPoolDto? selectedPool;
    [ObservableProperty] private int poolCount;

    // Pool Products
    public ObservableCollection<PoolProductDto> Products { get; } = [];
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalPages = 1;
    [ObservableProperty] private string paginationInfo = string.Empty;
    public int[] PageSizeOptions { get; } = [25, 50, 100];

    public DropshippingPoolAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var pools = await _mediator.Send(new GetDropshippingPoolsQuery(IsActive: null), ct);
            Pools.Clear();
            foreach (var p in pools.Items)
                Pools.Add(p);
            PoolCount = pools.TotalCount;

            if (SelectedPool is null && Pools.Count > 0)
                SelectedPool = Pools[0];
            else
                await LoadProducts();

            IsEmpty = Pools.Count == 0;
        }, "Havuz listesi yuklenirken hata");
    }

    partial void OnSelectedPoolChanged(DropshippingPoolDto? value)
    {
        CurrentPage = 1;
        _ = LoadProducts();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length == 0 || value.Length >= 2)
        {
            CurrentPage = 1;
            _ = LoadProducts();
        }
    }

    partial void OnPageSizeChanged(int value)
    {
        CurrentPage = 1;
        _ = LoadProducts();
    }

    private async Task LoadProducts()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetPoolProductsQuery(
                PoolId: SelectedPool?.Id,
                Search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                Page: CurrentPage,
                PageSize: PageSize), ct);

            Products.Clear();
            foreach (var p in result.Items)
                Products.Add(p);

            TotalCount = result.TotalCount;
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            PaginationInfo = TotalCount > 0
                ? $"Sayfa {CurrentPage}/{TotalPages} ({TotalCount} urun)"
                : string.Empty;
        }, "Havuz urunleri yuklenirken hata");
    }

    [RelayCommand]
    private void NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; _ = LoadProducts(); } }
    [RelayCommand]
    private void PrevPage() { if (CurrentPage > 1) { CurrentPage--; _ = LoadProducts(); } }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
