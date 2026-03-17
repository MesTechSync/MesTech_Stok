using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateTaxRecord;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Handlers;

/// <summary>
/// CreateTaxRecordHandler tests — valid tax record creation and invalid period validation.
/// </summary>
[Trait("Category", "Unit")]
public class CreateTaxRecordHandlerTests
{
    private readonly Mock<ITaxRecordRepository> _repoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly CreateTaxRecordHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateTaxRecordHandlerTests()
    {
        _repoMock = new Mock<ITaxRecordRepository>();
        _uowMock = new Mock<IUnitOfWork>();

        _sut = new CreateTaxRecordHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsGuidAndPersists()
    {
        // Arrange
        var command = new CreateTaxRecordCommand(
            _tenantId,
            "2026-03",
            "KDV",
            50000m,
            0.20m,
            10000m,
            new DateTime(2026, 4, 26, 0, 0, 0, DateTimeKind.Utc));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);

        _repoMock.Verify(
            r => r.AddAsync(It.Is<TaxRecord>(t =>
                t.Period == "2026-03" &&
                t.TaxType == "KDV" &&
                t.TaxableAmount == 50000m &&
                t.TaxAmount == 10000m),
                It.IsAny<CancellationToken>()),
            Times.Once());

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_EmptyPeriod_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateTaxRecordCommand(
            _tenantId,
            "",           // empty period
            "KDV",
            50000m,
            0.20m,
            10000m,
            DateTime.UtcNow);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();

        _repoMock.Verify(
            r => r.AddAsync(It.IsAny<TaxRecord>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task Handle_EmptyTaxType_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreateTaxRecordCommand(
            _tenantId,
            "2026-03",
            "  ",         // whitespace tax type
            50000m,
            0.20m,
            10000m,
            DateTime.UtcNow);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_MultipleTaxTypes_EachPersistsIndependently()
    {
        // Arrange — create two separate records
        var kdvCommand = new CreateTaxRecordCommand(
            _tenantId, "2026-03", "KDV", 50000m, 0.20m, 10000m,
            new DateTime(2026, 4, 26, 0, 0, 0, DateTimeKind.Utc));

        var gelirCommand = new CreateTaxRecordCommand(
            _tenantId, "2026-03", "GelirVergisi", 80000m, 0.15m, 12000m,
            new DateTime(2026, 4, 26, 0, 0, 0, DateTimeKind.Utc));

        // Act
        var result1 = await _sut.Handle(kdvCommand, CancellationToken.None);
        var result2 = await _sut.Handle(gelirCommand, CancellationToken.None);

        // Assert — both return valid Guids, and AddAsync called twice
        result1.Should().NotBe(Guid.Empty);
        result2.Should().NotBe(Guid.Empty);
        result1.Should().NotBe(result2);

        _repoMock.Verify(
            r => r.AddAsync(It.IsAny<TaxRecord>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}
