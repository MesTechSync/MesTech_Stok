using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Auth.Commands.EnableMfa;
using MesTech.Application.Features.Auth.Commands.VerifyTotp;
using Microsoft.Extensions.Logging;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// STD006: MFA Setup — QR kod göster + 6 haneli kod doğrula.
/// Flow: [MFA Etkinleştir] → EnableMfaCommand → QR URI → kullanıcı tarar →
///       6 haneli kod girer → VerifyTotpCommand → başarılı = MFA aktif.
/// OWASP ASVS V2.8
/// </summary>
public partial class MfaSetupViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<MfaSetupViewModel> _logger;

    [ObservableProperty] private bool isMfaEnabled;
    [ObservableProperty] private bool isSetupStarted;
    [ObservableProperty] private string qrCodeUri = string.Empty;
    [ObservableProperty] private string secretKey = string.Empty;
    [ObservableProperty] private string verificationCode = string.Empty;
    [ObservableProperty] private bool isVerified;
    [ObservableProperty] private string statusMessage = string.Empty;

    public MfaSetupViewModel(IMediator mediator, ILogger<MfaSetupViewModel> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        try
        {
            // Check if MFA is already enabled for current user
            // For now, default to not enabled
            IsMfaEnabled = false;
            IsSetupStarted = false;
            IsVerified = false;
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"MFA durumu yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task StartSetupAsync()
    {
        IsLoading = true;
        HasError = false;
        StatusMessage = string.Empty;
        try
        {
            var result = await _mediator.Send(new EnableMfaCommand(Guid.Empty));
            if (result.IsSuccess)
            {
                QrCodeUri = result.QrCodeUri ?? string.Empty;
                SecretKey = result.Secret ?? string.Empty;
                IsSetupStarted = true;
                StatusMessage = "QR kodu Google Authenticator ile tarayin, ardindan 6 haneli kodu girin.";
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "MFA etkinlestirme basarisiz.";
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"MFA baslatilamadi: {ex.Message}";
            _logger.LogError(ex, "MFA setup failed");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task VerifyCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(VerificationCode) || VerificationCode.Length != 6)
        {
            StatusMessage = "Lutfen 6 haneli dogrulama kodunu girin.";
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            var result = await _mediator.Send(new VerifyTotpCommand(Guid.Empty, VerificationCode));
            if (result.IsSuccess)
            {
                IsVerified = true;
                IsMfaEnabled = true;
                StatusMessage = "MFA basariyla etkinlestirildi!";
                _logger.LogInformation("MFA enabled for user");
            }
            else
            {
                StatusMessage = result.ErrorMessage ?? "Kod hatali. Tekrar deneyin.";
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Dogrulama hatasi: {ex.Message}";
            _logger.LogError(ex, "MFA verification failed");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}
