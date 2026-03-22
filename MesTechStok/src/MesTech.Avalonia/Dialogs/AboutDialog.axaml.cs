using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class AboutDialog : Window
{
    public AboutDialog() : this("v1.0.0") { }

    public AboutDialog(string version = "v1.0.0", string? license = null)
    {
        InitializeComponent();
        VersionText.Text = version;
        if (license != null)
            LicenseText.Text = license;
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
