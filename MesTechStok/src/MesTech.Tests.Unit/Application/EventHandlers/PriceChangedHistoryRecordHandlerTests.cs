using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using IUnitOfWork = MesTech.Domain.Interfaces.IUnitOfWork;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.EventHandlers;

/// <summary>
/// PriceChangedHistoryRecordHandler — fiyat değişim kayıt testi.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class PriceChangedHistoryRecordHandlerTests
{
    private readonly Mock<IPriceHistoryRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public PriceChangedHistoryRecordHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private PriceChangedHistoryRecordHandler CreateSut() => new(
        _repo.Object, _uow.Object, Mock.Of<ILogger<PriceChangedHistoryRecordHandler>>());

    [Fact]
    public async Task Handle_ShouldCreatePriceHistoryRecord()
    {
        PriceHistory? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<PriceHistory>(), It.IsAny<CancellationToken>()))
            .Callback<PriceHistory, CancellationToken>((ph, _) => captured = ph)
            .Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(Guid.NewGuid(), TenantId, "SKU-PRICE",
            100m, 150m, "admin", "Kampanya", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.OldPrice.Should().Be(100m);
        captured.NewPrice.Should().Be(150m);
        captured.ChangedBy.Should().Be("admin");
        captured.ChangeReason.Should().Be("Kampanya");
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChanges()
    {
        _repo.Setup(r => r.AddAsync(It.IsAny<PriceHistory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(Guid.NewGuid(), TenantId, "SKU-SAVE",
            200m, 180m, null, null, CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullChangedBy_ShouldUseSystemDefault()
    {
        PriceHistory? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<PriceHistory>(), It.IsAny<CancellationToken>()))
            .Callback<PriceHistory, CancellationToken>((ph, _) => captured = ph)
            .Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(Guid.NewGuid(), TenantId, "SKU-SYS",
            100m, 200m, null, null, CancellationToken.None);

        captured!.ChangedBy.Should().NotBeNullOrEmpty("should default to system user");
    }

    [Fact]
    public async Task Handle_SamePriceChange_ShouldStillRecord()
    {
        _repo.Setup(r => r.AddAsync(It.IsAny<PriceHistory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Fiyat değişmese bile kayıt olmalı (audit trail)
        await CreateSut().HandleAsync(Guid.NewGuid(), TenantId, "SKU-SAME",
            100m, 100m, "admin", "Manual", CancellationToken.None);

        _repo.Verify(r => r.AddAsync(It.IsAny<PriceHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SetsTenantId()
    {
        PriceHistory? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<PriceHistory>(), It.IsAny<CancellationToken>()))
            .Callback<PriceHistory, CancellationToken>((ph, _) => captured = ph)
            .Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(Guid.NewGuid(), TenantId, "SKU-TEN",
            50m, 75m, "user", "Zam", CancellationToken.None);

        captured!.TenantId.Should().Be(TenantId);
    }
}
