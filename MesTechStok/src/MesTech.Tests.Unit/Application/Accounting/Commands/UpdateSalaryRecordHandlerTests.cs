using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.UpdateSalaryRecord;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class UpdateSalaryRecordHandlerTests
{
    private readonly Mock<ISalaryRecordRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly UpdateSalaryRecordHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public UpdateSalaryRecordHandlerTests()
    {
        _sut = new UpdateSalaryRecordHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_MarkAsCompleted_ShouldCallMarkAsPaid()
    {
        // Arrange
        var record = SalaryRecord.Create(TenantId, "Ali", 20000m, 3000m, 2800m, 3000m, 150m, 2026, 3);
        _repoMock.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        var command = new UpdateSalaryRecordCommand(record.Id, PaymentStatus.Completed, DateTime.Today);

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
        var record = SalaryRecord.Create(TenantId, "Mehmet", 18000m, 2700m, 2520m, 2700m, 136m, 2026, 3);
        _repoMock.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        var command = new UpdateSalaryRecordCommand(record.Id, PaymentStatus.Processing);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(record, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistent_ShouldThrowKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SalaryRecord?)null);

        var act = () => _sut.Handle(
            new UpdateSalaryRecordCommand(Guid.NewGuid(), PaymentStatus.Completed),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
