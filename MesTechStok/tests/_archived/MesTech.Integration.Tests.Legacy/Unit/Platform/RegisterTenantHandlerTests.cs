using FluentAssertions;
using MesTech.Application.Features.Onboarding.Commands.RegisterTenant;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Entities.Onboarding;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

/// <summary>
/// RegisterTenantHandler: SaaS onboarding — 6 adımlı atomik kayıt.
/// G022: tenant+user+subscription+onboarding tek transaction'da.
/// Kritik iş kuralları:
///   - Username benzersizliği kontrol
///   - BCrypt ile şifre hashleme
///   - En ucuz plan ile trial başlatma
///   - Aktif plan yoksa exception
///   - Atomik SaveChanges
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "OnboardingChain")]
public class RegisterTenantHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ISubscriptionPlanRepository> _planRepo = new();
    private readonly Mock<ITenantSubscriptionRepository> _subRepo = new();
    private readonly Mock<IOnboardingProgressRepository> _onboardingRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<RegisterTenantHandler>> _logger = new();

    public RegisterTenantHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _tenantRepo.Setup(r => r.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _subRepo.Setup(r => r.AddAsync(It.IsAny<TenantSubscription>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _onboardingRepo.Setup(r => r.AddAsync(It.IsAny<OnboardingProgress>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Default: username benzersiz
        _userRepo.Setup(r => r.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        // Default: aktif plan var
        var starterPlan = SubscriptionPlan.Create("Başlangıç", 299m, 2990m, 1, 500, 1);
        _planRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubscriptionPlan> { starterPlan });
    }

    private RegisterTenantHandler CreateHandler() =>
        new(_tenantRepo.Object, _userRepo.Object, _planRepo.Object,
            _subRepo.Object, _onboardingRepo.Object, _uow.Object, _logger.Object);

    private static RegisterTenantCommand ValidCommand() => new(
        CompanyName: "Test Firma A.Ş.",
        TaxNumber: "1234567890",
        AdminUsername: "admin",
        AdminEmail: "admin@testfirma.com",
        AdminPassword: "Secure123!",
        AdminFirstName: "Admin",
        AdminLastName: "User");

    [Fact]
    public async Task Handle_ValidCommand_CreatesAllEntitiesAtomically()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        // Assert — tüm entity'ler oluşturulmalı
        result.TenantId.Should().NotBeEmpty();
        result.AdminUserId.Should().NotBeEmpty();
        result.SubscriptionId.Should().NotBeEmpty();
        result.OnboardingId.Should().NotBeEmpty();
        result.PlanName.Should().Be("Başlangıç");
        result.TrialEndsAt.Should().BeAfter(DateTime.UtcNow);

        // 4 repo AddAsync + 1 SaveChanges
        _tenantRepo.Verify(r => r.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Once);
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _subRepo.Verify(r => r.AddAsync(It.IsAny<TenantSubscription>(), It.IsAny<CancellationToken>()), Times.Once);
        _onboardingRepo.Verify(r => r.AddAsync(It.IsAny<OnboardingProgress>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateUsername_ThrowsInvalidOperation()
    {
        // Arrange — username zaten var
        _userRepo.Setup(r => r.GetByUsernameAsync("admin"))
            .ReturnsAsync(new User { Username = "admin" });

        var handler = CreateHandler();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(ValidCommand(), CancellationToken.None));

        // Tenant oluşturulMAMALI
        _tenantRepo.Verify(r => r.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoActivePlan_ThrowsInvalidOperation()
    {
        // Arrange — aktif plan yok (seed data eksik)
        _planRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubscriptionPlan>());

        var handler = CreateHandler();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(ValidCommand(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PasswordIsHashed_NotPlaintext()
    {
        User? capturedUser = null;
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        await handler.Handle(ValidCommand(), CancellationToken.None);

        // Assert — şifre BCrypt hash olmalı, plain text DEĞİL
        capturedUser.Should().NotBeNull();
        capturedUser!.PasswordHash.Should().NotBe("Secure123!");
        capturedUser.PasswordHash.Should().StartWith("$2"); // BCrypt prefix
    }

    [Fact]
    public async Task Handle_SelectsCheapestPlan()
    {
        // Arrange — birden fazla plan var
        var starter = SubscriptionPlan.Create("Başlangıç", 299m, 2990m, 1, 500, 1);
        var pro = SubscriptionPlan.Create("Profesyonel", 799m, 7990m, 5, 10000, 5);
        var enterprise = SubscriptionPlan.Create("Kurumsal", 1999m, 19990m, 100, 100000, 100);

        _planRepo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubscriptionPlan> { pro, enterprise, starter }); // karışık sıra

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        // En ucuz plan seçilmeli (299 TL/ay)
        result.PlanName.Should().Be("Başlangıç");
    }

    [Fact]
    public async Task Handle_TenantIsActive()
    {
        Tenant? capturedTenant = null;
        _tenantRepo.Setup(r => r.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .Callback<Tenant, CancellationToken>((t, _) => capturedTenant = t)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        await handler.Handle(ValidCommand(), CancellationToken.None);

        capturedTenant.Should().NotBeNull();
        capturedTenant!.IsActive.Should().BeTrue();
        capturedTenant.Name.Should().Be("Test Firma A.Ş.");
    }
}
