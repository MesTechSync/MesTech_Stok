using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.UpdateTaxRecord;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class UpdateTaxRecordHandlerTests
{
    private readonly Mock<ITaxRecordRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly UpdateTaxRecordHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public UpdateTaxRecordHandlerTests()
    {
        _sut = new UpdateTaxRecordHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_MarkAsPaid_ShouldCallMarkAsPaid()
    {
        // Arrange
        var record = TaxRecord.Create(TenantId, "2026-Q1", "KDV", 100000m, 20000m, DateTime.Today);
        _repoMock.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        var command = new UpdateTaxRecordCommand(record.Id, MarkAsPaid: true);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(record, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoMarkAsPaid_ShouldStillUpdate()
    {
        // Arrange
        var record = TaxRecord.Create(TenantId, "2026-Q1", "KDV", 100000m, 20000m, DateTime.Today);
        _repoMock.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        var command = new UpdateTaxRecordCommand(record.Id, MarkAsPaid: false);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(record, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistent_ShouldThrowKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxRecord?)null);

        var act = () => _sut.Handle(new UpdateTaxRecordCommand(Guid.NewGuid(), true), CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
