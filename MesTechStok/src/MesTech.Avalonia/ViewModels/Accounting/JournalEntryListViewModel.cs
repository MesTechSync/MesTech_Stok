using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetJournalEntries;

namespace MesTech.Avalonia.ViewModels.Accounting;

/// <summary>
/// Yevmiye Defteri ViewModel — Chain 3 GL kayıtları.
/// Wired to GetJournalEntriesQuery via MediatR.
/// </summary>
public partial class JournalEntryListViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private DateTimeOffset? fromDate = DateTimeOffset.Now.AddMonths(-1);
    [ObservableProperty] private DateTimeOffset? toDate = DateTimeOffset.Now;
    [ObservableProperty] private string selectedSourceType = "Tümü";

    public ObservableCollection<string> SourceTypes { get; } =
        ["Tümü", "Satış", "Alış", "İade", "Komisyon", "Kargo", "Manuel"];

    public ObservableCollection<JournalEntryItem> JournalEntries { get; } = [];

    public JournalEntryListViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async () =>
        {
            JournalEntries.Clear();

            var from = FromDate?.DateTime ?? DateTime.Now.AddMonths(-1);
            var to = ToDate?.DateTime ?? DateTime.Now;
            var results = await _mediator.Send(
                new GetJournalEntriesQuery(Guid.Empty, from, to), CancellationToken);

            foreach (var dto in results)
            {
                JournalEntries.Add(new(
                    dto.ReferenceNumber ?? $"YEV-{dto.Id:N}".Substring(0, 14),
                    dto.EntryDate,
                    dto.Description,
                    dto.Lines.FirstOrDefault()?.AccountName ?? "Manuel",
                    dto.TotalDebit,
                    dto.TotalCredit));
            }

            IsEmpty = JournalEntries.Count == 0;
        }, "Yevmiye defteri");
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
