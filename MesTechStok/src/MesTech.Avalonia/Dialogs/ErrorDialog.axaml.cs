using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class ErrorDialog : Window
{
    public ErrorDialog() : this(string.Empty) { }

    public ErrorDialog(string message, string? stackTrace = null)
    {
        InitializeComponent();
        MessageText.Text = message;
        DetailBox.Text = stackTrace ?? string.Empty;
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
