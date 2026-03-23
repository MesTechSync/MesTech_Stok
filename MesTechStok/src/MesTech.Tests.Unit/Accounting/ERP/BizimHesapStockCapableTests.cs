using System.Net;
using FluentAssertions;
using MesTech.Application.DTOs.ERP;
using MesTech.Application.Interfaces.Erp;
using MesTech.Infrastructure.Integration.ERP.BizimHesap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Accounting.ERP;

/// <summary>
/// BizimHesapERPAdapter IErpStockCapable contract tests.
/// Tests: GetStockLevels, GetStockByCode, UpdateStock.
/// </summary>
[Trait("Category", "Unit")]
public class BizimHesapStockCapableTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly IErpStockCapable _sut;

    public BizimHesapStockCapableTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();

        var httpClient = new HttpClient(_httpHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.bizimhesap.com")
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ERP:BizimHesap:BaseUrl"] = "https://api.bizimhesap.com/v1",
                ["ERP:BizimHesap:ApiKey"] = "test-api-key-bh"
            })
            .Build();

        var apiClient = new BizimHesapApiClient(
            httpClient,
            config,
            new Mock<ILogger<BizimHesapApiClient>>().Object);

        _sut = new BizimHesapERPAdapter(
            apiClient,
            new Mock<ILogger<BizimHesapERPAdapter>>().Object);
    }

    private void SetupHttpResponse(HttpStatusCode status, string content)
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(content)
            });
    }

    // ── GetStockLevels ──

    [Fact]
    public async Task GetStockLevels_Success_ReturnsList()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK,
            @"[
                {""code"":""STK-001"",""name"":""Urun A"",""quantity"":100,""unitCode"":""ADET"",""warehouseCode"":""WH-01"",""unitCost"":""25.50""},
                {""code"":""STK-002"",""name"":""Urun B"",""quantity"":50,""unitCode"":""KG"",""warehouseCode"":""WH-02"",""unitCost"":""10.00""}
            ]");

        // Act
        var result = await _sut.GetStockLevelsAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].ProductCode.Should().Be("STK-001");
        result[0].Quantity.Should().Be(100);
        result[0].UnitCode.Should().Be("ADET");
        result[0].WarehouseCode.Should().Be("WH-01");
        result[0].UnitCost.Should().Be(25.50m);
        result[1].ProductCode.Should().Be("STK-002");
        result[1].UnitCode.Should().Be("KG");
    }

    [Fact]
    public async Task GetStockLevels_Empty_ReturnsEmptyList()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, @"[]");

        // Act
        var result = await _sut.GetStockLevelsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStockLevels_ServerError_ReturnsEmptyList()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.InternalServerError, "error");

        // Act
        var result = await _sut.GetStockLevelsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    // ── GetStockByCode ──

    [Fact]
    public async Task GetStockByCode_Found_ReturnsItem()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK,
            @"{""code"":""STK-001"",""name"":""Urun A"",""quantity"":75,""unitCode"":""ADET"",""warehouseCode"":""WH-01"",""unitCost"":""30.00""}");

        // Act
        var result = await _sut.GetStockByCodeAsync("STK-001");

        // Assert
        result.Should().NotBeNull();
        result!.ProductCode.Should().Be("STK-001");
        result.Quantity.Should().Be(75);
        result.UnitCost.Should().Be(30m);
    }

    [Fact]
    public async Task GetStockByCode_NotFound_ReturnsNull()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.NotFound, @"{""error"":""not found""}");

        // Act
        var result = await _sut.GetStockByCodeAsync("NONEXISTENT");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStockByCode_NullInput_ThrowsArgumentException()
    {
        var act = () => _sut.GetStockByCodeAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── UpdateStock ──

    [Fact]
    public async Task UpdateStock_Success_ReturnsTrue()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, @"{""status"":""updated""}");

        // Act
        var result = await _sut.UpdateStockAsync("STK-001", 200, "WH-01");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateStock_NotFound_ReturnsFalse()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.NotFound, @"{""error"":""stock item not found""}");

        // Act
        var result = await _sut.UpdateStockAsync("NONEXISTENT", 10, "WH-01");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateStock_NullProductCode_ThrowsArgumentException()
    {
        var act = () => _sut.UpdateStockAsync(null!, 10, "WH-01");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateStock_NetworkError_ReturnsFalse()
    {
        // Arrange
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connection refused"));

        // Act
        var result = await _sut.UpdateStockAsync("STK-001", 10, "WH-01");

        // Assert
        result.Should().BeFalse();
    }
}
