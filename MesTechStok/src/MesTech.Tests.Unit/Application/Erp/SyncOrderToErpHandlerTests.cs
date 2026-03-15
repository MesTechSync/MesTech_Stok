using FluentAssertions;
using MesTech.Application.DTOs.ERP;
using MesTech.Application.Features.Erp.Commands.SyncOrderToErp;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Erp;

/// <summary>
/// SyncOrderToErpHandler unit tests — Dalga 12 final test coverage.
/// 4 tests covering success, adapter failure, unknown provider, and ERP reference return.
/// </summary>
[Trait("Category", "Unit")]
public class SyncOrderToErpHandlerTests
{
    private readonly Mock<IErpAdapterFactory> _adapterFactory = new();
    private readonly Mock<IErpSyncLogRepository> _syncLogRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private SyncOrderToErpHandler CreateHandler() =>
        new(_adapterFactory.Object, _syncLogRepo.Object, _uow.Object);

    private static SyncOrderToErpCommand CreateCommand(
        ErpProvider provider = ErpProvider.Parasut) =>
        new(
            TenantId: Guid.NewGuid(),
            OrderId: Guid.NewGuid(),
            Provider: provider);

    [Fact]
    public async Task SyncOrder_Success_ShouldCreateSuccessLog()
    {
        // Arrange
        var command = CreateCommand();
        var expectedRef = "ERP-REF-12345";
        var mockAdapter = new Mock<IErpAdapter>();
        mockAdapter
            .Setup(a => a.SyncOrderAsync(command.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErpSyncResult.Ok(expectedRef));

        _adapterFactory
            .Setup(f => f.GetAdapter(command.Provider))
            .Returns(mockAdapter.Object);

        ErpSyncLog? capturedLog = null;
        _syncLogRepo
            .Setup(r => r.UpdateAsync(It.IsAny<ErpSyncLog>(), It.IsAny<CancellationToken>()))
            .Callback<ErpSyncLog, CancellationToken>((log, _) => capturedLog = log);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _syncLogRepo.Verify(
            r => r.AddAsync(It.IsAny<ErpSyncLog>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _syncLogRepo.Verify(
            r => r.UpdateAsync(It.IsAny<ErpSyncLog>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(2)); // Once for initial log, once for update

        capturedLog.Should().NotBeNull();
        capturedLog!.Success.Should().BeTrue();
        capturedLog.ErpRef.Should().Be(expectedRef);
    }

    [Fact]
    public async Task SyncOrder_AdapterFails_ShouldCreateFailureLog()
    {
        // Arrange
        var command = CreateCommand(ErpProvider.Logo);
        var errorMessage = "Logo API baglanti hatasi";
        var mockAdapter = new Mock<IErpAdapter>();
        mockAdapter
            .Setup(a => a.SyncOrderAsync(command.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErpSyncResult.Fail(errorMessage));

        _adapterFactory
            .Setup(f => f.GetAdapter(command.Provider))
            .Returns(mockAdapter.Object);

        ErpSyncLog? capturedLog = null;
        _syncLogRepo
            .Setup(r => r.UpdateAsync(It.IsAny<ErpSyncLog>(), It.IsAny<CancellationToken>()))
            .Callback<ErpSyncLog, CancellationToken>((log, _) => capturedLog = log);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be(errorMessage);

        capturedLog.Should().NotBeNull();
        capturedLog!.Success.Should().BeFalse();
        capturedLog.ErrorMessage.Should().Be(errorMessage);

        _syncLogRepo.Verify(
            r => r.UpdateAsync(It.IsAny<ErpSyncLog>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SyncOrder_UnknownProvider_ShouldThrow()
    {
        // Arrange
        var command = CreateCommand(ErpProvider.None);

        _adapterFactory
            .Setup(f => f.GetAdapter(ErpProvider.None))
            .Throws(new ArgumentException("Desteklenmeyen ERP saglayicisi: None"));

        var handler = CreateHandler();

        // Act — The factory throws before any sync attempt. The handler's catch block
        // captures the exception and returns a failure result with the error logged.
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Desteklenmeyen");
    }

    [Fact]
    public async Task SyncOrder_Success_ShouldReturnErpReference()
    {
        // Arrange
        var command = CreateCommand(ErpProvider.Netsis);
        var erpReference = "NETSIS-INV-2026-0042";
        var mockAdapter = new Mock<IErpAdapter>();
        mockAdapter
            .Setup(a => a.SyncOrderAsync(command.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErpSyncResult.Ok(erpReference));

        _adapterFactory
            .Setup(f => f.GetAdapter(command.Provider))
            .Returns(mockAdapter.Object);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ErpRef.Should().Be(erpReference);
        result.ErrorMessage.Should().BeNull();
    }
}
