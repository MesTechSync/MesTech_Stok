using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class VergiTakvimiAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private int totalCount;

    // KPI
    [ObservableProperty] private int overdueCount;
    [ObservableProperty] private int upcomingCount;
    [ObservableProperty] private int completedCount;

    // Filters
    [ObservableProperty] private string selectedMonth = "Mart";
    [ObservableProperty] private string selectedYear = "2026";
    [ObservableProperty] private string selectedStatus = "Tumu";
    [ObservableProperty] private string selectedStatusFilter = "Tümü";

    public ObservableCollection<TaxCalendarItemDto> Items { get; } = [];
    private List<TaxCalendarItemDto> _allItems = [];

    public ObservableCollection<string> Months { get; } =
        ["Ocak", "Subat", "Mart", "Nisan", "Mayis", "Haziran", "Temmuz", "Agustos", "Eylul", "Ekim", "Kasim", "Aralik"];

    public ObservableCollection<string> Years { get; } =
        ["2024", "2025", "2026", "2027"];

    public ObservableCollection<string> StatusFilters { get; } =
        ["Tumu", "Gecikmis", "Yaklasan", "Tamamlanan"];

    public VergiTakvimiAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            // MediatR handler bağlantısı bekliyor — Task.Delay kaldırıldı

            _allItems =
            [
                new() { TaxName = "KDV Beyannamesi", DueDateFormatted = "26 Mart 2026", StatusText = "6 gun kaldi", AmountFormatted = "8.450,00 TL", StatusColor = "#DC2626" },
                new() { TaxName = "Muhtasar Beyanname", DueDateFormatted = "26 Mart 2026", StatusText = "6 gun kaldi", AmountFormatted = "3.200,00 TL", StatusColor = "#DC2626" },
                new() { TaxName = "SGK Prim Bildirge", DueDateFormatted = "31 Mart 2026", StatusText = "11 gun kaldi", AmountFormatted = "12.680,00 TL", StatusColor = "#D97706" },
                new() { TaxName = "Gecici Vergi (1. Donem)", DueDateFormatted = "17 Mayis 2026", StatusText = "58 gun kaldi", AmountFormatted = "—", StatusColor = "#059669" },
                new() { TaxName = "Kurumlar Vergisi", DueDateFormatted = "30 Nisan 2026", StatusText = "41 gun kaldi", AmountFormatted = "—", StatusColor = "#059669" },
            ];

            OverdueCount = _allItems.Count(x => x.StatusColor == "#DC2626");
            UpcomingCount = _allItems.Count(x => x.StatusColor == "#D97706");
            CompletedCount = 0;

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Vergi takvimi yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedStatusChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (SelectedStatus == "Gecikmis")
            filtered = filtered.Where(x => x.StatusColor == "#DC2626");
        else if (SelectedStatus == "Yaklasan")
            filtered = filtered.Where(x => x.StatusColor == "#D97706");
        else if (SelectedStatus == "Tamamlanan")
            filtered = filtered.Where(x => x.StatusColor == "#059669");

        Items.Clear();
        foreach (var item in filtered)
            Items.Add(item);

        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class TaxCalendarItemDto
{
    public string TaxName { get; set; } = string.Empty;
    public string DueDateFormatted { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string AmountFormatted { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#6B7280";
}
