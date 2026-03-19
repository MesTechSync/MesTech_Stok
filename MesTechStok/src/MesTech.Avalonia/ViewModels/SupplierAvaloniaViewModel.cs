using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Tedarikciler ViewModel — tedarikci listesi DataGrid.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class SupplierAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<SupplierItemDto> Suppliers { get; } = [];

    public SupplierAvaloniaViewModel(IMediator mediator)
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

            Suppliers.Clear();
            Suppliers.Add(new SupplierItemDto { SupplierName = "ABC Elektronik Ltd.", ContactPerson = "Hasan Yildiz", Phone = "0212 555 1234", Email = "hasan@abcelektronik.com", City = "Istanbul", Balance = 125000.00m });
            Suppliers.Add(new SupplierItemDto { SupplierName = "XYZ Bilisim A.S.", ContactPerson = "Zeynep Arslan", Phone = "0312 444 5678", Email = "zeynep@xyzbilisim.com", City = "Ankara", Balance = 48500.00m });
            Suppliers.Add(new SupplierItemDto { SupplierName = "Guney Aksesuar", ContactPerson = "Murat Can", Phone = "0232 333 9012", Email = "murat@guneyaksesuar.com", City = "Izmir", Balance = 12750.50m });

            TotalCount = Suppliers.Count;
            IsEmpty = TotalCount == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Tedarikciler yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class SupplierItemDto
{
    public string SupplierName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
