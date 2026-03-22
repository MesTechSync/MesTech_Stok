using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.DeleteSalaryRecord;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class DeleteSalaryRecordHandlerTests
{
    private readonly Mock<ISalaryRecordRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DeleteSalaryRecordHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public DeleteSalaryRecordHandlerTests()
    {
        _sut = new DeleteSalaryRecordHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingRecord_ShouldSoftDelete()
    {
        // Arrange
        var record = SalaryRecord.Create(TenantId, "Ali", 20000m, 3000m, 2800m, 3000m, 150m, 2026, 3);
        _repoMock.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        // Act
        await _sut.Handle(new DeleteSalaryRecordCommand(record.Id), CancellationToken.None);

        // Assert
        record.IsDeleted.Should().BeTrue();
        record.DeletedAt.Should().NotBeNull();
        _repoMock.Verify(r => r.UpdateAsync(record, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistent_ShouldThrowKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SalaryRecord?)null);

        var act = () => _sut.Handle(new DeleteSalaryRecordCommand(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldSetDeletedTimestamp()
    {
        // Arrange
        var record = SalaryRecord.Create(TenantId, "Ayse", 22000m, 3300m, 3080m, 3300m, 166m, 2026, 2);
        _repoMock.Setup(r => r.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        var before = DateTime.UtcNow;

        // Act
        await _sut.Handle(new DeleteSalaryRecordCommand(record.Id), CancellationToken.None);

        // Assert
        record.DeletedAt.Should().BeOnOrAfter(before);
    }
}
