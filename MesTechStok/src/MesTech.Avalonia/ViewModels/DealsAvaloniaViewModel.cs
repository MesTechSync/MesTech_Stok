using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class DealsAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? selectedStage;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string totalAmount = "0 TL";

    public ObservableCollection<DealListItemVm> Deals { get; } = [];
    public string[] StageOptions { get; } = ["Tumu", "Ilk Iletisim", "Teklif Verildi", "Muzakere", "Kazanildi", "Kaybedildi"];

    public DealsAvaloniaViewModel(IMediator mediator)
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
            await Task.Delay(50);
            Deals.Clear();
            Deals.Add(new DealListItemVm { Id = Guid.NewGuid(), Title = "ABC Ltd ERP Projesi", ContactName = "Ahmet Yilmaz", Amount = 45000, Stage = "Ilk Iletisim", Probability = 30, CreatedAt = DateTime.Now.AddDays(-10) });
            Deals.Add(new DealListItemVm { Id = Guid.NewGuid(), Title = "XYZ Stok Entegrasyonu", ContactName = "Fatma Demir", Amount = 22000, Stage = "Teklif Verildi", Probability = 50, CreatedAt = DateTime.Now.AddDays(-7) });
            Deals.Add(new DealListItemVm { Id = Guid.NewGuid(), Title = "DEF Marketplace Setup", ContactName = "Mehmet Can", Amount = 67000, Stage = "Muzakere", Probability = 70, CreatedAt = DateTime.Now.AddDays(-14) });
            Deals.Add(new DealListItemVm { Id = Guid.NewGuid(), Title = "GHI Dropshipping", ContactName = "Ayse Kara", Amount = 35000, Stage = "Kazanildi", Probability = 100, CreatedAt = DateTime.Now.AddDays(-21) });
            TotalCount = Deals.Count;
            TotalAmount = $"{Deals.Sum(d => d.Amount):N0} TL";
            IsEmpty = Deals.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Firsatlar yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    partial void OnSelectedStageChanged(string? value)
        => _ = LoadAsync();

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length == 0 || value.Length >= 2)
            _ = LoadAsync();
    }
}

public class DealListItemVm
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public decimal Amount { get; set; }
    public string Stage { get; set; } = string.Empty;
    public int Probability { get; set; }
    public DateTime CreatedAt { get; set; }

    public string AmountDisplay => $"{Amount:N0} TL";
    public string ProbabilityDisplay => $"%{Probability}";
}
