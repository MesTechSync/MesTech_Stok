using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.DeletePenaltyRecord;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class DeletePenaltyRecordHandlerTests
{
    private readonly Mock<IPenaltyRecordRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DeletePenaltyRecordHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public DeletePenaltyRecordHandlerTests()
    {
        _sut = new DeletePenaltyRecordHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingRecord_ShouldSoftDelete()
    {
        // Arrange
        var record = PenaltyRecord.Create(TenantId, PenaltySource.Trendyol, "Ceza", 100m, DateTime.Today);
        _repoMock.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        // Act
        await _sut.Handle(new DeletePenaltyRecordCommand(record.Id), CancellationToken.None);

        // Assert
        record.IsDeleted.Should().BeTrue();
        record.DeletedAt.Should().NotBeNull();
        _repoMock.Verify(r => r.UpdateAsync(record, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistent_ShouldThrowKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PenaltyRecord?)null);

        var act = () => _sut.Handle(new DeletePenaltyRecordCommand(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChanges()
    {
        // Arrange
        var record = PenaltyRecord.Create(TenantId, PenaltySource.N11, "Late delivery", 75m, DateTime.Today);
        _repoMock.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        // Act
        await _sut.Handle(new DeletePenaltyRecordCommand(record.Id), CancellationToken.None);

        // Assert
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
