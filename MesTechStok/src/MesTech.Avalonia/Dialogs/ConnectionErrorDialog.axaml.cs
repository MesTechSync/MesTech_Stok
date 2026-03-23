using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class ConnectionErrorDialog : Window
{
    public bool RetryRequested { get; private set; }

    public ConnectionErrorDialog() : this("Bilinmeyen hata") { }

    public ConnectionErrorDialog(string errorMessage, string? server = null)
    {
        InitializeComponent();
        ErrorDetailBox.Text = errorMessage;
        ServerBox.Text = server ?? "localhost";
    }

    private void OnRetry(object? sender, RoutedEventArgs e)
    {
        RetryRequested = true;
        Close();
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        RetryRequested = false;
        Close();
    }
}
