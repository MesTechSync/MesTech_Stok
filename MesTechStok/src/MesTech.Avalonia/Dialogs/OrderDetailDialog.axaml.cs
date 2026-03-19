using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public class OrderItemRow
{
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string UnitPrice { get; set; } = "0.00";
    public string Total { get; set; } = "0.00";
}

public partial class OrderDetailDialog : Window
{
    public OrderDetailDialog(string orderNo, string orderDate, string status,
                             IEnumerable<OrderItemRow> items, string totalAmount)
    {
        InitializeComponent();
        OrderNoText.Text = orderNo;
        OrderDateText.Text = orderDate;
        StatusText.Text = status;
        ItemsGrid.ItemsSource = items;
        TotalText.Text = totalAmount;
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
