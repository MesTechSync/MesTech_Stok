using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Dialogs;

public partial class OpenCartSiteDetailDialog : Window
{
    public bool Result { get; private set; }
    public string? SiteName => SiteNameBox.Text;
    public string? SiteUrl => SiteUrlBox.Text;
    public string? OpenCartVersion => (VersionCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
    public string? AdminUrl => AdminUrlBox.Text;
    public string? ApiKey => ApiKeyBox.Text;
    public string? ApiSecret => ApiSecretBox.Text;
    public bool SyncProducts => SyncProductsCheck.IsChecked == true;
    public bool SyncOrders => SyncOrdersCheck.IsChecked == true;
    public bool SyncStock => SyncStockCheck.IsChecked == true;

    public OpenCartSiteDetailDialog() : this("OpenCart Site Detay") { }

    public OpenCartSiteDetailDialog(string title = "OpenCart Site Detay",
                                     string? siteName = null,
                                     string? siteUrl = null,
                                     string? adminUrl = null,
                                     string? apiKey = null)
    {
        InitializeComponent();
        Title = title;

        if (siteName != null) SiteNameBox.Text = siteName;
        if (siteUrl != null) SiteUrlBox.Text = siteUrl;
        if (adminUrl != null) AdminUrlBox.Text = adminUrl;
        if (apiKey != null) ApiKeyBox.Text = apiKey;
    }

    private void OnTestConnection(object? sender, RoutedEventArgs e)
    {
        // Connection test logic — to be implemented with actual API call
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SiteNameBox.Text) ||
            string.IsNullOrWhiteSpace(SiteUrlBox.Text))
            return;

        Result = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Result = false;
            Close();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
}
