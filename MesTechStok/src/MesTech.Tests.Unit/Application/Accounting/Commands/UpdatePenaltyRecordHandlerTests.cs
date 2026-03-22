using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.UpdatePenaltyRecord;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class UpdatePenaltyRecordHandlerTests
{
    private readonly Mock<IPenaltyRecordRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly UpdatePenaltyRecordHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public UpdatePenaltyRecordHandlerTests()
    {
        _sut = new UpdatePenaltyRecordHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_MarkAsPaid_ShouldCallMarkAsPaid()
    {
        // Arrange
        var record = PenaltyRecord.Create(TenantId, PenaltySource.Trendyol, "Ceza", 100m, DateTime.Today);
        _repoMock.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        var command = new UpdatePenaltyRecordCommand(record.Id, PaymentStatus.Completed);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(record, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdateToProcessing_ShouldCallUpdatePaymentStatus()
    {
        // Arrange
        var record = PenaltyRecord.Create(TenantId, PenaltySource.Hepsiburada, "Ceza", 200m, DateTime.Today);
        _repoMock.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        var command = new UpdatePenaltyRecordCommand(record.Id, PaymentStatus.Processing);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(record, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistent_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PenaltyRecord?)null);

        // Act
        var act = () => _sut.Handle(
            new UpdatePenaltyRecordCommand(Guid.NewGuid(), PaymentStatus.Completed),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
