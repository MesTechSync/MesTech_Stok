using FluentAssertions;
using MesTech.Application.Features.Invoice.Commands;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
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
    [Fact(DisplayName = "G080: 10 orders = 10 separate GetByIdAsync calls (N+1)")]
    public async Task G080_BulkInvoice_CallsGetByIdForEachOrder()
    {
        var orderRepoMock = new Mock<IOrderRepository>();
        var invoiceRepoMock = new Mock<IInvoiceRepository>();
        var uowMock = new Mock<IUnitOfWork>();

        // 10 farklı sipariş ID
        var orderIds = Enumerable.Range(1, 10).Select(_ => Guid.NewGuid()).ToList();

        // Her GetByIdAsync çağrısında geçerli sipariş dön
        orderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new Order
            {
                Id = id,
                OrderNumber = $"ORD-{id.ToString()[..8]}",
                CustomerName = "Test",
                TotalAmount = 100m
            });

        var sut = new BulkCreateInvoiceHandler(
            orderRepoMock.Object, invoiceRepoMock.Object, uowMock.Object);

        var cmd = new BulkCreateInvoiceCommand(Guid.NewGuid(), orderIds);
        await sut.Handle(cmd, CancellationToken.None);

        // G080 KANIT: GetByIdAsync 10 KERE çağrıldı — her biri ayrı SQL query
        orderRepoMock.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>()),
            Times.Exactly(10),
            "G080: BulkCreateInvoice calls GetByIdAsync once per order — " +
            "should use batch GetByIdsAsync instead");
    }
}
