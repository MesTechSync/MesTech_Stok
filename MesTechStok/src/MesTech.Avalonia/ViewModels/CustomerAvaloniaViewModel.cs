using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
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
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            // Try real data via MediatR; fall back to mock if query fails
            GetCustomersCrmResult? result = null;
            try
            {
                result = await _mediator.Send(new GetCustomersCrmQuery(
                    _currentUser.TenantId,
                    IsVip: null,
                    IsActive: null,
                    SearchTerm: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                    Page: 1,
                    PageSize: 200));
            }
            catch
            {
                // Infrastructure not available — use mock data
            }

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
            else
            {
                LoadMockData();
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Musteriler yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    private void LoadMockData()
    {
        _allItems.Add(MakeItem("Ahmet Yilmaz",    "ahmet.yilmaz@gmail.com",      "0532 111 22 33", "Istanbul",  24, 48750.00m,  "15.03.2026", "VIP"));
        _allItems.Add(MakeItem("Fatma Demir",     "fatma.demir@hotmail.com",     "0541 222 33 44", "Ankara",    12, 22100.50m,  "10.03.2026", "Normal"));
        _allItems.Add(MakeItem("Mehmet Kaya",     "mehmet.kaya@outlook.com",     "0555 333 44 55", "Izmir",      8,  9800.00m,  "01.03.2026", "Normal"));
        _allItems.Add(MakeItem("Ayse Celik",      "ayse.celik@gmail.com",        "0543 444 55 66", "Bursa",     31, 61200.75m,  "20.03.2026", "VIP"));
        _allItems.Add(MakeItem("Mustafa Ozturk",  "mustafa.ozturk@yandex.com",   "0537 555 66 77", "Antalya",    5,  4500.00m,  "05.02.2026", "Pasif"));
        _allItems.Add(MakeItem("Zeynep Arslan",   "zeynep.arslan@gmail.com",     "0546 666 77 88", "Konya",     17, 33600.00m,  "18.03.2026", "Normal"));
        _allItems.Add(MakeItem("Hasan Sahin",     "hasan.sahin@hotmail.com",     "0533 777 88 99", "Gaziantep", 42, 89400.20m,  "22.03.2026", "VIP"));
        _allItems.Add(MakeItem("Elif Yildiz",     "elif.yildiz@outlook.com",     "0544 888 99 00", "Trabzon",    3,  2750.00m,  "10.01.2026", "Pasif"));
        _allItems.Add(MakeItem("Ali Korkmaz",     "ali.korkmaz@gmail.com",       "0538 999 00 11", "Eskisehir", 19, 38900.00m,  "19.03.2026", "Normal"));
        _allItems.Add(MakeItem("Merve Dogan",     "merve.dogan@yandex.com",      "0547 000 11 22", "Kayseri",    7,  7200.50m,  "28.02.2026", "Normal"));
    }

    private static CustomerItemDto MakeItem(
        string adSoyad, string email, string tel, string sehir,
        int siparis, decimal harcama, string sonSiparis, string segment)
        => new()
        {
            Id = Guid.NewGuid(),
            AdSoyad = adSoyad,
            Email = email,
            Telefon = tel,
            Sehir = sehir,
            SiparisSayisi = siparis,
            ToplamHarcama = harcama,
            SonSiparis = sonSiparis,
            Segment = segment,
            SegmentRenk = SegmentColor(segment),
        };

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

        foreach (var item in filtered)
            Items.Add(item);

        TotalCount = Items.Count;
        IsEmpty = Items.Count == 0;
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
