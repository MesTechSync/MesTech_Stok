using FluentAssertions;
using MesTech.Application.Features.Erp.Commands.CreateErpAccountMapping;
using MesTech.Application.Features.Erp.Commands.DeleteErpAccountMapping;
using MesTech.Application.Features.Erp.Queries.GetErpAccountMappings;
using MesTech.Application.Features.Erp.Queries.GetErpSyncHistory;
using MesTech.Application.Features.Invoice.Commands.ExportInvoiceReport;
using MesTech.Application.Features.Invoice.Commands.ExportInvoices;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

// ════════════════════════════════════════════════════════
// DEV5: Erp (4) + Invoice (2) handler batch tests — 6 handlers
// Pattern: mock repo → verify call or assert result
// ════════════════════════════════════════════════════════

#region CreateErpAccountMapping

[Trait("Category", "Unit")]
[Trait("Layer", "Erp")]
public class CreateErpAccountMappingHandlerTests
{
    [Fact]
    public async Task Handle_NoDuplicate_ShouldCallAddAndSave()
    {
        var repo = new Mock<IErpAccountMappingRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<CreateErpAccountMappingHandler>>();

        repo.Setup(r => r.FindByMesTechCodeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErpAccountMapping?)null);
        repo.Setup(r => r.FindByErpCodeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErpAccountMapping?)null);
        repo.Setup(r => r.AddAsync(It.IsAny<ErpAccountMapping>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new CreateErpAccountMappingHandler(repo.Object, uow.Object, logger.Object);
        var cmd = new CreateErpAccountMappingCommand(
            Guid.NewGuid(), "100-KASA", "Kasa", "Asset", "ERP-100", "ERP Kasa");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repo.Verify(r => r.AddAsync(It.IsAny<ErpAccountMapping>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region DeleteErpAccountMapping

[Trait("Category", "Unit")]
[Trait("Layer", "Erp")]
public class DeleteErpAccountMappingHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_ShouldReturnFalse()
    {
        var repo = new Mock<IErpAccountMappingRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<DeleteErpAccountMappingHandler>>();

        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ErpAccountMapping?)null);

        var sut = new DeleteErpAccountMappingHandler(repo.Object, uow.Object, logger.Object);
        var cmd = new DeleteErpAccountMappingCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

#endregion

#region GetErpAccountMappings

[Trait("Category", "Unit")]
[Trait("Layer", "Erp")]
public class GetErpAccountMappingsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallRepoGetByTenant()
    {
        var repo = new Mock<IErpAccountMappingRepository>();
        repo.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ErpAccountMapping>());

        var sut = new GetErpAccountMappingsHandler(repo.Object);
        var query = new GetErpAccountMappingsQuery(Guid.NewGuid());

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
        repo.Verify(r => r.GetByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetErpSyncHistory

[Trait("Category", "Unit")]
[Trait("Layer", "Erp")]
public class GetErpSyncHistoryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallRepoGetByTenantPaged()
    {
        var repo = new Mock<IErpSyncLogRepository>();
        repo.Setup(r => r.GetByTenantPagedAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ErpSyncLog>());

        var sut = new GetErpSyncHistoryHandler(repo.Object);
        var query = new GetErpSyncHistoryQuery(Guid.NewGuid());

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
        repo.Verify(r => r.GetByTenantPagedAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region ExportInvoiceReport

[Trait("Category", "Unit")]
[Trait("Layer", "Invoice")]
public class ExportInvoiceReportHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnResultWithFileName()
    {
        var sut = new ExportInvoiceReportHandler();
        var cmd = new ExportInvoiceReportCommand(Guid.NewGuid(), "pdf");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.FileName.Should().Contain(".pdf");
        result.ExportedCount.Should().Be(0);
    }
}

#endregion

#region ExportInvoices

[Trait("Category", "Unit")]
[Trait("Layer", "Invoice")]
public class ExportInvoicesHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnResultWithFileName()
    {
        var sut = new ExportInvoicesHandler();
        var cmd = new ExportInvoicesCommand(Guid.NewGuid(), "csv");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.FileName.Should().Contain(".csv");
        result.ExportedCount.Should().Be(0);
    }
}

#endregion
