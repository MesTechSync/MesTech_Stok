using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels.Accounting;

/// <summary>
/// Yevmiye Defteri ViewModel — Chain 3 GL kayıtları.
/// DEV 1'in JournalEntry entity'sine bağlanacak.
/// </summary>
public partial class JournalEntryListViewModel : ViewModelBase
{
    [ObservableProperty] private DateTimeOffset? fromDate = DateTimeOffset.Now.AddMonths(-1);
    [ObservableProperty] private DateTimeOffset? toDate = DateTimeOffset.Now;
    [ObservableProperty] private string selectedSourceType = "Tümü";

    public ObservableCollection<string> SourceTypes { get; } =
        ["Tümü", "Satış", "Alış", "İade", "Komisyon", "Kargo", "Manuel"];

    public ObservableCollection<JournalEntryItem> JournalEntries { get; } = [];

    public override async Task LoadAsync()
    {
        JournalEntries.Clear();
        await Task.Delay(150); // Simulate — MediatR Send kullanılacak

        JournalEntries.Add(new("YEV-2026-001", DateTime.Now.AddDays(-3), "Trendyol sipariş #SIP-0041 satış kaydı", "Satış", 2450m, 2450m));
        JournalEntries.Add(new("YEV-2026-002", DateTime.Now.AddDays(-2), "Hepsiburada komisyon kesintisi", "Komisyon", 189.50m, 189.50m));
        JournalEntries.Add(new("YEV-2026-003", DateTime.Now.AddDays(-1), "Yurtiçi Kargo gönderim ücreti", "Kargo", 45m, 45m));
        JournalEntries.Add(new("YEV-2026-004", DateTime.Now, "N11 iade ürün stok girişi", "İade", 890m, 890m));
    }

    [RelayCommand]
    private async Task FilterAsync() => await LoadAsync();
}

public class JournalEntryItem
{
    public JournalEntryItem(string entryNumber, DateTime date, string description,
        string sourceType, decimal debit, decimal credit)
    {
        EntryNumber = entryNumber;
        EntryDate = date;
        Description = description;
        SourceType = sourceType;
        TotalDebit = debit;
        TotalCredit = credit;
    }

    public string EntryNumber { get; }
    public DateTime EntryDate { get; }
    public string Description { get; }
    public string SourceType { get; }
    public decimal TotalDebit { get; }
    public decimal TotalCredit { get; }

    public string EntryDateText => EntryDate.ToString("dd.MM.yyyy");
    public string SourceTypeText => SourceType;
    public string TotalDebitText => TotalDebit.ToString("N2");
    public string TotalCreditText => TotalCredit.ToString("N2");
    public string BalanceIcon => Math.Abs(TotalDebit - TotalCredit) < 0.01m ? "✓" : "✗";
}
