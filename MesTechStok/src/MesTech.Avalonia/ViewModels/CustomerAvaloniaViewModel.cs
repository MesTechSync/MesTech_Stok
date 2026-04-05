using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Crm.Commands.ExportCustomers;
using MesTech.Application.Features.Crm.Queries.GetCustomersCrm;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Musteri CRM ViewModel — segment filtreleme, detay paneli, sag-tik menu.
/// WPF018: GetCustomersCrmQuery ile MediatR entegrasyonu.
/// </summary>
public partial class CustomerAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string activeSegment = "Tum";
    [ObservableProperty] private bool isDetailOpen;
    [ObservableProperty] private CustomerItemDto? selectedCustomer;

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

    private readonly List<CustomerItemDto> _allItems = [];

    public ObservableCollection<CustomerItemDto> Items { get; } = [];

    // Segment filter buttons
    public static IReadOnlyList<SegmentFilterItem> SegmentFilters { get; } =
    [
        new("Tum",   "Tüm",   "#6B7280"),
        new("VIP",   "VIP",   "#D4AF37"),
        new("Normal","Normal","#3B82F6"),
        new("Pasif", "Pasif", "#9CA3AF"),
    ];

    public CustomerAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetCustomersCrmQuery(
                _currentUser.TenantId,
                IsVip: null,
                IsActive: null,
                SearchTerm: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                Page: 1,
                PageSize: 200), ct);

            _allItems.Clear();

            if (result?.Items is { Count: > 0 } crmItems)
            {
                foreach (var c in crmItems)
                {
                    var segment = c.IsVip ? "VIP" : c.IsActive ? "Normal" : "Pasif";
                    _allItems.Add(new CustomerItemDto
                    {
                        Id = c.Id,
                        AdSoyad = c.Name,
                        Email = c.Email ?? string.Empty,
                        Telefon = c.Phone ?? string.Empty,
                        Sehir = c.City ?? string.Empty,
                        SiparisSayisi = 0,
                        ToplamHarcama = c.CurrentBalance,
                        SonSiparis = c.LastOrderDate.HasValue
                            ? c.LastOrderDate.Value.ToString("dd.MM.yyyy")
                            : "--",
                        Segment = segment,
                        SegmentRenk = SegmentColor(segment),
                    });
                }
            }

            ApplyFilter();
        }, "Musteriler yuklenirken hata");
    }

    private static string SegmentColor(string segment) => segment switch
    {
        "VIP"    => "#D4AF37",
        "Normal" => "#3B82F6",
        "Pasif"  => "#9CA3AF",
        _        => "#6B7280",
    };

    private void ApplyFilter()
    {
        Items.Clear();
        var filtered = _allItems.AsEnumerable();

        // Segment filter
        if (ActiveSegment != "Tum")
            filtered = filtered.Where(c => c.Segment == ActiveSegment);

        // Text search
        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(c =>
                c.AdSoyad.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                c.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                c.Telefon.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        // Sort
        var sortedList = SortColumn switch
        {
            "AdSoyad"       => SortAscending ? filtered.OrderBy(x => x.AdSoyad).ToList()         : filtered.OrderByDescending(x => x.AdSoyad).ToList(),
            "Email"         => SortAscending ? filtered.OrderBy(x => x.Email).ToList()            : filtered.OrderByDescending(x => x.Email).ToList(),
            "ToplamHarcama" => SortAscending ? filtered.OrderBy(x => x.ToplamHarcama).ToList()    : filtered.OrderByDescending(x => x.ToplamHarcama).ToList(),
            "Segment"       => SortAscending ? filtered.OrderBy(x => x.Segment).ToList()          : filtered.OrderByDescending(x => x.Segment).ToList(),
            "SiparisSayisi" => SortAscending ? filtered.OrderBy(x => x.SiparisSayisi).ToList()    : filtered.OrderByDescending(x => x.SiparisSayisi).ToList(),
            _               => SortAscending ? filtered.OrderBy(x => x.AdSoyad).ToList()          : filtered.OrderByDescending(x => x.AdSoyad).ToList(),
        };

        foreach (var item in sortedList)
            Items.Add(item);

        TotalCount = Items.Count;
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        ApplyFilter();
    }

    // HH-FIX-014: Excel export
    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new ExportCustomersCommand(
                _currentUser.TenantId, "xlsx",
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText), ct);

            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(
                    System.IO.Path.Combine(dir, result.FileName), result.FileData.ToArray());
            }
        }, "Musteriler disa aktarilirken hata");
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void FilterSegment(string segment)
    {
        ActiveSegment = segment;
        ApplyFilter();
    }

    [RelayCommand]
    private void OpenDetail(CustomerItemDto? customer)
    {
        if (customer is null) return;
        SelectedCustomer = customer;
        IsDetailOpen = true;
    }

    [RelayCommand]
    private void CloseDetail()
    {
        IsDetailOpen = false;
        SelectedCustomer = null;
    }

    [RelayCommand]
    private void ChangeSegment(string newSegment)
    {
        if (SelectedCustomer is null) return;
        SelectedCustomer.Segment = newSegment;
        SelectedCustomer.SegmentRenk = SegmentColor(newSegment);
        // Refresh list to reflect the change
        ApplyFilter();
    }

    [RelayCommand]
    private void ShowOrders(CustomerItemDto? customer)
    {
        // Navigation placeholder — future: navigate to OrdersView filtered by customer
    }

    [RelayCommand]
    private void AddNote(CustomerItemDto? customer)
    {
        // Placeholder — future: open NoteDialog
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_allItems.Count > 0)
            ApplyFilter();
        else if (value.Length == 0 || value.Length >= 2)
            _ = LoadAsync();
    }

    partial void OnSelectedCustomerChanged(CustomerItemDto? value)
    {
        if (value is not null)
            IsDetailOpen = true;
    }
}

public class CustomerItemDto
{
    public Guid Id { get; set; }
    public string AdSoyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string Sehir { get; set; } = string.Empty;
    public int SiparisSayisi { get; set; }
    public decimal ToplamHarcama { get; set; }
    public string ToplamHarcamaStr => ToplamHarcama.ToString("N2") + " ₺";
    public string SonSiparis { get; set; } = "--";
    public string Segment { get; set; } = "Normal";
    public string SegmentRenk { get; set; } = "#3B82F6";
}

public record SegmentFilterItem(string Key, string Label, string Color);
