using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Login screen ViewModel — Username + Password + authentication.
/// Production: authenticates via API. Debug: accepts demo credentials.
/// </summary>
public partial class LoginAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private bool isAuthenticated;
    [ObservableProperty] private string welcomeMessage = string.Empty;

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(100); // Simulate init
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Giris ekrani yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            HasError = true;
            ErrorMessage = "Kullanici adi ve sifre bos birakilamaz.";
            return;
        }

        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(500); // Simulate auth call

#if DEBUG
            // Debug-only demo credentials — stripped from Release builds
            if (Username == "admin" && Password == "1234")
            {
                IsAuthenticated = true;
                WelcomeMessage = $"Hosgeldiniz, {Username}!";
                return;
            }
#endif
            HasError = true;
            ErrorMessage = "Gecersiz kullanici adi veya sifre.";
            IsAuthenticated = false;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Giris yapilamadi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}
