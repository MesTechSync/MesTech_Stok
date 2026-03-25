using Avalonia.Controls;
using Avalonia.Input;
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

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            RetryRequested = false;
            Close();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
}
