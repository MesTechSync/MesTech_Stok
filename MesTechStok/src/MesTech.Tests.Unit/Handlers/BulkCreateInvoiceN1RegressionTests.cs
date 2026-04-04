using FluentAssertions;
using MesTech.Application.Features.Invoice.Commands;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// G080 REGRESYON: BulkCreateInvoiceHandler foreach loop içinde
/// GetByIdAsync çağırıyor — N sipariş = N SQL query (N+1 problemi).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Bug", "G080")]
public class BulkCreateInvoiceN1RegressionTests
{
    private static Order CreateTestOrder()
    {
        var item = new OrderItem
        {
            ProductId = Guid.NewGuid(),
            ProductSKU = "TST-001",
            ProductName = "Test Product",
            Quantity = 1,
            UnitPrice = 100m,
            TotalPrice = 100m
        };
        return Order.CreateFromPlatform(
            Guid.NewGuid(), $"EXT-{Guid.NewGuid().ToString()[..8]}", PlatformType.Trendyol,
            "Test Customer", "test@test.com", new List<OrderItem> { item });
    }

    [Fact(DisplayName = "G080: 10 orders = 1 batch GetByIdsAsync call (N+1 fixed)")]
    public async Task G080_BulkInvoice_UsesBatchGetByIds()
    {
        var orderRepoMock = new Mock<IOrderRepository>();
        var invoiceRepoMock = new Mock<IInvoiceRepository>();
        var uowMock = new Mock<IUnitOfWork>();

        // 10 farklı sipariş ID
        var orderIds = Enumerable.Range(1, 10).Select(_ => Guid.NewGuid()).ToList();

        // Batch GetByIdsAsync — returns 10 orders in one call
        orderRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken _) =>
                ids.Select(_ => CreateTestOrder()).ToList().AsReadOnly() as IReadOnlyList<Order>);

        var sut = new BulkCreateInvoiceHandler(
            invoiceRepoMock.Object, orderRepoMock.Object, uowMock.Object,
            Mock.Of<ILogger<BulkCreateInvoiceHandler>>());

        var cmd = new BulkCreateInvoiceCommand(orderIds, InvoiceProvider.Sovos);
        await sut.Handle(cmd, CancellationToken.None);

        // G080 FIX KANIT: GetByIdsAsync 1 KERE çağrıldı — batch query
        orderRepoMock.Verify(
            r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "G080 FIX: BulkCreateInvoice now uses batch GetByIdsAsync — N+1 eliminated");

        // GetByIdAsync hiç çağrılmamış olmalı
        orderRepoMock.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "G080 FIX: GetByIdAsync should not be called — batch method used instead");
    }
}
