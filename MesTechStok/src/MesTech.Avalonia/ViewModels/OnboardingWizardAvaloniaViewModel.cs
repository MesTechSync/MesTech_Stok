using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Onboarding.Commands.CompleteOnboardingStep;
using MesTech.Application.Features.Onboarding.Commands.RegisterTenant;
using MesTech.Application.Features.Onboarding.Queries.GetOnboardingProgress;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Onboarding wizard ViewModel — 7 adımlı ilk kurulum.
/// 1.Kayıt → 2.Firma → 3.Mağaza → 4.Platform → 5.Credential → 6.Sync → 7.Dashboard
/// </summary>
public partial class OnboardingWizardAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    // Step 1: Kayıt bilgileri
    [ObservableProperty] private string companyName = string.Empty;
    [ObservableProperty] private string? taxNumber;
    [ObservableProperty] private string adminUsername = string.Empty;
    [ObservableProperty] private string adminEmail = string.Empty;
    [ObservableProperty] private string adminPassword = string.Empty;
    [ObservableProperty] private string? adminFirstName;
    [ObservableProperty] private string? adminLastName;

    // Step 3: Mağaza bilgileri
    [ObservableProperty] private string storeName = string.Empty;
    [ObservableProperty] private string selectedPlatform = "Trendyol";

    // Wizard state
    [ObservableProperty] private int currentStep = 1;
    [ObservableProperty] private int totalSteps = 7;
    [ObservableProperty] private int completionPercent;
    [ObservableProperty] private string stepTitle = "Firma Kaydı";
    [ObservableProperty] private bool isRegistered;
    [ObservableProperty] private string? registrationMessage;
    [ObservableProperty] private Guid tenantId;
    [ObservableProperty] private DateTime? trialEndsAt;
    [ObservableProperty] private string planName = string.Empty;

    public string[] AvailablePlatforms { get; } =
        ["Trendyol", "Hepsiburada", "N11", "Çiçeksepeti", "Amazon", "eBay", "Shopify", "WooCommerce"];

    public OnboardingWizardAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;

        try
        {
            if (TenantId != Guid.Empty)
            {
                var progress = await _mediator.Send(new GetOnboardingProgressQuery(TenantId));
                if (progress is not null)
                {
                    CurrentStep = (int)progress.CurrentStep;
                    CompletionPercent = progress.CompletionPercentage;
                    IsRegistered = true;
                    UpdateStepTitle();
                }
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(CompanyName) || string.IsNullOrWhiteSpace(AdminUsername)
            || string.IsNullOrWhiteSpace(AdminEmail) || string.IsNullOrWhiteSpace(AdminPassword))
        {
            ErrorMessage = "Tüm alanları doldurun.";
            HasError = true;
            return;
        }

        IsLoading = true;
        HasError = false;

        try
        {
            var result = await _mediator.Send(new RegisterTenantCommand(
                CompanyName, TaxNumber,
                AdminUsername, AdminEmail, AdminPassword,
                AdminFirstName, AdminLastName));

            TenantId = result.TenantId;
            TrialEndsAt = result.TrialEndsAt;
            PlanName = result.PlanName;
            IsRegistered = true;
            RegistrationMessage = $"Kayıt başarılı! {PlanName} planı, {TrialEndsAt:dd MMM yyyy} tarihine kadar ücretsiz.";

            await NextStepAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Kayıt başarısız: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NextStepAsync()
    {
        if (CurrentStep >= TotalSteps) return;

        IsLoading = true;
        HasError = false;

        try
        {
            await _mediator.Send(new CompleteOnboardingStepCommand(TenantId));
            CurrentStep++;
            CompletionPercent = CurrentStep * 100 / TotalSteps;
            UpdateStepTitle();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
            UpdateStepTitle();
        }
    }

    private void UpdateStepTitle()
    {
        StepTitle = CurrentStep switch
        {
            1 => "Firma Kaydı",
            2 => "Firma Bilgileri",
            3 => "İlk Mağaza Ekle",
            4 => "Platform Seç",
            5 => "API Bilgileri",
            6 => "İlk Senkronizasyon",
            7 => "Dashboard'a Git",
            _ => "Tamamlandı"
        };
    }
}
