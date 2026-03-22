using FluentAssertions;
using MesTech.Application.Features.Product.Commands.ExportProducts;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Moq;

namespace MesTech.Tests.Unit.Application.Products;

[Trait("Category", "Unit")]
[Trait("Domain", "Products")]
public class ExportProductsHandlerTests
{
    private readonly Mock<IBulkProductImportService> _importService = new();

    private ExportProductsHandler CreateSut() => new(_importService.Object);

    [Fact]
    public async Task Handle_ValidRequest_ReturnsExportedBytes()
    {
        // Arrange
        var expectedBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
        _importService
            .Setup(s => s.ExportProductsAsync(
                It.Is<BulkExportOptions>(o => o.Format == "xlsx"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBytes);

        var command = new ExportProductsCommand(Format: "xlsx");
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedBytes);
        _importService.Verify(
            s => s.ExportProductsAsync(It.IsAny<BulkExportOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_WithPlatformFilter_PassesCorrectOptionsToService()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _importService
            .Setup(s => s.ExportProductsAsync(It.IsAny<BulkExportOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<byte>());

        var command = new ExportProductsCommand(
            Platform: PlatformType.Trendyol,
            CategoryId: categoryId,
            InStock: true,
            Format: "csv");
        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        _importService.Verify(s => s.ExportProductsAsync(
            It.Is<BulkExportOptions>(o =>
                o.Platform == PlatformType.Trendyol &&
                o.CategoryId == categoryId &&
                o.InStock == true &&
                o.Format == "csv"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
