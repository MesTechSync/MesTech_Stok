using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.DeactivateFixedAsset;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Commands;

[Trait("Category", "Unit")]
public class DeactivateFixedAssetHandlerTests
{
    private readonly Mock<IFixedAssetRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DeactivateFixedAssetHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public DeactivateFixedAssetHandlerTests()
    {
        _sut = new DeactivateFixedAssetHandler(_repoMock.Object, _uowMock.Object);
    }

    private static FixedAsset CreateActiveAsset()
    {
        return FixedAsset.Create(
            TenantId, "Bilgisayar", "255", 15_000m,
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            5, DepreciationMethod.StraightLine, "Test asset");
    }

    [Fact]
    public async Task Handle_ExistingActiveAsset_DeactivatesSuccessfully()
    {
        // Arrange
        var asset = CreateActiveAsset();
        _repoMock
            .Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        var command = new DeactivateFixedAssetCommand(asset.Id, TenantId);

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

        var command = new DeactivateFixedAssetCommand(assetId, TenantId);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{assetId}*");
    }

    [Fact]
    public async Task Handle_SavesChangesAfterDeactivation()
    {
        // Arrange
        var asset = CreateActiveAsset();
        _repoMock
            .Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        var command = new DeactivateFixedAssetCommand(asset.Id, TenantId);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(
            It.IsAny<FixedAsset>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
