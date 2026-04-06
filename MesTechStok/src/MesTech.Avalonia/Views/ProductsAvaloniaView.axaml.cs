using Avalonia.Controls;
using Avalonia.Input;
using MesTech.Avalonia.Views.Base;

namespace MesTech.Avalonia.Views;

public partial class ProductsAvaloniaView : BaseView
{
    public ProductsAvaloniaView()
    {
        InitializeComponent();
    }

    protected override void SubscribeEvents() { base.SubscribeEvents(); KeyDown += OnKeyDown; }
    protected override void UnsubscribeEvents() { KeyDown -= OnKeyDown; base.UnsubscribeEvents(); }

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

    /// <summary>D2-025: DataGrid double-click → EditProduct dialog açar.</summary>
    private void OnDataGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is ViewModels.ProductsAvaloniaViewModel vm && vm.EditProductCommand.CanExecute(null))
            vm.EditProductCommand.Execute(null);
    }
}
