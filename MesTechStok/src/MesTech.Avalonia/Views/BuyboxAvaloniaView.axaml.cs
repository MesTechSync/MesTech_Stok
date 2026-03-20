using Avalonia.Controls;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

public partial class BuyboxAvaloniaView : UserControl
{
    public BuyboxAvaloniaView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is BuyboxAvaloniaViewModel vm)
                await vm.LoadDataCommand.ExecuteAsync(null);
        };
    }
}
