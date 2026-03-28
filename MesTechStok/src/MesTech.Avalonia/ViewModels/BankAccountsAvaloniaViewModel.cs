using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stub ViewModel for Bank Accounts screen — Dalga 11.
/// Will be wired to GetBankAccountsQuery via MediatR when full migration starts.
/// </summary>
public partial class BankAccountsAvaloniaViewModel : ViewModelBase
{
    [ObservableProperty] private string summary = "Banka hesaplari ekrani — Dalga 11 sonrasi aktif edilecek.";
    [ObservableProperty] private int totalCount;

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(50); // Simulate async load
            TotalCount = 0;
            Summary = "Banka hesaplari ekrani hazir. Hesap bakiyeleri, hareket gecmisi, havale/EFT takibi ve mutabakat burada yer alacak.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void Add()
    {
        // TODO: Navigate to bank account create form or show dialog
    }
}
