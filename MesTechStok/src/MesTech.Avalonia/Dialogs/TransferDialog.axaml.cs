using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class TransferDialog : Window
{
    public bool Result { get; private set; }
    public string? SelectedSource => (SourceWarehouse.SelectedItem as ComboBoxItem)?.Content?.ToString();
    public string? SelectedTarget => (TargetWarehouse.SelectedItem as ComboBoxItem)?.Content?.ToString();
    public string? Product => ProductBox.Text;
    public int Quantity => (int)(QuantityInput.Value ?? 1);

    public TransferDialog() : this(Enumerable.Empty<string>()) { }

    public TransferDialog(IEnumerable<string> warehouses)
    {
        InitializeComponent();
        foreach (var w in warehouses)
        {
            SourceWarehouse.Items.Add(new ComboBoxItem { Content = w });
            TargetWarehouse.Items.Add(new ComboBoxItem { Content = w });
        }
    }

    private void OnTransfer(object? sender, RoutedEventArgs e)
    {
        if (SourceWarehouse.SelectedItem == null || TargetWarehouse.SelectedItem == null)
        {
            ErrorText.Text = "Kaynak ve hedef depoyu seciniz.";
            ErrorText.IsVisible = true;
            return;
        }

        if (SelectedSource == SelectedTarget)
        {
            ErrorText.Text = "Kaynak ve hedef depo ayni olamaz.";
            ErrorText.IsVisible = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(ProductBox.Text))
        {
            ErrorText.Text = "Urun bilgisi giriniz.";
            ErrorText.IsVisible = true;
            return;
        }

        Result = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }
}
