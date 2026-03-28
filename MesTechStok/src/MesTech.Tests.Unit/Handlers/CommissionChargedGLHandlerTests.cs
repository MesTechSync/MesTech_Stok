using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests CommissionChargedGLHandler — Zincir 6: Platform komisyon → GL gider kaydi.
/// </summary>
[Trait("Category", "Unit")]
public class CommissionChargedGLHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow;
    private readonly Mock<IJournalEntryRepository> _journalRepo;
    private readonly CommissionChargedGLHandler _sut;

    public CommissionChargedGLHandlerTests()
    {
        _uow = new Mock<IUnitOfWork>();
        _journalRepo = new Mock<IJournalEntryRepository>();
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _sut = new CommissionChargedGLHandler(_uow.Object, _journalRepo.Object, NullLogger<CommissionChargedGLHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_PositiveCommission_CreatesGLEntryAndSaves()
    {
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        await _sut.HandleAsync(orderId, tenantId, PlatformType.Trendyol, 150m, 0.15m, CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task HandleAsync_ZeroCommission_SkipsGLEntry()
    {
        await _sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), PlatformType.Hepsiburada, 0m, 0m, CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_NegativeCommission_SkipsGLEntry()
    {
        await _sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), PlatformType.N11, -10m, 0.05m, CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_DifferentPlatforms_AllComplete()
    {
        var platforms = new[] { PlatformType.Trendyol, PlatformType.Hepsiburada, PlatformType.Amazon, PlatformType.N11 };

        foreach (var platform in platforms)
        {
            var act = () => _sut.HandleAsync(
                Guid.NewGuid(), Guid.NewGuid(), platform, 100m, 0.10m, CancellationToken.None);

            await act.Should().NotThrowAsync($"Platform {platform} should complete without error");
        }
    }
}
