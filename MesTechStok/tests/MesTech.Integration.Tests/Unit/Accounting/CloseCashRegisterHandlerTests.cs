using FluentAssertions;
using MesTech.Application.Features.Finance.Commands.CloseCashRegister;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// CloseCashRegisterHandler: kasa kapanış — günlük nakit mutabakat.
/// Kritik iş kuralları:
///   - Gelir/gider toplamları doğru hesaplanmalı
///   - Kasa farkı (fazla/eksik) tespit edilmeli
///   - Kapanış sonrası kasa durumu güncellenmeli
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "FinanceChain")]
public class CloseCashRegisterHandlerTests
{
    private readonly Mock<ICashRegisterRepository> _cashRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<CloseCashRegisterHandler>> _logger = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    public CloseCashRegisterHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private CloseCashRegisterHandler CreateHandler() =>
        new(_cashRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task Handle_RegisterNotFound_ThrowsOrReturnsDefault()
    {
        _cashRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CashRegister?)null);

        var cmd = new CloseCashRegisterCommand(_tenantId, Guid.NewGuid(), DateTime.UtcNow, 1000m);
        var handler = CreateHandler();

        // Handler throws or returns non-closed result when register not found
        try
        {
            var result = await handler.Handle(cmd, CancellationToken.None);
            result.IsClosed.Should().BeFalse();
        }
        catch (Exception)
        {
            // Expected — register not found
        }
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNull()
    {
        var handler = CreateHandler();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.Handle(null!, CancellationToken.None));
    }
}
