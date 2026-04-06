using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MesTech.Application.Features.Platform.Commands.TriggerSync;
using MesTech.Application.Features.Shipping.Commands.PrintShipmentLabel;
using MesTech.Application.Features.Settings.Commands.SaveApiSettings;
using MesTech.Application.Commands.SeedDemoData;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using MediatR;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5 Batch 9: Command handler testleri — TriggerSync, PrintShipmentLabel, SaveApiSettings, SeedDemoData.
/// </summary>

#region TriggerSync
[Trait("Category", "Unit")]
public class TriggerSyncHandlerTests
{
    [Fact]
    public async Task Handle_ShouldEnqueueJob()
    {
        var jobService = new Mock<IBackgroundJobService>();
        jobService.Setup(j => j.Enqueue(It.IsAny<Expression<Func<Task>>>()))
            .Returns("JOB-123");
        var logger = new Mock<ILogger<TriggerSyncHandler>>();

        var handler = new TriggerSyncHandler(jobService.Object, logger.Object);
        var result = await handler.Handle(
            new TriggerSyncCommand(Guid.NewGuid(), "TRENDYOL"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.JobId.Should().Be("JOB-123");
    }

    [Fact]
    public async Task Handle_JobServiceThrows_ShouldReturnError()
    {
        var jobService = new Mock<IBackgroundJobService>();
        jobService.Setup(j => j.Enqueue(It.IsAny<Expression<Func<Task>>>()))
            .Throws(new InvalidOperationException("Hangfire down"));
        var logger = new Mock<ILogger<TriggerSyncHandler>>();

        var handler = new TriggerSyncHandler(jobService.Object, logger.Object);
        var result = await handler.Handle(
            new TriggerSyncCommand(Guid.NewGuid(), "N11"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Hangfire");
    }
}
#endregion

#region PrintShipmentLabel
[Trait("Category", "Unit")]
public class PrintShipmentLabelHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnSuccess()
    {
        var handler = new PrintShipmentLabelHandler();
        var result = await handler.Handle(
            new PrintShipmentLabelCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
    }
}
#endregion

#region SeedDemoData
[Trait("Category", "Unit")]
public class SeedDemoDataHandlerTests
{
    [Fact]
    public async Task Handle_ExistingProducts_ShouldSkip()
    {
        var mediator = new Mock<IMediator>();
        var productRepo = new Mock<IProductRepository>();
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(Guid.NewGuid());
        productRepo.Setup(r => r.CountByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100); // already has products

        var handler = new SeedDemoDataHandler(mediator.Object, productRepo.Object, tenantProvider.Object);
        var result = await handler.Handle(new SeedDemoDataCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.WasSkipped.Should().BeTrue();
    }
}
#endregion

#region SaveApiSettings
[Trait("Category", "Unit")]
public class SaveApiSettingsHandlerTests
{
    [Fact]
    public async Task Handle_NewSettings_ShouldCreateAndSave()
    {
        var settingsRepo = new Mock<ICompanySettingsRepository>();
        settingsRepo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanySettings?)null);
        var uow = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<SaveApiSettingsHandler>>();

        var handler = new SaveApiSettingsHandler(settingsRepo.Object, uow.Object, logger.Object);
        var cmd = new SaveApiSettingsCommand(Guid.NewGuid(), "https://api.mestech.com", "secret", 60, true);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
#endregion
