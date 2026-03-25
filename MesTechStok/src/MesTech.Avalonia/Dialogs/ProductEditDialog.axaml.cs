using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class ProductEditDialog : Window
{
    public bool Result { get; private set; }
    public string? ProductName => NameBox.Text;
    public string? Sku => SkuBox.Text;
    public string? Barcode => BarcodeBox.Text;
    public string? Price => PriceBox.Text;
    public string? Category => CategoryBox.Text;
    public string? Description => DescriptionBox.Text;

    public ProductEditDialog() : this("Urun Duzenle") { }

    public ProductEditDialog(string title = "Urun Duzenle",
                             string? name = null,
                             string? sku = null,
                             string? barcode = null,
                             string? price = null,
                             string? category = null,
                             string? description = null)
    {
        InitializeComponent();
        TitleText.Text = title;

        if (name != null) NameBox.Text = name;
        if (sku != null) SkuBox.Text = sku;
        if (barcode != null) BarcodeBox.Text = barcode;
        if (price != null) PriceBox.Text = price;
        if (category != null) CategoryBox.Text = category;
        if (description != null) DescriptionBox.Text = description;
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text)) return;
        Result = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Result = false;
            Close();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
}
