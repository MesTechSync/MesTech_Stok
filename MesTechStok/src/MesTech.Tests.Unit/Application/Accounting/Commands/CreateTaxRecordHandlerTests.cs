using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateTaxRecord;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class CreateTaxRecordHandlerTests
{
    private readonly Mock<ITaxRecordRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateTaxRecordHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CreateTaxRecordHandlerTests()
    {
        _sut = new CreateTaxRecordHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAndReturnId()
    {
        // Arrange
        var command = new CreateTaxRecordCommand(
            TenantId, "2026-Q1", "KDV", 100000m, 0.20m, 20000m, DateTime.Today.AddDays(30));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<TaxRecord>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_IncomeTax_ShouldCreateSuccessfully()
    {
        // Arrange
        var command = new CreateTaxRecordCommand(
            TenantId, "2026-03", "GelirVergisi", 50000m, 0.15m, 7500m,
            DateTime.Today.AddMonths(1), Year: 2026);

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
