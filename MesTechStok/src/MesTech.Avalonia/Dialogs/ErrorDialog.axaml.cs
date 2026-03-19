using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class ErrorDialog : Window
{
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
}
