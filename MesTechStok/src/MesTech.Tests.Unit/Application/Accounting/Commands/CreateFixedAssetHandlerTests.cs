using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateFixedAsset;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class CreateFixedAssetHandlerTests
{
    private readonly Mock<IFixedAssetRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateFixedAssetHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public CreateFixedAssetHandlerTests()
    {
        _sut = new CreateFixedAssetHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidAsset_CreatesAndReturnsId()
    {
        // Arrange
        var command = new CreateFixedAssetCommand(
            TenantId, "Bilgisayar", "255",
            15_000m,
            new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            5, DepreciationMethod.StraightLine,
            "Dell Latitude 5540");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(
            It.IsAny<FixedAsset>(),
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
    public async Task Handle_AssetWithoutDescription_CreatesSuccessfully()
    {
        // Arrange
        var command = new CreateFixedAssetCommand(
            TenantId, "Ofis Mobilyasi", "253",
            8_000m,
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            10, DepreciationMethod.StraightLine);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
    }
}
