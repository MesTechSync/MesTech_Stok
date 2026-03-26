using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Queries.GetCariHesaplar;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Cari Hesaplar (Accounts Receivable/Payable) screen.
/// Wired to GetCariHesaplarQuery via MediatR.
/// </summary>
public partial class CariHesaplarAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    [ObservableProperty] private int totalCount;

    [ObservableProperty] private string selectedType = "Tumu";
    [ObservableProperty] private decimal totalDebit;
    [ObservableProperty] private decimal totalCredit;
    [ObservableProperty] private decimal netBalance;

    public ObservableCollection<CariHesapItemDto> Items { get; } = [];
    public ObservableCollection<CariHesapItemDto> FilteredItems { get; } = [];
    public ObservableCollection<string> TypeOptions { get; } = ["Tumu", "Musteri", "Tedarikci"];

    private readonly List<CariHesapItemDto> _allItems = [];

    public CariHesaplarAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _mediator.Send(
                new GetCariHesaplarQuery(TenantId: _tenantProvider.GetCurrentTenantId()));

            _allItems.Clear();
            _allItems.AddRange(result.Select(c => new CariHesapItemDto
            {
                HesapAdi = c.Name,
                Tip = c.Type.ToString() == "Customer" ? "Musteri" : "Tedarikci",
                Borc = 0m,
                Alacak = 0m,
                Bakiye = 0m
            }));

            ApplyFilter();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Cari hesaplar yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedTypeChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredItems.Clear();

        var filtered = SelectedType == "Tumu"
            ? _allItems
            : _allItems.Where(x => x.Tip == SelectedType).ToList();

        foreach (var item in filtered)
            FilteredItems.Add(item);

        TotalCount = FilteredItems.Count;
        IsEmpty = FilteredItems.Count == 0;
        TotalDebit = FilteredItems.Sum(x => x.Borc);
        TotalCredit = FilteredItems.Sum(x => x.Alacak);
        NetBalance = TotalDebit - TotalCredit;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class CariHesapItemDto
{
    public string HesapAdi { get; set; } = string.Empty;
    public string Tip { get; set; } = string.Empty;
    public decimal Borc { get; set; }
    public decimal Alacak { get; set; }
    public decimal Bakiye { get; set; }
}
