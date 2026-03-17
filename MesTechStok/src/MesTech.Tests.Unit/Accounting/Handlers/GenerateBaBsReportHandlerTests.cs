using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GenerateBaBsReport;
using MesTech.Application.Interfaces.Accounting;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Handlers;

/// <summary>
/// GenerateBaBsReportHandler tests — Ba/Bs report generation and service delegation.
/// </summary>
[Trait("Category", "Unit")]
public class GenerateBaBsReportHandlerTests
{
    private readonly Mock<IBaBsReportService> _babsServiceMock;
    private readonly GenerateBaBsReportHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GenerateBaBsReportHandlerTests()
    {
        _babsServiceMock = new Mock<IBaBsReportService>();

        _sut = new GenerateBaBsReportHandler(_babsServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidYearMonth_ReturnsBaBsReportDto()
    {
        // Arrange
        var expectedReport = new BaBsReportDto
        {
            TenantId = _tenantId,
            Year = 2026,
            Month = 3,
            BaEntries = new List<BaBsCounterpartyDto>
            {
                new()
                {
                    Name = "ABC Tedarik Ltd.",
                    VKN = "1234567890",
                    TotalAmount = 25000m,
                    DocumentCount = 5
                }
            },
            BsEntries = new List<BaBsCounterpartyDto>
            {
                new()
                {
                    Name = "XYZ Musteri A.S.",
                    VKN = "9876543210",
                    TotalAmount = 40000m,
                    DocumentCount = 8
                }
            }
        };

        _babsServiceMock.Setup(s => s.GenerateBaBsReportAsync(
                _tenantId, 2026, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GenerateBaBsReportQuery(_tenantId, 2026, 3);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Year.Should().Be(2026);
        result.Month.Should().Be(3);
        result.BaEntries.Should().HaveCount(1);
        result.BsEntries.Should().HaveCount(1);
        result.TotalBaAmount.Should().Be(25000m);
        result.TotalBsAmount.Should().Be(40000m);

        _babsServiceMock.Verify(
            s => s.GenerateBaBsReportAsync(_tenantId, 2026, 3, It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task Handle_EmptyReport_ReturnsZeroTotals()
    {
        // Arrange
        var emptyReport = new BaBsReportDto
        {
            TenantId = _tenantId,
            Year = 2026,
            Month = 1,
            BaEntries = new List<BaBsCounterpartyDto>(),
            BsEntries = new List<BaBsCounterpartyDto>()
        };

        _babsServiceMock.Setup(s => s.GenerateBaBsReportAsync(
                _tenantId, 2026, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyReport);

        var query = new GenerateBaBsReportQuery(_tenantId, 2026, 1);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.BaEntries.Should().BeEmpty();
        result.BsEntries.Should().BeEmpty();
        result.TotalBaAmount.Should().Be(0m);
        result.TotalBsAmount.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        _babsServiceMock.Setup(s => s.GenerateBaBsReportAsync(
                _tenantId, 2030, 6, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Future period not allowed."));

        var query = new GenerateBaBsReportQuery(_tenantId, 2030, 6);

        // Act
        var act = () => _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Future period not allowed*");
    }

    [Fact]
    public async Task Handle_MultipleBaEntries_AllIncludedInReport()
    {
        // Arrange
        var report = new BaBsReportDto
        {
            TenantId = _tenantId,
            Year = 2026,
            Month = 2,
            BaEntries = new List<BaBsCounterpartyDto>
            {
                new() { Name = "Firma A", VKN = "1111111111", TotalAmount = 10000m, DocumentCount = 2 },
                new() { Name = "Firma B", VKN = "2222222222", TotalAmount = 15000m, DocumentCount = 3 },
                new() { Name = "Firma C", VKN = "3333333333", TotalAmount = 8000m, DocumentCount = 1 }
            },
            BsEntries = new List<BaBsCounterpartyDto>()
        };

        _babsServiceMock.Setup(s => s.GenerateBaBsReportAsync(
                _tenantId, 2026, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var query = new GenerateBaBsReportQuery(_tenantId, 2026, 2);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.BaEntries.Should().HaveCount(3);
        result.TotalBaAmount.Should().Be(33000m);
    }
}
