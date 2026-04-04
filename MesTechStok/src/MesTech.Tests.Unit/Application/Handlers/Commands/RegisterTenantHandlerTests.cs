using FluentAssertions;
using MesTech.Application.Features.Onboarding.Commands.RegisterTenant;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Entities.Onboarding;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: RegisterTenantHandler testi — tenant kayıt (SaaS onboarding).
/// P1: Tenant oluşturma = sistemin giriş noktası. Atomik işlem kritik.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class RegisterTenantHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ISubscriptionPlanRepository> _planRepo = new();
    private readonly Mock<ITenantSubscriptionRepository> _subRepo = new();
    private readonly Mock<IOnboardingProgressRepository> _onboardingRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<RegisterTenantHandler>> _logger = new();

    private RegisterTenantHandler CreateSut() =>
        new(_tenantRepo.Object, _userRepo.Object, _planRepo.Object,
            _subRepo.Object, _onboardingRepo.Object, _uow.Object, _logger.Object);

    private void SetupDefaultPlans()
    {
        _planRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubscriptionPlan>
            {
                SubscriptionPlan.Create("Starter", 99m, 990m, 3, 500, 5)
            });
    }

    [Fact]
    public async Task Handle_DuplicateUsername_ShouldThrow()
    {
        _userRepo.Setup(r => r.GetByUsernameAsync("existing"))
            .ReturnsAsync(new User { Username = "existing" });

        var cmd = new RegisterTenantCommand("Test Co", null, "existing", "test@test.com", "Pass123!");

        var act = () => CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*zaten*");
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldReturnAllIds()
    {
        _userRepo.Setup(r => r.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        SetupDefaultPlans();

        var cmd = new RegisterTenantCommand(
            "MesTech Ltd", "1234567890", "admin", "admin@mestech.com", "Pass123!",
            "Ali", "Veli");

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.TenantId.Should().NotBeEmpty();
        result.AdminUserId.Should().NotBeEmpty();
        result.SubscriptionId.Should().NotBeEmpty();
        result.OnboardingId.Should().NotBeEmpty();
        result.PlanName.Should().Be("Starter");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoActivePlan_ShouldThrow()
    {
        _userRepo.Setup(r => r.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _planRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubscriptionPlan>());

        var cmd = new RegisterTenantCommand("Test", null, "admin", "a@b.com", "Pass123!");

        var act = () => CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*plan bulunamadı*");
    }
}
