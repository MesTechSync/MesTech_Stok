using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Queries.GetProductByBarcode;
using MesTech.Application.Queries.GetWarehouses;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Depo Arasi Transfer Wizard ViewModel — barkod/SKU arama + coklu urun + validasyon.
/// I-06 Gorev 4: Mevcut TransferStockCommand kullanir — yeni command YOK.
/// </summary>
public partial class TransferWizardAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private IReadOnlyList<WarehouseListDto> _warehouseList = [];

    [ObservableProperty] private string transferStatus = string.Empty;
    [ObservableProperty] private string productSearchText = string.Empty;

    [ObservableProperty] private string? selectedSourceWarehouse;
    [ObservableProperty] private string? selectedTargetWarehouse;

    public ObservableCollection<string> Warehouses { get; } = [];
    public ObservableCollection<TransferWizardItemDto> TransferItems { get; } = [];
    public ObservableCollection<string> Warnings { get; } = [];

    public bool IsSameWarehouse =>
        SelectedSourceWarehouse is not null &&
        SelectedTargetWarehouse is not null &&
        SelectedSourceWarehouse == SelectedTargetWarehouse;

    public bool HasWarnings => Warnings.Count > 0;

    public string TransferSummary
    {
        get
        {
            var count = TransferItems.Count(i => i.TransferQuantity > 0);
            var total = TransferItems.Sum(i => i.TransferQuantity);
            return $"{count} urun, {total} adet";
        }
    }

    public TransferWizardAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            _warehouseList = await _mediator.Send(new GetWarehousesQuery(ActiveOnly: true));

            Warehouses.Clear();
            foreach (var wh in _warehouseList)
                Warehouses.Add(wh.Name);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Depolar yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            IsEmpty = TransferItems.Count == 0;
        }
    }

    partial void OnSelectedSourceWarehouseChanged(string? value)
    {
        OnPropertyChanged(nameof(IsSameWarehouse));
        ValidateTransfer();
    }

    partial void OnSelectedTargetWarehouseChanged(string? value)
    {
        OnPropertyChanged(nameof(IsSameWarehouse));
        ValidateTransfer();
    }

    [RelayCommand]
    private async Task AddProductAsync()
    {
        var search = ProductSearchText?.Trim();
        if (string.IsNullOrEmpty(search)) return;

        var dto = await _mediator.Send(new GetProductByBarcodeQuery(search)).ConfigureAwait(false);
        if (dto is null)
        {
            TransferStatus = $"Urun bulunamadi: {search}";
            return;
        }

        // Check if already in list
        if (TransferItems.Any(i => i.Sku == dto.SKU))
        {
            TransferStatus = $"{dto.SKU} zaten listede.";
            return;
        }

        TransferItems.Add(new TransferWizardItemDto
        {
            ProductId = dto.Id,
            Sku = dto.SKU,
            ProductName = dto.Name,
            SourceStock = dto.Stock,
            TargetStock = 0,
            MinimumStock = dto.MinimumStock,
        });
        ProductSearchText = string.Empty;
        TransferStatus = $"{dto.Name} eklendi.";
        OnPropertyChanged(nameof(TransferSummary));
        ValidateTransfer();
    }

    private Guid ResolveWarehouseId(string? name)
    {
        if (name is null) return Guid.Empty;
        var wh = _warehouseList.FirstOrDefault(w => w.Name == name);
        return wh?.Id ?? Guid.Empty;
    }

    [RelayCommand]
    private async Task ExecuteTransfer()
    {
        if (IsSameWarehouse) return;
        if (TransferItems.Count == 0) { TransferStatus = "Transfer listesi bos."; return; }

        var itemsToTransfer = TransferItems.Where(i => i.TransferQuantity > 0).ToList();
        if (itemsToTransfer.Count == 0) { TransferStatus = "Transfer adedi girilmedi."; return; }

        var sourceId = ResolveWarehouseId(SelectedSourceWarehouse);
        var targetId = ResolveWarehouseId(SelectedTargetWarehouse);
        if (sourceId == Guid.Empty || targetId == Guid.Empty)
        {
            TransferStatus = "Depo secimi gecersiz.";
            return;
        }

        IsLoading = true;
        TransferStatus = string.Empty;
        int successCount = 0;
        try
        {
            foreach (var item in itemsToTransfer)
            {
                var result = await _mediator.Send(new Application.Commands.TransferStock.TransferStockCommand(
                    ProductId: item.ProductId,
                    SourceWarehouseId: sourceId,
                    TargetWarehouseId: targetId,
                    Quantity: item.TransferQuantity,
                    Notes: $"Wizard transfer: {item.Sku}"
                ));

                if (result.IsSuccess)
                {
                    item.SourceStock = result.SourceRemainingStock;
                    item.TargetStock = result.TargetNewStock;
                    item.TransferQuantity = 0;
                    successCount++;
                }
                else
                {
                    Warnings.Add($"{item.Sku}: {result.ErrorMessage}");
                }
            }

            TransferStatus = $"{successCount}/{itemsToTransfer.Count} urun basariyla transfer edildi.";
            OnPropertyChanged(nameof(TransferSummary));
            OnPropertyChanged(nameof(HasWarnings));
            ValidateTransfer();
        }
        catch (Exception ex)
        {
            TransferStatus = $"Transfer hatasi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void Reset()
    {
        TransferItems.Clear();
        SelectedSourceWarehouse = null;
        SelectedTargetWarehouse = null;
        ProductSearchText = string.Empty;
        TransferStatus = string.Empty;
        Warnings.Clear();
        OnPropertyChanged(nameof(TransferSummary));
        OnPropertyChanged(nameof(HasWarnings));
    }

    private void ValidateTransfer()
    {
        Warnings.Clear();
        foreach (var item in TransferItems)
        {
            if (item.TransferQuantity > item.SourceStock)
                Warnings.Add($"{item.Sku}: Transfer adedi ({item.TransferQuantity}) kaynak stoktan ({item.SourceStock}) buyuk!");
            if (item.SourceStock - item.TransferQuantity <= item.MinimumStock && item.TransferQuantity > 0)
                Warnings.Add($"{item.Sku}: Transfer sonrasi kaynak stok kritik seviyeye duser (min: {item.MinimumStock}).");
        }
        OnPropertyChanged(nameof(HasWarnings));
    }

}

public partial class TransferWizardItemDto : ObservableObject
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    [ObservableProperty] private int sourceStock;
    [ObservableProperty] private int targetStock;
    [ObservableProperty] private int transferQuantity;
    public int MinimumStock { get; set; }
}
