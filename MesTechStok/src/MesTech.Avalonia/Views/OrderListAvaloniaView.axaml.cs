using Avalonia.Controls;
using Avalonia.Input;
using MesTech.Avalonia.Views.Base;

namespace MesTech.Avalonia.Views;

public partial class OrderListAvaloniaView : BaseView
{
    public OrderListAvaloniaView()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && e.KeyModifiers == KeyModifiers.Control)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            SearchBox.Clear();
            e.Handled = true;
        }
    }
}
