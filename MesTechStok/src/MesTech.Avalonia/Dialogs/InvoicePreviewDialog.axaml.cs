using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public class InvoiceItemRow
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string UnitPrice { get; set; } = "0.00";
    public string VatRate { get; set; } = "0";
    public string Total { get; set; } = "0.00";
}

public partial class InvoicePreviewDialog : Window
{
    public InvoicePreviewDialog() : this(string.Empty, string.Empty, string.Empty,
        string.Empty, string.Empty, string.Empty,
        string.Empty, string.Empty, string.Empty,
        System.Array.Empty<InvoiceItemRow>(),
        string.Empty, string.Empty, string.Empty) { }

    public InvoicePreviewDialog(string invoiceNo, string invoiceDate, string invoiceType,
                                 string sellerName, string sellerAddress, string sellerTaxInfo,
                                 string buyerName, string buyerAddress, string buyerTaxInfo,
                                 IEnumerable<InvoiceItemRow> items,
                                 string subtotal, string vat, string grandTotal)
    {
        InitializeComponent();
        InvoiceNoText.Text = invoiceNo;
        InvoiceDateText.Text = invoiceDate;
        InvoiceTypeText.Text = invoiceType;
        SellerName.Text = sellerName;
        SellerAddress.Text = sellerAddress;
        SellerTaxInfo.Text = sellerTaxInfo;
        BuyerName.Text = buyerName;
        BuyerAddress.Text = buyerAddress;
        BuyerTaxInfo.Text = buyerTaxInfo;
        ItemsGrid.ItemsSource = items;
        SubtotalText.Text = subtotal;
        VatText.Text = vat;
        GrandTotalText.Text = grandTotal;
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
}
