using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class AccountingDashboardAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    // KPI metrics
    [ObservableProperty] private string totalRevenue = "0,00 TL";
    [ObservableProperty] private string totalExpense = "0,00 TL";
    [ObservableProperty] private string netProfit = "0,00 TL";
    [ObservableProperty] private string balance = "0,00 TL";
    [ObservableProperty] private string lastUpdated = "--:--";

    public ObservableCollection<AccountingTransactionDto> RecentTransactions { get; } = [];

    public AccountingDashboardAvaloniaViewModel(IMediator mediator)
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
            await Task.Delay(200); // Will be replaced with MediatR query

            // Sample data — will be replaced by real CQRS query
            TotalRevenue = "125.480,00 TL";
            TotalExpense = "87.320,00 TL";
            NetProfit = "38.160,00 TL";
            Balance = "52.740,00 TL";
            LastUpdated = DateTime.Now.ToString("HH:mm:ss");

            RecentTransactions.Clear();
            var items = new List<AccountingTransactionDto>
            {
                new() { Date = "19.03.2026", Description = "Trendyol satis hasilati", Category = "Satis", Type = "Gelir", AmountFormatted = "+4.520,00 TL" },
                new() { Date = "18.03.2026", Description = "Kargo gideri - Aras", Category = "Kargo", Type = "Gider", AmountFormatted = "-380,00 TL" },
                new() { Date = "18.03.2026", Description = "Hepsiburada satis hasilati", Category = "Satis", Type = "Gelir", AmountFormatted = "+2.180,00 TL" },
                new() { Date = "17.03.2026", Description = "Ofis kirasi", Category = "Genel Gider", Type = "Gider", AmountFormatted = "-6.500,00 TL" },
                new() { Date = "17.03.2026", Description = "N11 satis hasilati", Category = "Satis", Type = "Gelir", AmountFormatted = "+1.240,00 TL" },
            };
            foreach (var item in items)
                RecentTransactions.Add(item);

            IsEmpty = RecentTransactions.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Muhasebe verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class AccountingTransactionDto
{
    public string Date { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string AmountFormatted { get; set; } = string.Empty;
}
