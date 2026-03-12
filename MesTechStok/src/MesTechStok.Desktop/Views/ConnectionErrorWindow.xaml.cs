using System;
using System.Windows;
using System.Windows.Input;

namespace MesTechStok.Desktop.Views;

/// <summary>
/// DB baglantisi kurulamadiginda gosterilen pencere.
/// Tekrar Dene / Ayarlar / Cikis secenekleri sunar.
/// Bitrix24 tema uyumlu, borderless modern tasarim.
/// </summary>
public partial class ConnectionErrorWindow : Window
{
    private readonly Func<bool>? _retryAction;

    /// <summary>
    /// Kullanici basariyla baglandiysa true doner.
    /// </summary>
    public bool ConnectionSucceeded { get; private set; }

    /// <summary>DEV 1 will subscribe to this event from App.xaml.cs</summary>
    public event EventHandler? RetryRequested;

    /// <summary>DEV 1 will subscribe to this event from App.xaml.cs</summary>
    public event EventHandler? SettingsRequested;

    /// <summary>
    /// Legacy constructor — backward compatible with existing App.xaml.cs caller.
    /// </summary>
    public ConnectionErrorWindow(string connectionInfo, string errorDetails, Func<bool> retryAction)
    {
        InitializeComponent();

        _retryAction = retryAction;

        ErrorMessageText.Text = errorDetails;
        ParseConnectionInfo(connectionInfo);
    }

    /// <summary>
    /// D-11 pattern: optional null params for WPF designer compatibility.
    /// </summary>
    public ConnectionErrorWindow(
        string? errorMessage = null,
        string? host = null,
        int? port = null,
        string? database = null)
    {
        InitializeComponent();

        ErrorMessageText.Text = errorMessage ?? "Veritabani sunucusuna baglanilamadi.";
        HostText.Text = host ?? "localhost";
        PortText.Text = (port ?? 5432).ToString();
        DatabaseText.Text = database ?? "mestech_stok";
    }

    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        // If legacy retryAction is provided, use it
        if (_retryAction is not null)
        {
            RetryButton.IsEnabled = false;
            RetryButtonText.Text = "Baglaniyor...";

            try
            {
                var success = _retryAction();
                if (success)
                {
                    ConnectionSucceeded = true;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ErrorMessageText.Text = $"[{DateTime.Now:HH:mm:ss}] Baglanti tekrar basarisiz.\n\n{ErrorMessageText.Text}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessageText.Text = $"[{DateTime.Now:HH:mm:ss}] Baglanti hatasi: {ex.Message}";
            }
            finally
            {
                RetryButton.IsEnabled = true;
                RetryButtonText.Text = "Tekrar Dene";
            }
        }

        // Always fire the event for new subscribers
        RetryRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        // Fire event for new subscribers
        SettingsRequested?.Invoke(this, EventArgs.Empty);

        // Legacy behavior: open appsettings.json if no event subscribers
        if (SettingsRequested is null)
        {
            var appSettingsPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            try
            {
                if (System.IO.File.Exists(appSettingsPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = appSettingsPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show(
                        $"appsettings.json bulunamadi:\n{appSettingsPath}\n\nDosyayi olusturup baglanti bilgilerini ekleyin.",
                        "Ayar Dosyasi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dosya acilamadi: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        ConnectionSucceeded = false;

        // If shown as dialog, set DialogResult; otherwise shutdown
        if (_retryAction is not null)
        {
            DialogResult = false;
            Close();
        }
        else
        {
            Application.Current.Shutdown();
        }
    }

    // Custom title bar drag support
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    /// <summary>
    /// Parse masked connection string into host/port/database fields.
    /// Expected format: "Host=xxx;Port=xxx;Database=xxx;Password=****"
    /// </summary>
    private void ParseConnectionInfo(string connectionInfo)
    {
        HostText.Text = "localhost";
        PortText.Text = "5432";
        DatabaseText.Text = "mestech_stok";

        if (string.IsNullOrWhiteSpace(connectionInfo))
            return;

        foreach (var part in connectionInfo.Split(';', StringSplitOptions.TrimEntries))
        {
            var kvp = part.Split('=', 2);
            if (kvp.Length != 2) continue;

            var key = kvp[0].Trim();
            var value = kvp[1].Trim();

            if (key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Server", StringComparison.OrdinalIgnoreCase))
            {
                HostText.Text = value;
            }
            else if (key.Equals("Port", StringComparison.OrdinalIgnoreCase))
            {
                PortText.Text = value;
            }
            else if (key.Equals("Database", StringComparison.OrdinalIgnoreCase))
            {
                DatabaseText.Text = value;
            }
            // Password is always masked in XAML — no need to parse
        }
    }
}
