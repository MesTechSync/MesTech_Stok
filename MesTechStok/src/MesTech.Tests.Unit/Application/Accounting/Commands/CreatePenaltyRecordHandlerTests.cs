using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreatePenaltyRecord;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class CreatePenaltyRecordHandlerTests
{
    private readonly Mock<IPenaltyRecordRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreatePenaltyRecordHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CreatePenaltyRecordHandlerTests()
    {
        _sut = new CreatePenaltyRecordHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAndReturnId()
    {
        // Arrange
        var command = new CreatePenaltyRecordCommand(
            TenantId, PenaltySource.Trendyol, "Gec kargo cezasi", 50m, DateTime.Today);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<PenaltyRecord>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TaxAuthorityPenalty_ShouldCreateSuccessfully()
    {
        // Arrange
        var command = new CreatePenaltyRecordCommand(
            TenantId, PenaltySource.TaxAuthority, "KDV gecikme cezasi", 1500m,
            DateTime.Today, DueDate: DateTime.Today.AddDays(30), ReferenceNumber: "VUK-2026-001");

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
