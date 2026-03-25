using FluentAssertions;
using MesTech.Application.Features.Notifications.Queries.GetNotificationSettings;
using MesTech.Application.Features.Onboarding.Commands.CompleteOnboardingStep;
using MesTech.Application.Features.Onboarding.Queries.GetOnboardingProgress;
using MesTech.Application.Features.Settings.Queries.GetCredentialsSettings;
using MesTech.Application.Features.Settings.Queries.GetGeneralSettings;
using MesTech.Application.Features.Settings.Queries.GetProfileSettings;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Onboarding;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class SettingsHandlerTests
{
    // ── GetGeneralSettingsHandler ──

    [Fact]
    public async Task GetGeneralSettings_NullRequest_ThrowsException()
    {
        var repo = new Mock<ITenantRepository>();
        var sut = new GetGeneralSettingsHandler(repo.Object);

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetGeneralSettings_TenantNotFound_ReturnsNull()
    {
        var repo = new Mock<ITenantRepository>();
        var tenantId = Guid.NewGuid();
        repo.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var sut = new GetGeneralSettingsHandler(repo.Object);
        var query = new GetGeneralSettingsQuery(tenantId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetGeneralSettings_TenantFound_ReturnsDto()
    {
        var repo = new Mock<ITenantRepository>();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Name = "TestCo", TaxNumber = "1234567890" };
        repo.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var sut = new GetGeneralSettingsHandler(repo.Object);
        var query = new GetGeneralSettingsQuery(tenantId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.TenantName.Should().Be("TestCo");
    }

    // ── GetCredentialsSettingsHandler ──

    [Fact]
    public async Task GetCredentialsSettings_NullRequest_ThrowsException()
    {
        var repo = new Mock<IStoreRepository>();
        var sut = new GetCredentialsSettingsHandler(repo.Object);

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetCredentialsSettings_ValidRequest_ReturnsPlatformList()
    {
        var repo = new Mock<IStoreRepository>();
        var tenantId = Guid.NewGuid();
        repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>().AsReadOnly());

        var sut = new GetCredentialsSettingsHandler(repo.Object);
        var query = new GetCredentialsSettingsQuery(tenantId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.ConfiguredPlatforms.Should().BeEmpty();
    }

    // ── GetNotificationSettingsHandler ──

    [Fact]
    public async Task GetNotificationSettings_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<INotificationSettingRepository>();
        var sut = new GetNotificationSettingsHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetNotificationSettings_ValidRequest_ReturnsSettingsList()
    {
        var repo = new Mock<INotificationSettingRepository>();
        var userId = Guid.NewGuid();
        repo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationSetting>().AsReadOnly());

        var sut = new GetNotificationSettingsHandler(repo.Object);
        var query = new GetNotificationSettingsQuery(Guid.NewGuid(), userId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
        repo.Verify(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetProfileSettingsHandler ──

    [Fact]
    public async Task GetProfileSettings_NullRequest_ThrowsException()
    {
        var repo = new Mock<ITenantRepository>();
        var sut = new GetProfileSettingsHandler(repo.Object);

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetProfileSettings_TenantNotFound_ReturnsNull()
    {
        var repo = new Mock<ITenantRepository>();
        var tenantId = Guid.NewGuid();
        repo.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var sut = new GetProfileSettingsHandler(repo.Object);
        var query = new GetProfileSettingsQuery(tenantId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProfileSettings_TenantFound_ReturnsProfileDto()
    {
        var repo = new Mock<ITenantRepository>();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Name = "TestCo", TaxNumber = "9876543210" };
        repo.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var sut = new GetProfileSettingsHandler(repo.Object);
        var query = new GetProfileSettingsQuery(tenantId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.TenantName.Should().Be("TestCo");
        result.TaxNumber.Should().Be("9876543210");
    }

    // ── GetOnboardingProgressHandler ──

    [Fact]
    public async Task GetOnboardingProgress_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IOnboardingProgressRepository>();
        var sut = new GetOnboardingProgressHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetOnboardingProgress_NotFound_ReturnsNull()
    {
        var repo = new Mock<IOnboardingProgressRepository>();
        var tenantId = Guid.NewGuid();
        repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingProgress?)null);

        var sut = new GetOnboardingProgressHandler(repo.Object);
        var query = new GetOnboardingProgressQuery(tenantId);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    // ── CompleteOnboardingStepHandler ──

    [Fact]
    public async Task CompleteOnboardingStep_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IOnboardingProgressRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new CompleteOnboardingStepHandler(repo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CompleteOnboardingStep_ProgressNotFound_ThrowsInvalidOperationException()
    {
        var repo = new Mock<IOnboardingProgressRepository>();
        var uow = new Mock<IUnitOfWork>();
        var tenantId = Guid.NewGuid();

        repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingProgress?)null);

        var sut = new CompleteOnboardingStepHandler(repo.Object, uow.Object);
        var command = new CompleteOnboardingStepCommand(tenantId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(command, CancellationToken.None));
    }
}
