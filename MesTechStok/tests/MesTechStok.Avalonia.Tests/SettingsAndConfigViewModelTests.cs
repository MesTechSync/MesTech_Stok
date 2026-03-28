using FluentAssertions;
using MesTech.Avalonia.Services;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

// ════════════════════════════════════════════════════════
// DEV5 TUR 11: Settings & Config ViewModel tests (G050)
// Coverage: Settings, OnboardingWizard, MfaSetup
// ════════════════════════════════════════════════════════

#region SettingsAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class SettingsAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeDefaults()
    {
        var sut = new SettingsAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IThemeService>());

        sut.AppVersion.Should().Contain("MesTech");
        sut.ApiUrl.Should().NotBeNullOrWhiteSpace();
        sut.PlatformCredentials.Should().NotBeNull();
    }

    [Fact]
    public async Task LoadAsync_ShouldNotThrow()
    {
        var sut = new SettingsAvaloniaViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IThemeService>());

        var act = async () => await sut.LoadAsync();

        await act.Should().NotThrowAsync();
    }
}

#endregion

#region OnboardingWizardAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class OnboardingWizardAvaloniaViewModelTests
{
    [Fact]
    public void Constructor_ShouldStartAtStep1()
    {
        var sut = new OnboardingWizardAvaloniaViewModel(Mock.Of<IMediator>());

        sut.CurrentStep.Should().Be(1);
        sut.TotalSteps.Should().Be(7);
        sut.StepTitle.Should().Be("Firma Kaydı");
    }

    [Fact]
    public void Constructor_ShouldHavePlatformOptions()
    {
        var sut = new OnboardingWizardAvaloniaViewModel(Mock.Of<IMediator>());

        sut.AvailablePlatforms.Should().Contain("Trendyol");
        sut.AvailablePlatforms.Should().Contain("Hepsiburada");
        sut.AvailablePlatforms.Should().HaveCountGreaterThanOrEqualTo(5);
    }

    [Fact]
    public void SelectedPlatform_ShouldDefaultToTrendyol()
    {
        var sut = new OnboardingWizardAvaloniaViewModel(Mock.Of<IMediator>());

        sut.SelectedPlatform.Should().Be("Trendyol");
    }
}

#endregion

#region MfaSetupViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class MfaSetupViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithMfaDisabled()
    {
        var sut = new MfaSetupViewModel(
            Mock.Of<IMediator>(),
            Mock.Of<Microsoft.Extensions.Logging.ILogger<MfaSetupViewModel>>());

        sut.IsMfaEnabled.Should().BeFalse();
        sut.IsSetupStarted.Should().BeFalse();
        sut.IsVerified.Should().BeFalse();
        sut.QrCodeUri.Should().BeEmpty();
        sut.SecretKey.Should().BeEmpty();
    }
}

#endregion
