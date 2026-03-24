using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Musteri yonetimi ViewModel — arama destekli DataGrid.
/// Will be wired to GetCustomersPagedQuery via MediatR when full migration starts.
/// </summary>
public partial class CustomerAvaloniaViewModel : ViewModelBase
{
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    private readonly List<CustomerItemDto> _allItems = [];

    public ObservableCollection<CustomerItemDto> Items { get; } = [];

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(300);

            _allItems.Clear();
            _allItems.Add(new CustomerItemDto { AdSoyad = "Ahmet Yilmaz", Email = "ahmet.yilmaz@gmail.com", Telefon = "0532 111 22 33", Sehir = "Istanbul", SiparisSayisi = 24 });
            _allItems.Add(new CustomerItemDto { AdSoyad = "Fatma Demir", Email = "fatma.demir@hotmail.com", Telefon = "0541 222 33 44", Sehir = "Ankara", SiparisSayisi = 12 });
            _allItems.Add(new CustomerItemDto { AdSoyad = "Mehmet Kaya", Email = "mehmet.kaya@outlook.com", Telefon = "0555 333 44 55", Sehir = "Izmir", SiparisSayisi = 8 });
            _allItems.Add(new CustomerItemDto { AdSoyad = "Ayse Celik", Email = "ayse.celik@gmail.com", Telefon = "0543 444 55 66", Sehir = "Bursa", SiparisSayisi = 31 });
            _allItems.Add(new CustomerItemDto { AdSoyad = "Mustafa Ozturk", Email = "mustafa.ozturk@yandex.com", Telefon = "0537 555 66 77", Sehir = "Antalya", SiparisSayisi = 5 });
            _allItems.Add(new CustomerItemDto { AdSoyad = "Zeynep Arslan", Email = "zeynep.arslan@gmail.com", Telefon = "0546 666 77 88", Sehir = "Konya", SiparisSayisi = 17 });
            _allItems.Add(new CustomerItemDto { AdSoyad = "Hasan Sahin", Email = "hasan.sahin@hotmail.com", Telefon = "0533 777 88 99", Sehir = "Gaziantep", SiparisSayisi = 42 });
            _allItems.Add(new CustomerItemDto { AdSoyad = "Elif Yildiz", Email = "elif.yildiz@outlook.com", Telefon = "0544 888 99 00", Sehir = "Trabzon", SiparisSayisi = 3 });
            _allItems.Add(new CustomerItemDto { AdSoyad = "Ali Korkmaz", Email = "ali.korkmaz@gmail.com", Telefon = "0538 999 00 11", Sehir = "Eskisehir", SiparisSayisi = 19 });
            _allItems.Add(new CustomerItemDto { AdSoyad = "Merve Dogan", Email = "merve.dogan@yandex.com", Telefon = "0547 000 11 22", Sehir = "Kayseri", SiparisSayisi = 7 });

            ApplySearch();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Musteriler yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    private void ApplySearch()
    {
        Items.Clear();
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allItems
            : _allItems.Where(c =>
                c.AdSoyad.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                c.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var item in filtered)
            Items.Add(item);

        TotalCount = Items.Count;
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    partial void OnSearchTextChanged(string value)
    {
        if (_allItems.Count > 0)
            ApplySearch();
        else if (value.Length == 0 || value.Length >= 2)
            _ = LoadAsync();
    }
}

public class CustomerItemDto
{
    public string AdSoyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string Sehir { get; set; } = string.Empty;
    public int SiparisSayisi { get; set; }
}
