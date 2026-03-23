using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.RecordTaxWithholding;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class RecordTaxWithholdingHandlerTests
{
    private readonly Mock<ITaxWithholdingRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly RecordTaxWithholdingHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public RecordTaxWithholdingHandlerTests()
    {
        _sut = new RecordTaxWithholdingHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidWithholding_CreatesAndReturnsId()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var command = new RecordTaxWithholdingCommand(
            TenantId, 10_000m, 0.20m, "KDV", invoiceId);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(
            It.IsAny<TaxWithholding>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_WithholdingWithoutInvoice_CreatesSuccessfully()
    {
        // Arrange
        var command = new RecordTaxWithholdingCommand(
            TenantId, 5_000m, 0.10m, "StopajGelir");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
