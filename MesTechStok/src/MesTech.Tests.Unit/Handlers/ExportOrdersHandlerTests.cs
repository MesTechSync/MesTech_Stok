using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Orders.Commands.ExportOrders;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class ExportOrdersHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IExcelExportService> _excelServiceMock = new();
    private readonly ExportOrdersHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ExportOrdersHandlerTests()
    {
        _sut = new ExportOrdersHandler(_orderRepoMock.Object, _excelServiceMock.Object);
    }

    [Fact]
    public async Task Handle_NoOrders_ReturnsFailWithMessage()
    {
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        _orderRepoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var cmd = new ExportOrdersCommand(_tenantId, from, to);
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bulunamadı");
        _excelServiceMock.Verify(s => s.ExportOrdersAsync(It.IsAny<IEnumerable<OrderExportDto>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithOrders_CallsExcelService()
    {
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        var orders = new List<Order>
        {
            new() { OrderNumber = "ORD-001", CustomerName = "Test", OrderDate = DateTime.UtcNow, TotalAmount = 100m }
        };
        _orderRepoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.AsReadOnly());

        var ms = new MemoryStream(new byte[] { 0x50, 0x4B }); // fake xlsx header
        _excelServiceMock.Setup(s => s.ExportOrdersAsync(It.IsAny<IEnumerable<OrderExportDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ms);

        var cmd = new ExportOrdersCommand(_tenantId, from, to);
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.ExportedCount.Should().Be(1);
        result.FileName.Should().Contain("siparisler_");
        _excelServiceMock.Verify(s => s.ExportOrdersAsync(It.IsAny<IEnumerable<OrderExportDto>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
