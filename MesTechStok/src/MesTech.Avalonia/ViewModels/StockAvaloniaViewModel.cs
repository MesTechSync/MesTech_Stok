using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stub ViewModel for Stock management screen — Dalga 10.
/// Will be wired to GetStockSummaryQuery via MediatR when full migration starts.
/// </summary>
public partial class StockAvaloniaViewModel : ViewModelBase
{
    [ObservableProperty] private string summary = "Stok yonetimi ekrani — Dalga 10 sonrasi aktif edilecek.";
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private System.Collections.ObjectModel.ObservableCollection<string> stockItems = new();
    [ObservableProperty] private string? selectedStockItem;

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(50); // Simulate async load
            Summary = "Stok yonetimi ekrani hazir. Depo secimi, stok hareketi ve envanter islemleri burada yer alacak.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void AddMovement()
    {
        // TODO: Navigate to stock movement create form
    }
}
