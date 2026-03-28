using FluentAssertions;
using MesTech.Application.Features.Erp.Commands.SyncOrderToErp;
using MesTech.Application.Features.Erp.Queries.GetErpDashboard;
using MesTech.Application.Features.Erp.Queries.GetErpSyncHistory;
using MesTech.Application.Features.Erp.Queries.GetErpSyncLogs;
using MesTech.Application.Features.Erp.Commands.ERPNextCustomer;
using MesTech.Application.Features.Erp.Commands.ERPNextSalesInvoice;
using MesTech.Application.Features.Erp.Commands.ERPNextStockEntry;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

[Trait("Category", "Unit")]
[Trait("Layer", "ERP")]
[Trait("Group", "Handler")]
public class ErpHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    public ErpHandlerTests() => _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    [Fact] public async Task SyncOrderToErp_Null_Throws() { var f = new Mock<IErpAdapterFactory>(); var h = new SyncOrderToErpHandler(f.Object, Mock.Of<ILogger<SyncOrderToErpHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetErpDashboard_Null_Throws() { var f = new Mock<IErpAdapterFactory>(); var h = new GetErpDashboardHandler(f.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetErpSyncHistory_Null_Throws() { var r = new Mock<IErpSyncLogRepository>(); var h = new GetErpSyncHistoryHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetErpSyncLogs_Null_Throws() { var r = new Mock<IErpSyncLogRepository>(); var h = new GetErpSyncLogsHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task ERPNextCustomer_Null_Throws() { var f = new Mock<IErpAdapterFactory>(); var h = new ERPNextCustomerHandler(f.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task ERPNextSalesInvoice_Null_Throws() { var f = new Mock<IErpAdapterFactory>(); var h = new ERPNextSalesInvoiceHandler(f.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task ERPNextStockEntry_Null_Throws() { var f = new Mock<IErpAdapterFactory>(); var h = new ERPNextStockEntryHandler(f.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
}
