using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Login screen ViewModel — Username + Password + authentication.
/// Authenticates via IAuthService (BCrypt password verification).
/// </summary>
public partial class LoginAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IAuthService _authService;

    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private bool isAuthenticated;
    [ObservableProperty] private string welcomeMessage = string.Empty;

    public LoginAvaloniaViewModel(IMediator mediator, IAuthService authService)
    {
        _mediator = mediator;
        _authService = authService;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            // TODO: Wire to MediatR query when AuthenticateCommand is available
            await Task.CompletedTask;
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

    private async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        var result = await _authService.ValidateAsync(username, password);

        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage ?? "Kimlik doğrulama başarısız.";
            return false;
        }

        WelcomeMessage = $"Hoşgeldiniz, {result.DisplayName}!";
        return true;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}
