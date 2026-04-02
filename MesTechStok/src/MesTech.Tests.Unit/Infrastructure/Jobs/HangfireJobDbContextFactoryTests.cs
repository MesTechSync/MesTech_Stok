using FluentAssertions;
using MesTech.Infrastructure.Jobs;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure.Jobs;

/// <summary>
/// KÖK-1 FIX doğrulama: 4 Hangfire Job IDbContextFactory pattern testi.
/// Commit 02d40336 — AppDbContext → IDbContextFactory geçişi.
/// [PAYLAŞIM-DEV5] DEV1 yazdı.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
public class DataRetentionJobTests
{
    private readonly Mock<IDbContextFactory<AppDbContext>> _factory = new();
    private readonly Mock<ILogger<DataRetentionJob>> _logger = new();

    [Fact]
    public void Constructor_ShouldAcceptIDbContextFactory()
    {
        var sut = new DataRetentionJob(_factory.Object, _logger.Object);
        sut.Should().NotBeNull();
        sut.JobId.Should().Be("kvkk-data-retention");
        sut.CronExpression.Should().Be("0 3 * * *");
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
public class ParasutInvoiceSyncJobTests
{
    [Fact]
    public void Constructor_ShouldAcceptIDbContextFactory()
    {
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        var syncService = new Mock<MesTech.Infrastructure.Integration.ERP.IParasutInvoiceSyncService>();
        var options = Microsoft.Extensions.Options.Options.Create(
            new MesTech.Infrastructure.Integration.ERP.Parasut.ParasutOptions { InvoiceSyncEnabled = false });
        var logger = new Mock<ILogger<ParasutInvoiceSyncJob>>();

        var sut = new ParasutInvoiceSyncJob(factory.Object, syncService.Object, options, logger.Object);
        sut.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ShouldReturnEarly()
    {
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        var syncService = new Mock<MesTech.Infrastructure.Integration.ERP.IParasutInvoiceSyncService>();
        var options = Microsoft.Extensions.Options.Options.Create(
            new MesTech.Infrastructure.Integration.ERP.Parasut.ParasutOptions { InvoiceSyncEnabled = false });
        var logger = new Mock<ILogger<ParasutInvoiceSyncJob>>();

        var sut = new ParasutInvoiceSyncJob(factory.Object, syncService.Object, options, logger.Object);

        await sut.ExecuteAsync(CancellationToken.None);

        // Should NOT create a DbContext when disabled
        factory.Verify(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
public class ReliabilityScoreRecalcJobTests
{
    [Fact]
    public void Constructor_ShouldAcceptIDbContextFactory()
    {
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        var uow = new Mock<MesTech.Domain.Interfaces.IUnitOfWork>();
        var logger = new Mock<ILogger<ReliabilityScoreRecalcJob>>();

        var sut = new ReliabilityScoreRecalcJob(factory.Object, uow.Object, logger.Object);
        sut.Should().NotBeNull();
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
public class SupplierFeedSyncJobTests
{
    [Fact]
    public void Constructor_ShouldAcceptIDbContextFactory()
    {
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        var productRepo = new Mock<MesTech.Domain.Interfaces.IProductRepository>();
        var uow = new Mock<MesTech.Domain.Interfaces.IUnitOfWork>();
        var adapterFactory = new Mock<MesTech.Application.Interfaces.IAdapterFactory>();
        var httpFactory = new Mock<IHttpClientFactory>();
        var feedParserFactory = new Mock<MesTech.Application.Interfaces.IFeedParserFactory>();
        var logger = new Mock<ILogger<SupplierFeedSyncJob>>();

        var sut = new SupplierFeedSyncJob(
            factory.Object, productRepo.Object, uow.Object,
            adapterFactory.Object, httpFactory.Object, feedParserFactory.Object, logger.Object);
        sut.Should().NotBeNull();
    }
}
