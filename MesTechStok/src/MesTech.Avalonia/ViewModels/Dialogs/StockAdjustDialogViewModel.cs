using CommunityToolkit.Mvvm.ComponentModel;

namespace MesTech.Avalonia.ViewModels.Dialogs;

/// <summary>ViewModel for StockAdjustDialog — stock quantity adjustment form.</summary>
public partial class StockAdjustDialogViewModel : ObservableObject
{
    [ObservableProperty] private string productName = string.Empty;
    [ObservableProperty] private int currentStock;
    [ObservableProperty] private int adjustQuantity;
    [ObservableProperty] private string reason = string.Empty;
}
