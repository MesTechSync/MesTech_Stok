using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.CalculateDepreciation;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Accounting.Services;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Queries;

/// <summary>
/// CalculateDepreciationHandler tests — amortisman hesaplama ve bulunamayan varlık senaryosu.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class CalculateDepreciationHandlerTests
{
    private readonly Mock<IFixedAssetRepository> _assetRepoMock;
    private readonly DepreciationCalculationService _depreciationService;
    private readonly CalculateDepreciationHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CalculateDepreciationHandlerTests()
    {
        _assetRepoMock = new Mock<IFixedAssetRepository>();
        _depreciationService = new DepreciationCalculationService();
        _sut = new CalculateDepreciationHandler(_assetRepoMock.Object, _depreciationService);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsDepreciationResultWithSchedule()
    {
        // Arrange
        var asset = FixedAsset.Create(
            tenantId: _tenantId,
            name: "CNC Tezgahi",
            assetCode: "253",
            acquisitionCost: 120_000m,
            acquisitionDate: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            usefulLifeYears: 5,
            method: DepreciationMethod.StraightLine,
            description: "Test asset");

        var query = new CalculateDepreciationQuery(asset.Id);

        _assetRepoMock
            .Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AssetId.Should().Be(asset.Id);
        result.AssetName.Should().Be("CNC Tezgahi");
        result.AcquisitionCost.Should().Be(120_000m);
        result.Method.Should().Be(DepreciationMethod.StraightLine.ToString());
        result.UsefulLifeYears.Should().Be(5);
        result.CurrentYearDepreciation.Should().BeGreaterThan(0);
        result.Schedule.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_AssetNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        var query = new CalculateDepreciationQuery(missingId);

        _assetRepoMock
            .Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FixedAsset?)null);

        // Act
        var act = () => _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{missingId}*");
    }
}
