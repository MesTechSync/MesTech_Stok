using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class ContactAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<ContactItemVm> Contacts { get; } = [];

    public ContactAvaloniaViewModel(IMediator mediator)
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
            await Task.Delay(50);
            Contacts.Clear();
            Contacts.Add(new ContactItemVm { Id = Guid.NewGuid(), FullName = "Ahmet Yilmaz", Company = "ABC Ltd", Email = "ahmet@abc.com", Phone = "0532 123 45 67", City = "Istanbul", Type = "Musteri" });
            Contacts.Add(new ContactItemVm { Id = Guid.NewGuid(), FullName = "Fatma Demir", Company = "XYZ AS", Email = "fatma@xyz.com", Phone = "0541 987 65 43", City = "Ankara", Type = "Lead" });
            Contacts.Add(new ContactItemVm { Id = Guid.NewGuid(), FullName = "Mehmet Can", Company = "DEF Tic.", Email = "mehmet@def.com", Phone = "0555 111 22 33", City = "Izmir", Type = "Musteri" });
            Contacts.Add(new ContactItemVm { Id = Guid.NewGuid(), FullName = "Ayse Kara", Company = "GHI Ltd", Email = "ayse@ghi.com", Phone = "0544 222 33 44", City = "Bursa", Type = "Tedarikci" });
            TotalCount = Contacts.Count;
            IsEmpty = Contacts.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Kisiler yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length == 0 || value.Length >= 2)
            _ = LoadAsync();
    }
}

public class ContactItemVm
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string Type { get; set; } = string.Empty;
}
