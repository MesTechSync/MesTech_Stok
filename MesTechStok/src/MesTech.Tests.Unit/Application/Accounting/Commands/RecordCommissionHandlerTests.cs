using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.RecordCommission;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class RecordCommissionHandlerTests
{
    private readonly Mock<ICommissionRecordRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly RecordCommissionHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public RecordCommissionHandlerTests()
    {
        _sut = new RecordCommissionHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAndReturnId()
    {
        // Arrange
        var command = new RecordCommissionCommand(
            TenantId, "Trendyol", 1000m, 0.15m, 150m, 10m, "ORD-001", "Elektronik");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<CommissionRecord>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutOptionalFields_ShouldCreateSuccessfully()
    {
        // Arrange
        var command = new RecordCommissionCommand(
            TenantId, "Hepsiburada", 500m, 0.12m, 60m, 5m);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
