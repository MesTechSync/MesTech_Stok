using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stub ViewModel for Reports screen — Dalga 11.
/// Will be wired to report generation queries via MediatR when full migration starts.
/// </summary>
public partial class ReportsAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string summary = "Raporlar ekrani — Dalga 11 sonrasi aktif edilecek.";

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(50); // Simulate async load
            Summary = "Raporlar ekrani hazir. Satis analizi, stok raporu, platform karsilastirmasi ve ozel rapor sablonlari burada yer alacak.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
