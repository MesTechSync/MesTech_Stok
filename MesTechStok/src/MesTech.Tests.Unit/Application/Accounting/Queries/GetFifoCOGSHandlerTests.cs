using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetFifoCOGS;
using MesTech.Application.Interfaces.Accounting;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Queries;

/// <summary>
/// GetFifoCOGSHandler tests — FIFO cost of goods sold calculation.
/// Verifies single product, all products, and empty result scenarios.
/// </summary>
[Trait("Category", "Unit")]
public class GetFifoCOGSHandlerTests
{
    private readonly Mock<IFifoCostCalculationService> _fifoServiceMock;
    private readonly GetFifoCOGSHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetFifoCOGSHandlerTests()
    {
        _fifoServiceMock = new Mock<IFifoCostCalculationService>();
        _sut = new GetFifoCOGSHandler(_fifoServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithProductId_ReturnsSingleProductCOGS()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var expectedResult = new FifoCostResultDto
        {
            ProductId = productId,
            ProductName = "Test Urun",
            SKU = "SKU-001",
            TotalPurchased = 100,
            TotalSold = 60,
            CurrentStock = 40,
            TotalCOGS = 3000m,
            AverageCostPerUnit = 50m,
            RemainingLayers = new List<FifoLayerDto>
            {
                new() { PurchaseDate = DateTime.UtcNow.AddDays(-30), Quantity = 40, UnitCost = 55m }
            }
        };

        _fifoServiceMock
            .Setup(s => s.CalculateCOGSAsync(_tenantId, productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var query = new GetFifoCOGSQuery(_tenantId, productId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].ProductId.Should().Be(productId);
        result[0].TotalCOGS.Should().Be(3000m);
        result[0].CurrentStock.Should().Be(40);
        result[0].RemainingLayers.Should().HaveCount(1);

        _fifoServiceMock.Verify(
            s => s.CalculateCOGSAsync(_tenantId, productId, It.IsAny<CancellationToken>()),
            Times.Once);
        _fifoServiceMock.Verify(
            s => s.CalculateAllCOGSAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithoutProductId_ReturnsAllProductsCOGS()
    {
        // Arrange
        var results = new List<FifoCostResultDto>
        {
            new() { ProductId = Guid.NewGuid(), ProductName = "Urun A", TotalCOGS = 1000m },
            new() { ProductId = Guid.NewGuid(), ProductName = "Urun B", TotalCOGS = 2000m },
            new() { ProductId = Guid.NewGuid(), ProductName = "Urun C", TotalCOGS = 3000m }
        };

        _fifoServiceMock
            .Setup(s => s.CalculateAllCOGSAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        var query = new GetFifoCOGSQuery(_tenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Sum(r => r.TotalCOGS).Should().Be(6000m);

        _fifoServiceMock.Verify(
            s => s.CalculateAllCOGSAsync(_tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
        _fifoServiceMock.Verify(
            s => s.CalculateCOGSAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_AllProductsEmpty_ReturnsEmptyList()
    {
        // Arrange
        _fifoServiceMock
            .Setup(s => s.CalculateAllCOGSAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<FifoCostResultDto>());

        var query = new GetFifoCOGSQuery(_tenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
