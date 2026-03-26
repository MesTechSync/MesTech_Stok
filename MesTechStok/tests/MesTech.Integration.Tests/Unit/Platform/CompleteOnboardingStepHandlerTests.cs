using FluentAssertions;
using MesTech.Application.Features.Onboarding.Commands.CompleteOnboardingStep;
using MesTech.Domain.Entities.Onboarding;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

/// <summary>
/// CompleteOnboardingStepHandler: onboarding adım tamamlama.
/// Kritik: progress bulunamazsa exception fırlatılmalı.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "OnboardingChain")]
public class CompleteOnboardingStepHandlerTests
{
    private readonly Mock<IOnboardingProgressRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public CompleteOnboardingStepHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<OnboardingProgress>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private CompleteOnboardingStepHandler CreateHandler() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Handle_ProgressNotFound_ThrowsInvalidOperation()
    {
        _repo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingProgress?)null);

        var cmd = new CompleteOnboardingStepCommand(Guid.NewGuid());
        var handler = CreateHandler();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidProgress_CompletesStepAndSaves()
    {
        var progress = OnboardingProgress.Start(Guid.NewGuid());
        _repo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);

        var cmd = new CompleteOnboardingStepCommand(progress.TenantId);
        var handler = CreateHandler();

        await handler.Handle(cmd, CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
