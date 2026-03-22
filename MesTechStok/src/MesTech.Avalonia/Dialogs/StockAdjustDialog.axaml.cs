using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class StockAdjustDialog : Window
{
    public bool Result { get; private set; }
    public int AdjustQuantity => (int)(QuantityInput.Value ?? 0);
    public string? Reason => ReasonBox.Text;

    public StockAdjustDialog() : this(string.Empty, 0) { }

    public StockAdjustDialog(string productName, int currentStock)
    {
        InitializeComponent();
        ProductNameText.Text = productName;
        CurrentStockText.Text = $"Mevcut Stok: {currentStock}";
    }

    private void OnApply(object? sender, RoutedEventArgs e)
    {
        if (QuantityInput.Value == 0) return;
        Result = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }
}
