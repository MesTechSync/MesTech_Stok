using FluentAssertions;
using MesTech.Application.Commands.SaveCompanySettings;
using MesTech.Application.Commands.SendInvoice;
using MesTech.Application.Features.Settings.Commands.UpdateStoreSettings;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Invoice = MesTech.Domain.Entities.Invoice;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5 Batch 10: Remaining command handler testleri.
/// </summary>

#region SaveCompanySettings
[Trait("Category", "Unit")]
public class SaveCompanySettingsHandlerTests
{
    [Fact]
    public async Task Handle_NewSettings_ShouldCreateAndSave()
    {
        var settingsRepo = new Mock<ICompanySettingsRepository>();
        settingsRepo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanySettings?)null);
        var warehouseRepo = new Mock<IWarehouseRepository>();
        var uow = new Mock<IUnitOfWork>();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(Guid.NewGuid());

        var handler = new SaveCompanySettingsHandler(
            settingsRepo.Object, warehouseRepo.Object, uow.Object, tenantProvider.Object);
        var cmd = new SaveCompanySettingsCommand("MesTech", "1234567890", "+90555",
            "info@mestech.com", "Istanbul", new List<WarehouseInput>());
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
#endregion

#region UpdateStoreSettings
[Trait("Category", "Unit")]
public class UpdateStoreSettingsHandlerTests
{
    [Fact]
    public async Task Handle_NoExistingSettings_ShouldCreateNewAndReturnTrue()
    {
        var settingsRepo = new Mock<ICompanySettingsRepository>();
        settingsRepo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanySettings?)null);
        var uow = new Mock<IUnitOfWork>();
        var handler = new UpdateStoreSettingsHandler(settingsRepo.Object, uow.Object);
        var cmd = new UpdateStoreSettingsCommand(Guid.NewGuid(), "Test", null, null, null, null);
        var result = await handler.Handle(cmd, CancellationToken.None);
        result.Should().BeTrue();
        settingsRepo.Verify(r => r.AddAsync(It.IsAny<CompanySettings>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
#endregion

#region SendInvoice
[Trait("Category", "Unit")]
public class SendInvoiceHandlerTests
{
    [Fact]
    public async Task Handle_InvoiceNotFound_ShouldReturnError()
    {
        var invoiceRepo = new Mock<IInvoiceRepository>();
        invoiceRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MesTech.Domain.Entities.Invoice?)null);
        var uow = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<SendInvoiceHandler>>();
        var handler = new SendInvoiceHandler(invoiceRepo.Object, uow.Object, logger.Object);
        var cmd = new SendInvoiceCommand(Guid.NewGuid());
        var result = await handler.Handle(cmd, CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
    }
}
#endregion
