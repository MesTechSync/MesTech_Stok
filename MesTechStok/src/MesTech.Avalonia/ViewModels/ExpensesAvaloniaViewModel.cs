using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stub ViewModel for Expenses screen — Dalga 11.
/// Will be wired to GetExpensesPagedQuery via MediatR when full migration starts.
/// </summary>
public partial class ExpensesAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string summary = "Gider yonetimi ekrani — Dalga 11 sonrasi aktif edilecek.";
    [ObservableProperty] private int totalCount;

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(50); // Simulate async load
            TotalCount = 0;
            Summary = "Gider yonetimi ekrani hazir. Gider kaydi, kategori bazli takip, fatura eslestirme ve onay sureci burada yer alacak.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
