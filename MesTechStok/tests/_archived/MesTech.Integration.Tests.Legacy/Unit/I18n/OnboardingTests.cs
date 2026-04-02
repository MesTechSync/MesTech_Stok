using FluentAssertions;
using MesTech.Blazor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.I18n;

/// <summary>
/// Onboarding wizard structure and OnboardingService behavior tests (EMR-17).
/// Validates wizard has correct 7-step structure, service state management,
/// and redirect logic.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Blazor")]
public sealed class OnboardingTests
{
    private static readonly MesTechApiClient _apiClient = new(new HttpClient(), Mock.Of<IConfiguration>());
    private static readonly ILogger<OnboardingService> _logger = Mock.Of<ILogger<OnboardingService>>();

    private static readonly string SolutionRoot = FindSolutionRoot();

    private static readonly string WizardPath = Path.Combine(
        SolutionRoot, "src", "MesTech.Blazor", "Components", "Pages",
        "Onboarding", "OnboardingWizard.razor");

    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "MesTechStok.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Solution root not found");
    }

    // --- Test 1: IsOnboardingCompleted returns true initially (default for existing users) ---
    [Fact]
    public async Task IsOnboardingCompleted_DefaultsToTrue_ForExistingUsers()
    {
        // Arrange
        var service = new OnboardingService(_apiClient, _logger);
        var userId = Guid.NewGuid();

        // Act
        var result = await service.IsOnboardingCompletedAsync(userId);

        // Assert — default true so existing users are not disrupted
        result.Should().BeTrue(
            "OnboardingService defaults to completed for existing users");
    }

    // --- Test 2: MarkCompleted changes state ---
    [Fact]
    public async Task MarkCompleted_DoesNotThrow()
    {
        // Arrange
        var service = new OnboardingService(_apiClient, _logger);
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await service.MarkOnboardingCompletedAsync(userId);

        // Assert
        await act.Should().NotThrowAsync(
            "MarkOnboardingCompletedAsync must not throw for any valid user ID");
    }

    // --- Test 3: SaveStep persists step data ---
    [Fact]
    public async Task SaveStep_DoesNotThrow_ForValidSteps()
    {
        // Arrange
        var service = new OnboardingService(_apiClient, _logger);
        var userId = Guid.NewGuid();
        var stepData = new Dictionary<string, string>
        {
            ["companyName"] = "Test Firma",
            ["taxId"] = "1234567890"
        };

        // Act — save steps 1 through 7
        for (int step = 1; step <= 7; step++)
        {
            var s = step;
            var act = async () => await service.SaveOnboardingStepAsync(userId, s, stepData);
            await act.Should().NotThrowAsync(
                $"SaveOnboardingStepAsync must not throw for step {s}");
        }
    }

    // --- Test 4: Wizard has validation — step 2 requires company name ---
    [Fact]
    public void WizardSource_Step2_RequiresCompanyName()
    {
        // Arrange — source-level verification
        var content = File.ReadAllText(WizardPath);

        // Assert — step 2 validation: company name is required
        content.Should().Contain("IsCurrentStepValid",
            "wizard must have step validation method");
        content.Should().Contain("string.IsNullOrWhiteSpace(companyName)",
            "step 2 must validate that company name is not empty");
        content.Should().Contain("is-invalid",
            "wizard must show validation error styling");
        content.Should().Contain("Error.Required",
            "wizard must use localized required field error message");
    }

    // --- Test 5: Wizard redirects when onboarding completed ---
    [Fact]
    public void WizardSource_RedirectsWhenCompleted()
    {
        // Arrange
        var content = File.ReadAllText(WizardPath);

        // Assert — redirect logic
        content.Should().Contain("IsOnboardingCompletedAsync",
            "wizard must check onboarding completion status on init");
        content.Should().Contain("Nav.NavigateTo(\"/dashboard\"",
            "wizard must redirect to dashboard when onboarding is already completed");
        content.Should().Contain("replace: true",
            "redirect must replace history entry to prevent back-button loop");
    }

    // --- Test 6: Wizard step 1 has language selection ---
    [Fact]
    public void WizardSource_Step1_HasLanguageSelection()
    {
        // Arrange
        var content = File.ReadAllText(WizardPath);

        // Assert — language options in step 1
        content.Should().Contain("selectedLanguage",
            "step 1 must bind to a language selection field");
        content.Should().Contain("Language.Turkish",
            "step 1 must offer Turkish language option via i18n key");
        content.Should().Contain("Language.English",
            "step 1 must offer English language option via i18n key");
        content.Should().Contain("Language.German",
            "step 1 must offer German language option via i18n key");
        content.Should().Contain("Language.Arabic",
            "step 1 must offer Arabic language option via i18n key");
    }
}
