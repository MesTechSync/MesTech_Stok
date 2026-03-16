using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Core.Services.Abstract;
using MesTechStok.Desktop.Services;
using MesTechStok.Desktop.ViewModels;

namespace MesTechStok.Desktop.Views;

/// <summary>
/// LoginWindow - FAZ 1 GÖREV 1.1 Authentication
/// </summary>
public partial class LoginWindow : Window
{
    private readonly IAuthService _authService;
    private bool _isLoggingIn = false;
    public string? TargetModule { get; set; }

    public LoginWindow()
    {
        InitializeComponent();

        // Get AuthService from DI container - Fixed static access
        _authService = App.Services!.GetRequiredService<IAuthService>();

        // Setup events
        this.KeyDown += LoginWindow_KeyDown;

        // Focus ve UI setup
        this.Loaded += LoginWindow_Loaded;

        // Demo credentials but NOT pre-filled for security
        // User must manually enter credentials
    }

    private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Kullanıcı adı alanına odaklan
            UsernameTextBox.Focus();

            // Ensure password visibility defaults
            UpdatePasswordVisibility(false);
            if (MenuShowPassword != null)
            {
                MenuShowPassword.IsChecked = false;
            }
            if (ShowPasswordCheckBox != null)
            {
                ShowPasswordCheckBox.IsChecked = false;
            }

            // Force update layout
            this.UpdateLayout();
        }
        catch (Exception ex)
        {
            ShowStatus($"Login window setup error: {ex.Message}", "Error");
        }
    }

    private void LoginWindow_KeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                CloseButton_Click(sender, e);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"KeyDown handler error: {ex.Message}", "Error");
        }
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isLoggingIn) return;

        try
        {
            _isLoggingIn = true;
            LoginButton.Content = "🔄 Giriş yapılıyor...";
            LoginButton.IsEnabled = false;
            StatusMessage.Text = "";

            var username = UsernameTextBox.Text.Trim();
            var password = PasswordVisibleTextBox != null && PasswordVisibleTextBox.Visibility == Visibility.Visible
                ? PasswordVisibleTextBox.Text
                : PasswordBox.Password;

            // Validation
            if (string.IsNullOrEmpty(username))
            {
                ShowStatus("Kullanıcı adı boş olamaz", "Error");
                UsernameTextBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowStatus("Sifre bos olamaz", "Error");
                PasswordBox.Focus();
                return;
            }

            // Attempt login
            var result = await _authService.LoginAsync(username, password);

            if (result.IsSuccess && result.User != null)
            {
                ShowStatus($"Hoş geldiniz {result.User.FullName}!", "Success");

                // Wait a moment to show success message
                await Task.Delay(1500);

                // Security: SimpleSecurityService integration pending
                // Şu anda önbellek sıfırlama yapılmıyor

                // Create or reuse single WelcomeWindow instance
                App.WelcomeWindowInstance ??= new Views.WelcomeWindow(TargetModule);
                var welcomeWindow = App.WelcomeWindowInstance;
                if (!welcomeWindow.IsVisible) welcomeWindow.Show();
                else welcomeWindow.Activate();

                // Close login window
                this.Close();
            }
            else
            {
                ShowStatus(result.Message, "Error");
                PasswordBox.Password = "";
                if (PasswordVisibleTextBox != null) PasswordVisibleTextBox.Text = "";
                PasswordBox.Focus();
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Giriş hatası: {ex.Message}", "Error");
        }
        finally
        {
            _isLoggingIn = false;
            LoginButton.Content = "🚀 Sisteme Giriş Yap";
            LoginButton.IsEnabled = true;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            ShowStatus($"CloseButton handler error: {ex.Message}", "Error");
        }
    }

    private void ShowStatus(string message, string type)
    {
        StatusMessage.Text = message;
        StatusBorder.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;

        // Update status border and text colors based on type
        StatusMessage.Foreground = type switch
        {
            "Success" => System.Windows.Media.Brushes.Green,
            "Error" => System.Windows.Media.Brushes.Red,
            "Info" => System.Windows.Media.Brushes.DarkBlue,
            _ => System.Windows.Media.Brushes.DarkBlue
        };

        StatusBorder.Background = type switch
        {
            "Success" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 253, 244)),
            "Error" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 242, 242)),
            "Info" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 246, 255)),
            _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(247, 250, 252))
        };

        StatusBorder.BorderBrush = type switch
        {
            "Success" => System.Windows.Media.Brushes.LightGreen,
            "Error" => System.Windows.Media.Brushes.LightCoral,
            "Info" => System.Windows.Media.Brushes.LightBlue,
            _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240))
        };
    }

    // Helpers and menu handlers
    private void UpdatePasswordVisibility(bool show)
    {
        if (PasswordVisibleTextBox == null || PasswordBox == null) return;

        if (show)
        {
            PasswordVisibleTextBox.Text = PasswordBox.Password;
            PasswordVisibleTextBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
        }
        else
        {
            PasswordBox.Password = PasswordVisibleTextBox.Text;
            PasswordVisibleTextBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
        }
    }

    private void ShowPasswordCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdatePasswordVisibility(true);
            if (MenuShowPassword != null) MenuShowPassword.IsChecked = true;
        }
        catch (Exception ex)
        {
            ShowStatus($"ShowPassword handler error: {ex.Message}", "Error");
        }
    }

    private void ShowPasswordCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdatePasswordVisibility(false);
            if (MenuShowPassword != null) MenuShowPassword.IsChecked = false;
        }
        catch (Exception ex)
        {
            ShowStatus($"ShowPassword handler error: {ex.Message}", "Error");
        }
    }

    private void MenuShowPassword_Checked(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdatePasswordVisibility(true);
            if (ShowPasswordCheckBox != null) ShowPasswordCheckBox.IsChecked = true;
        }
        catch (Exception ex)
        {
            ShowStatus($"MenuShowPassword handler error: {ex.Message}", "Error");
        }
    }

    private void MenuShowPassword_Unchecked(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdatePasswordVisibility(false);
            if (ShowPasswordCheckBox != null) ShowPasswordCheckBox.IsChecked = false;
        }
        catch (Exception ex)
        {
            ShowStatus($"MenuShowPassword handler error: {ex.Message}", "Error");
        }
    }

    private void MenuVirtualKeyboard_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "osk.exe",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ShowStatus($"Sanal klavye açılamadı: {ex.Message}", "Error");
        }
    }

    private void MenuForgotPassword_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            MessageBox.Show("Sifrenizi unuttuysaniz sistem yoneticinize basvurun.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowStatus($"ForgotPassword handler error: {ex.Message}", "Error");
        }
    }
}
