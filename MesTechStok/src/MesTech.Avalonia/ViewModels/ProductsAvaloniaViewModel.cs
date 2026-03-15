using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Simplified product list ViewModel for Avalonia PoC.
/// WPF ProductsView is heavily code-behind — this proves the MVVM-first pattern
/// that should be applied to the WPF version as well.
/// </summary>
public partial class ProductsAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<ProductItemVm> Products { get; } = [];

    public ProductsAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            // Same MediatR pipeline — Application.Queries.GetProductsPaged
            await Task.Delay(50);

            Products.Clear();
            Products.Add(new ProductItemVm { Id = Guid.NewGuid(), Name = "Samsung Galaxy S24", Barcode = "8806095350127", Stock = 45, Price = 42999.00m, Category = "Telefon" });
            Products.Add(new ProductItemVm { Id = Guid.NewGuid(), Name = "Apple MacBook Air M3", Barcode = "0194253943952", Stock = 12, Price = 54999.00m, Category = "Bilgisayar" });
            Products.Add(new ProductItemVm { Id = Guid.NewGuid(), Name = "Sony WH-1000XM5", Barcode = "4548736132610", Stock = 78, Price = 11499.00m, Category = "Kulaklik" });
            Products.Add(new ProductItemVm { Id = Guid.NewGuid(), Name = "Logitech MX Master 3S", Barcode = "5099206101173", Stock = 156, Price = 3299.00m, Category = "Aksesuar" });
            Products.Add(new ProductItemVm { Id = Guid.NewGuid(), Name = "Dell U2723QE Monitor", Barcode = "5397184505120", Stock = 8, Price = 18799.00m, Category = "Monitor" });
            TotalCount = Products.Count;
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length == 0 || value.Length >= 2)
            _ = LoadAsync();
    }
}

public class ProductItemVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public int Stock { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}
