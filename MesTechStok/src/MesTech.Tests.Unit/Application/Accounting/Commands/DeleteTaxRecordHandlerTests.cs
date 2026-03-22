using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.DeleteTaxRecord;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class DeleteTaxRecordHandlerTests
{
    private readonly Mock<ITaxRecordRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DeleteTaxRecordHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public DeleteTaxRecordHandlerTests()
    {
        _sut = new DeleteTaxRecordHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingRecord_ShouldSoftDelete()
    {
        // Arrange
        var record = TaxRecord.Create(TenantId, "2026-Q1", "KDV", 100000m, 20000m, DateTime.Today);
        _repoMock.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        // Act
        await _sut.Handle(new DeleteTaxRecordCommand(record.Id), CancellationToken.None);

        // Assert
        record.IsDeleted.Should().BeTrue();
        record.DeletedAt.Should().NotBeNull();
        _repoMock.Verify(r => r.UpdateAsync(record, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistent_ShouldThrowKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxRecord?)null);

        var act = () => _sut.Handle(new DeleteTaxRecordCommand(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChanges()
    {
        // Arrange
        var record = TaxRecord.Create(TenantId, "2026-03", "GelirVergisi", 50000m, 7500m, DateTime.Today);
        _repoMock.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        // Act
        await _sut.Handle(new DeleteTaxRecordCommand(record.Id), CancellationToken.None);

        // Assert
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
