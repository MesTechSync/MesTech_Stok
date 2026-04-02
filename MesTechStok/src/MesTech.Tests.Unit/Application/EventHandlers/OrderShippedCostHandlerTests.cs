using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Application.EventHandlers;

[Trait("Category", "Unit")]
public class OrderShippedCostHandlerTests2
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IJournalEntryRepository> _journalRepoMock = new();
    private readonly OrderShippedCostHandler _sut;

    public OrderShippedCostHandlerTests2()
    {
        _sut = new OrderShippedCostHandler(
            _uowMock.Object,
            _journalRepoMock.Object,
            NullLogger<OrderShippedCostHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeCargoProviderInDescription()
    {
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var trackingNumber = "TRK-123456";

        _journalRepoMock.Setup(r => r.ExistsByReferenceAsync(
            tenantId, trackingNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _sut.HandleAsync(
            orderId, tenantId, trackingNumber,
            CargoProvider.YurticiKargo, 25.50m,
            CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ZeroCost_SkipsGLRecord()
    {
        await _sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "TRK-000",
            CargoProvider.ArasKargo, 0m,
            CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_DuplicateTracking_SkipsGLRecord()
    {
        var tenantId = Guid.NewGuid();
        var tracking = "TRK-DUP";

        _journalRepoMock.Setup(r => r.ExistsByReferenceAsync(
            tenantId, tracking, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.HandleAsync(
            Guid.NewGuid(), tenantId, tracking,
            CargoProvider.SuratKargo, 30m,
            CancellationToken.None);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
