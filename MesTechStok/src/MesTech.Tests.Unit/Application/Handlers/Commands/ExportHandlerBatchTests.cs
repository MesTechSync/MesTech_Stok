using FluentAssertions;
using MesTech.Application.Features.Orders.Commands.ExportOrders;
using MesTech.Application.Features.Product.Commands.ExportProducts;
using MesTech.Application.Features.Stock.Commands.ExportStock;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: Export handler batch testleri — Products, Orders, Stock.
/// P1: Export = müşteri veri çıkışı, hata = güven kaybı.
/// </summary>

#region ExportProducts

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class ExportProductsHandlerTests
{
    private readonly Mock<IBulkProductImportService> _importService = new();

    [Fact]
    public async Task Handle_ShouldDelegateToService()
    {
        var expected = new byte[] { 0x50, 0x4B };
        _importService.Setup(s => s.ExportProductsAsync(It.IsAny<BulkExportOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new ExportProductsHandler(_importService.Object);
        var result = await handler.Handle(new ExportProductsCommand(), CancellationToken.None);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Handle_WithFilters_ShouldPassOptions()
    {
        BulkExportOptions? captured = null;
        _importService.Setup(s => s.ExportProductsAsync(It.IsAny<BulkExportOptions>(), It.IsAny<CancellationToken>()))
            .Callback<BulkExportOptions, CancellationToken>((o, _) => captured = o)
            .ReturnsAsync(Array.Empty<byte>());

        var categoryId = Guid.NewGuid();
        var cmd = new ExportProductsCommand(PlatformType.Trendyol, categoryId, true, "csv");
        var handler = new ExportProductsHandler(_importService.Object);
        await handler.Handle(cmd, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Platform.Should().Be(PlatformType.Trendyol);
        captured.CategoryId.Should().Be(categoryId);
        captured.InStock.Should().BeTrue();
        captured.Format.Should().Be("csv");
    }
}

#endregion

#region ExportOrders

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class ExportOrdersHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IExcelExportService> _excelService = new();

    private ExportOrdersHandler CreateSut() => new(_orderRepo.Object, _excelService.Object);

    [Fact]
    public async Task Handle_NoOrders_ShouldReturnFailure()
    {
        _orderRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var cmd = new ExportOrdersCommand(Guid.NewGuid(), DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bulunamadı");
    }

    [Fact]
    public async Task Handle_WithOrders_ShouldExportSuccessfully()
    {
        var tenantId = Guid.NewGuid();
        var orders = new List<Order> { FakeData.CreateOrder() };
        _orderRepo.Setup(r => r.GetByDateRangeAsync(tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        using var exportStream = new MemoryStream(new byte[] { 1, 2, 3 });
        _excelService.Setup(s => s.ExportOrdersAsync(It.IsAny<IReadOnlyList<OrderExportDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exportStream);

        var cmd = new ExportOrdersCommand(tenantId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.ExportedCount.Should().Be(1);
        result.FileName.Should().Contain(".xlsx");
    }
}

#endregion

#region ExportStock

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class ExportStockHandlerTests
{
    [Fact]
    public async Task Handle_CsvFormat_ShouldReturnCsvFilename()
    {
        var handler = new ExportStockHandler();
        var cmd = new ExportStockCommand(Guid.NewGuid(), "csv");
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.FileName.Should().EndWith(".csv");
    }

    [Fact]
    public async Task Handle_PdfFormat_ShouldReturnPdfFilename()
    {
        var handler = new ExportStockHandler();
        var cmd = new ExportStockCommand(Guid.NewGuid(), "pdf");
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.FileName.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task Handle_DefaultFormat_ShouldReturnXlsx()
    {
        var handler = new ExportStockHandler();
        var cmd = new ExportStockCommand(Guid.NewGuid());
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.FileName.Should().EndWith(".xlsx");
    }
}

#endregion
