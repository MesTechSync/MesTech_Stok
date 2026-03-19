using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Magaza Ekleme Sihirbazi — 3 adimli wizard: Platform Sec → Kimlik Bilgileri → Test + Kaydet.
/// TODO: Replace with MediatR commands when A1 CQRS is ready.
/// </summary>
public partial class StoreWizardAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    // Wizard state
    [ObservableProperty] private int currentStep = 1;
    [ObservableProperty] private string selectedPlatform = string.Empty;
    [ObservableProperty] private bool isStep1Visible = true;
    [ObservableProperty] private bool isStep2Visible;
    [ObservableProperty] private bool isStep3Visible;

    // Step 2: Credential fields
    [ObservableProperty] private string storeName = string.Empty;
    [ObservableProperty] private string field1Value = string.Empty;
    [ObservableProperty] private string field2Value = string.Empty;
    [ObservableProperty] private string field3Value = string.Empty;
    [ObservableProperty] private string field1Label = "API Key";
    [ObservableProperty] private string field2Label = "API Secret";
    [ObservableProperty] private string field3Label = "Seller ID";
    [ObservableProperty] private bool field3Visible = true;

    // Step 3: Test results
    [ObservableProperty] private bool isTesting;
    [ObservableProperty] private bool testPassed;
    [ObservableProperty] private bool testFailed;
    [ObservableProperty] private string testResultMessage = string.Empty;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private bool saveCompleted;

    public ObservableCollection<WizardPlatformDto> AvailablePlatforms { get; } = [];

    public StoreWizardAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
        InitPlatforms();
    }

    private void InitPlatforms()
    {
        AvailablePlatforms.Add(new WizardPlatformDto { Name = "Trendyol", Color = "#FF6F00" });
        AvailablePlatforms.Add(new WizardPlatformDto { Name = "Hepsiburada", Color = "#FF6000" });
        AvailablePlatforms.Add(new WizardPlatformDto { Name = "N11", Color = "#0B2441" });
        AvailablePlatforms.Add(new WizardPlatformDto { Name = "Ciceksepeti", Color = "#F27A1A" });
        AvailablePlatforms.Add(new WizardPlatformDto { Name = "Amazon", Color = "#FF9900" });
        AvailablePlatforms.Add(new WizardPlatformDto { Name = "eBay", Color = "#E53238" });
        AvailablePlatforms.Add(new WizardPlatformDto { Name = "Shopify", Color = "#96BF48" });
        AvailablePlatforms.Add(new WizardPlatformDto { Name = "WooCommerce", Color = "#96588A" });
        AvailablePlatforms.Add(new WizardPlatformDto { Name = "Pazarama", Color = "#00B8D4" });
        AvailablePlatforms.Add(new WizardPlatformDto { Name = "PttAVM", Color = "#FFD600" });
        AvailablePlatforms.Add(new WizardPlatformDto { Name = "OpenCart", Color = "#23A8E0" });
        AvailablePlatforms.Add(new WizardPlatformDto { Name = "Ozon", Color = "#005BFF" });
        AvailablePlatforms.Add(new WizardPlatformDto { Name = "Etsy", Color = "#F1641E" });
    }

    [RelayCommand]
    private void SelectPlatform(WizardPlatformDto? platform)
    {
        if (platform is null) return;
        SelectedPlatform = platform.Name;

        // Configure credential fields per platform
        switch (platform.Name)
        {
            case "Trendyol":
                Field1Label = "API Key"; Field2Label = "API Secret"; Field3Label = "Seller ID"; Field3Visible = true;
                break;
            case "Hepsiburada":
                Field1Label = "Merchant ID"; Field2Label = "API Key"; Field3Label = "API Secret"; Field3Visible = true;
                break;
            case "N11":
                Field1Label = "API Key"; Field2Label = "API Secret"; Field3Label = ""; Field3Visible = false;
                break;
            case "Ciceksepeti":
                Field1Label = "API Key"; Field2Label = "API Secret"; Field3Label = ""; Field3Visible = false;
                break;
            case "Amazon":
                Field1Label = "Access Key"; Field2Label = "Secret Key"; Field3Label = "Marketplace ID"; Field3Visible = true;
                break;
            case "eBay":
                Field1Label = "App ID"; Field2Label = "Cert ID"; Field3Label = "Dev ID"; Field3Visible = true;
                break;
            case "Shopify":
                Field1Label = "Store URL"; Field2Label = "Access Token"; Field3Label = ""; Field3Visible = false;
                break;
            case "WooCommerce":
                Field1Label = "Store URL"; Field2Label = "Consumer Key"; Field3Label = "Consumer Secret"; Field3Visible = true;
                break;
            case "OpenCart":
                Field1Label = "Store URL"; Field2Label = "API Token"; Field3Label = ""; Field3Visible = false;
                break;
            default:
                Field1Label = "API Key"; Field2Label = "API Secret"; Field3Label = ""; Field3Visible = false;
                break;
        }

        GoToStep(2);
    }

    [RelayCommand]
    private void GoBack()
    {
        if (CurrentStep > 1)
            GoToStep(CurrentStep - 1);
    }

    [RelayCommand]
    private void GoNext()
    {
        if (CurrentStep < 3)
            GoToStep(CurrentStep + 1);
    }

    private void GoToStep(int step)
    {
        CurrentStep = step;
        IsStep1Visible = step == 1;
        IsStep2Visible = step == 2;
        IsStep3Visible = step == 3;
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        TestPassed = false;
        TestFailed = false;
        TestResultMessage = string.Empty;
        try
        {
            // TODO: Replace with MediatR.Send(new TestStoreConnectionCommand()) when A1 CQRS is ready
            await Task.Delay(1500);
            TestPassed = true;
            TestResultMessage = $"{SelectedPlatform} baglantisi basarili!";
        }
        catch (Exception ex)
        {
            TestFailed = true;
            TestResultMessage = $"Baglanti hatasi: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private async Task SaveStoreAsync()
    {
        IsSaving = true;
        HasError = false;
        try
        {
            // TODO: Replace with MediatR.Send(new CreateStoreCommand()) when A1 CQRS is ready
            await Task.Delay(800);
            SaveCompleted = true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Magaza kaydedilemedi: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void Reset()
    {
        GoToStep(1);
        SelectedPlatform = string.Empty;
        StoreName = string.Empty;
        Field1Value = string.Empty;
        Field2Value = string.Empty;
        Field3Value = string.Empty;
        TestPassed = false;
        TestFailed = false;
        TestResultMessage = string.Empty;
        SaveCompleted = false;
    }
}

public class WizardPlatformDto
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#0078D4";
}
