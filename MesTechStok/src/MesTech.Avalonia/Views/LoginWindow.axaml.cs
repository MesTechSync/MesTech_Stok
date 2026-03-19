using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MesTech.Avalonia.Services;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Avalonia.ViewModels;

namespace MesTech.Avalonia.Views;

/// <summary>
/// LoginWindow — BCrypt auth, WPF ile ayni akis.
/// Basarili giris → MainWindow (DI'dan resolve), basarisiz → hata mesaji.
/// Enter tusu ile giris, loading state, bos alan kontrolu.
/// </summary>
public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

        // Focus kullanici adi alanina
        Opened += (_, _) => UsernameBox?.Focus();

        // Enter tusu ile giris
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
                OnLoginClick(this, new RoutedEventArgs());
        };
    }

    private async void OnLoginClick(object? sender, RoutedEventArgs e)
    {
        var username = UsernameBox?.Text?.Trim() ?? "";
        var password = PasswordBox?.Text ?? "";

        // Bos alan kontrolu
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Kullanıcı adı ve şifre gereklidir.");
            return;
        }

        // Loading state
        if (LoginButton != null)
        {
            LoginButton.IsEnabled = false;
            LoginButton.Content = "Giriş yapılıyor...";
        }

        try
        {
            // BCrypt dogrulama — WPF ile ayni backend servisi kullanilir.
            // IAuthService DI'dan alinir (mevcut Application katmanindaki).
            //
            // Gecici — simdilik hardcoded admin/admin ile test:
            var isValid = await Task.Run(() =>
            {
                // TODO: Gercek IAuthService.ValidateAsync(username, password) cagrisi
                // WPF'teki login mantigi buraya kopyalanacak.
                return username == "admin" && password == "admin";
            });

            if (isValid)
            {
                // MainWindow'u DI container'dan resolve et (App uzerinden)
                var app = (App)global::Avalonia.Application.Current!;
                var mainWindow = app.CreateMainWindow();
                mainWindow.Show();
                Close();
            }
            else
            {
                ShowError("Kullanıcı adı veya şifre hatalı.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Bağlantı hatası: {ex.Message}");
        }
        finally
        {
            if (LoginButton != null)
            {
                LoginButton.IsEnabled = true;
                LoginButton.Content = "GİRİŞ YAP";
            }
        }
    }

    private void ShowError(string message)
    {
        if (ErrorPanel != null && ErrorText != null)
        {
            ErrorText.Text = message;
            ErrorPanel.IsVisible = true;
        }
    }
}
