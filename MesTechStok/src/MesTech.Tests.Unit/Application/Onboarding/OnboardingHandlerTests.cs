using FluentAssertions;
using MesTech.Application.Features.Onboarding.Commands.CompleteOnboardingStep;
using MesTech.Application.Features.Onboarding.Commands.StartOnboarding;
using MesTech.Application.Features.Onboarding.Queries.GetOnboardingProgress;
using MesTech.Domain.Entities.Onboarding;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Onboarding;

[Trait("Category", "Unit")]
[Trait("Feature", "Onboarding")]
public class OnboardingHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    // ── StartOnboardingHandler ──

    [Fact]
    public async Task StartOnboarding_NewTenant_ShouldCreateAndReturnId()
    {
        // Arrange
        var mockRepo = new Mock<IOnboardingProgressRepository>();
        mockRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingProgress?)null);
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new StartOnboardingHandler(mockRepo.Object, mockUow.Object);

        // Act
        var result = await handler.Handle(new StartOnboardingCommand(_tenantId), CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        mockRepo.Verify(r => r.AddAsync(It.IsAny<OnboardingProgress>(), It.IsAny<CancellationToken>()), Times.Once);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartOnboarding_ExistingTenant_ShouldThrow()
    {
        // Arrange
        var existing = OnboardingProgress.Start(_tenantId);
        var mockRepo = new Mock<IOnboardingProgressRepository>();
        mockRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = new StartOnboardingHandler(mockRepo.Object, Mock.Of<IUnitOfWork>());

        // Act
        var act = () => handler.Handle(new StartOnboardingCommand(_tenantId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*zaten baslamis*");
    }

    [Fact]
    public async Task StartOnboarding_NullRequest_ShouldThrow()
    {
        var handler = new StartOnboardingHandler(
            Mock.Of<IOnboardingProgressRepository>(), Mock.Of<IUnitOfWork>());
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── CompleteOnboardingStepHandler ──

    [Fact]
    public async Task CompleteStep_ValidProgress_ShouldAdvanceStep()
    {
        // Arrange
        var progress = OnboardingProgress.Start(_tenantId);
        var initialStep = progress.CurrentStep;
        var mockRepo = new Mock<IOnboardingProgressRepository>();
        mockRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new CompleteOnboardingStepHandler(mockRepo.Object, mockUow.Object);

        // Act
        await handler.Handle(new CompleteOnboardingStepCommand(_tenantId), CancellationToken.None);

        // Assert
        progress.CurrentStep.Should().NotBe(initialStep);
        progress.CurrentStep.Should().Be(OnboardingStep.CompanyInfo);
        mockRepo.Verify(r => r.UpdateAsync(progress, It.IsAny<CancellationToken>()), Times.Once);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteStep_ProgressNotFound_ShouldThrow()
    {
        // Arrange
        var mockRepo = new Mock<IOnboardingProgressRepository>();
        mockRepo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingProgress?)null);

        var handler = new CompleteOnboardingStepHandler(mockRepo.Object, Mock.Of<IUnitOfWork>());

        // Act
        var act = () => handler.Handle(new CompleteOnboardingStepCommand(_tenantId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Onboarding bulunamadi*");
    }

    [Fact]
    public async Task CompleteStep_NullRequest_ShouldThrow()
    {
        var handler = new CompleteOnboardingStepHandler(
            Mock.Of<IOnboardingProgressRepository>(), Mock.Of<IUnitOfWork>());
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── GetOnboardingProgressHandler ──

    [Fact]
    public async Task GetProgress_Exists_ShouldReturnDto()
    {
        // Arrange
        var progress = OnboardingProgress.Start(_tenantId);
        var mockRepo = new Mock<IOnboardingProgressRepository>();
        mockRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);

        var handler = new GetOnboardingProgressHandler(mockRepo.Object);

        // Act
        var result = await handler.Handle(
            new GetOnboardingProgressQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.CurrentStep.Should().Be(OnboardingStep.Registration);
        result.IsCompleted.Should().BeFalse();
        result.CompletionPercentage.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetProgress_NotExists_ShouldReturnNull()
    {
        // Arrange
        var mockRepo = new Mock<IOnboardingProgressRepository>();
        mockRepo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingProgress?)null);

        var handler = new GetOnboardingProgressHandler(mockRepo.Object);

        // Act
        var result = await handler.Handle(
            new GetOnboardingProgressQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProgress_NullRequest_ShouldThrow()
    {
        var handler = new GetOnboardingProgressHandler(Mock.Of<IOnboardingProgressRepository>());
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
