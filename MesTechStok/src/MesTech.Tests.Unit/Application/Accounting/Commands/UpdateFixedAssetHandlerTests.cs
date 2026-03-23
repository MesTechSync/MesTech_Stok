using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.UpdateFixedAsset;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class UpdateFixedAssetHandlerTests
{
    private readonly Mock<IFixedAssetRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly UpdateFixedAssetHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public UpdateFixedAssetHandlerTests()
    {
        _sut = new UpdateFixedAssetHandler(_repoMock.Object, _uowMock.Object);
    }

    private static FixedAsset CreateAsset()
    {
        return FixedAsset.Create(
            TenantId, "Eski Bilgisayar", "255", 10_000m,
            new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            5, DepreciationMethod.StraightLine, "Eski aciklama");
    }

    [Fact]
    public async Task Handle_ExistingAsset_UpdatesNameAndDescription()
    {
        // Arrange
        var asset = CreateAsset();
        _repoMock
            .Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        var command = new UpdateFixedAssetCommand(
            asset.Id, TenantId, "Yeni Bilgisayar", "Yeni aciklama", 7);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(MediatR.Unit.Value);
        _repoMock.Verify(r => r.UpdateAsync(asset, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentAsset_ThrowsKeyNotFoundException()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        _repoMock
            .Setup(r => r.GetByIdAsync(assetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FixedAsset?)null);

        var command = new UpdateFixedAssetCommand(
            assetId, TenantId, "Test", null, 5);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{assetId}*");
    }

    [Fact]
    public async Task Handle_NullDescription_UpdatesSuccessfully()
    {
        // Arrange
        var asset = CreateAsset();
        _repoMock
            .Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        var command = new UpdateFixedAssetCommand(
            asset.Id, TenantId, "Guncellenmis Varlik", null, 3);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(MediatR.Unit.Value);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
