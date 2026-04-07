using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MesTech.Domain.Entities.Finance;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Çek/Senet Takip ViewModel — S1-DEV2-03 (Menü 32).
/// Portföy DataGrid, vade takvimi, karşılıksız uyarı.
/// DEV1 handler (GetChequesQuery) bekliyor — şimdilik boş state.
/// </summary>
public partial class ChequeAvaloniaViewModel : ViewModelBase
{
    // KPI
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int inPortfolioCount;
    [ObservableProperty] private int overdueCount;
    [ObservableProperty] private int bouncedCount;
    [ObservableProperty] private decimal totalAmount;

    // Filter
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedType = "Tumu";
    [ObservableProperty] private string selectedStatus = "Tumu";
    public string[] TypeOptions { get; } = ["Tumu", "Alinan", "Verilen"];
    public string[] StatusOptions { get; } = ["Tumu", "Portfoyde", "Tahsile", "Tahsil Edildi", "Karsiliks\u0131z", "Ciro", "Iptal"];

    // Pagination
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalPages = 1;
    [ObservableProperty] private string paginationInfo = string.Empty;
    public int[] PageSizeOptions { get; } = [25, 50, 100];

    public ObservableCollection<ChequeItemDto> Cheques { get; } = [];
    private List<ChequeItemDto> _allCheques = [];

    public override Task LoadAsync()
    {
        // DEP: GetChequesQuery henüz yok — DEV1 handler gerekli.
        // Hazır olduğunda: _mediator.Send(new GetChequesQuery(TenantId))
        IsEmpty = true;
        return Task.CompletedTask;
    }

    partial void OnSearchTextChanged(string value) { if (_allCheques.Count > 0) { CurrentPage = 1; ApplyFilters(); } }
    partial void OnSelectedTypeChanged(string value) { if (_allCheques.Count > 0) { CurrentPage = 1; ApplyFilters(); } }
    partial void OnSelectedStatusChanged(string value) { if (_allCheques.Count > 0) { CurrentPage = 1; ApplyFilters(); } }
    partial void OnPageSizeChanged(int value) { CurrentPage = 1; if (_allCheques.Count > 0) ApplyFilters(); }

    private void ApplyFilters()
    {
        var filtered = _allCheques.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText.ToLowerInvariant();
            filtered = filtered.Where(c =>
                c.ChequeNumber.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                c.BankName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                c.DrawerName.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedType == "Alinan") filtered = filtered.Where(c => c.Type == "Alinan");
        else if (SelectedType == "Verilen") filtered = filtered.Where(c => c.Type == "Verilen");

        if (SelectedStatus != "Tumu")
            filtered = filtered.Where(c => c.Status == SelectedStatus);

        var all = filtered.ToList();
        TotalCount = all.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;

        Cheques.Clear();
        foreach (var c in all.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
            Cheques.Add(c);

        IsEmpty = TotalCount == 0;
        PaginationInfo = TotalCount > 0 ? $"Sayfa {CurrentPage}/{TotalPages} ({TotalCount} cek)" : string.Empty;

        // KPI
        InPortfolioCount = _allCheques.Count(c => c.Status == "Portfoyde");
        OverdueCount = _allCheques.Count(c => c.IsOverdue);
        BouncedCount = _allCheques.Count(c => c.Status == "Karsiliks\u0131z");
        TotalAmount = _allCheques.Sum(c => c.Amount);
    }

    [RelayCommand]
    private void NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; ApplyFilters(); } }
    [RelayCommand]
    private void PrevPage() { if (CurrentPage > 1) { CurrentPage--; ApplyFilters(); } }

    [RelayCommand]
    private Task Refresh() => LoadAsync();
}

public class ChequeItemDto
{
    public Guid Id { get; set; }
    public string ChequeNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string MaturityDate { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string DrawerName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }

    public string StatusColor => Status switch
    {
        "Portfoyde" => "#3B82F6",
        "Tahsile" => "#F59E0B",
        "Tahsil Edildi" => "#10B981",
        "Karsiliks\u0131z" => "#EF4444",
        "Ciro" => "#8B5CF6",
        "Iptal" => "#64748B",
        _ => "#64748B"
    };

    public string AmountDisplay => $"\u20BA{Amount:N2}";
}
