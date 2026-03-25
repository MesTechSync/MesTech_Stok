using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Login screen ViewModel — Username + Password + authentication.
/// Production: authenticates via IAuthService (DI resolved at runtime).
/// Offline mode: shows "Auth servisi yapılandırılmadı" message.
/// </summary>
public partial class LoginAvaloniaViewModel : ViewModelBase
{
    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private bool isAuthenticated;
    [ObservableProperty] private string welcomeMessage = string.Empty;

    public override async Task LoadAsync()
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
            // Auth validation via MediatR login command (IAuthService DI resolved at runtime)
            bool result = await ValidateCredentialsAsync(Username, Password);

            if (result)
            {
                IsAuthenticated = true;
                WelcomeMessage = $"Hosgeldiniz, {Username}!";
                return;
            }

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

    private Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        // Auth servisi DI'a kayıtlı değil — DEV1 IAuthService kaydı ekleyecek.
        // Şimdilik offline mode: herhangi bir kullanıcı/şifre ile giriş yapılamaz.
        ErrorMessage = "Auth servisi henüz yapılandırılmadı. DI kaydı bekliyor.";
        return Task.FromResult(false);
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}
