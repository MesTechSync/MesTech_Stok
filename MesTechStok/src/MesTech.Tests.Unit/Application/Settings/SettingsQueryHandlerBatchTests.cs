using FluentAssertions;
using MesTech.Application.Features.Settings.Commands.SaveFulfillmentSettings;
using MesTech.Application.Features.Settings.Queries.GetErpSettings;
using MesTech.Application.Features.Settings.Queries.GetFulfillmentSettings;
using MesTech.Application.Features.Settings.Queries.GetImportSettings;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Application.Settings;

// ═══════════════════════════════════════════════════════
// GetErpSettingsHandler Tests
// ═══════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetErpSettingsHandlerTests
{
    private readonly Mock<IErpSyncLogRepository> _syncLogRepo = new();
    private readonly Mock<ICompanySettingsRepository> _settingsRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetErpSettingsHandler CreateSut() => new(_syncLogRepo.Object, _settingsRepo.Object);

    [Fact]
    public void Constructor_NullSyncRepo_Throws()
    {
        var act = () => new GetErpSettingsHandler(null!, _settingsRepo.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_NullSettings_ReturnsDefaults()
    {
        _settingsRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanySettings?)null);
        _syncLogRepo.Setup(r => r.GetByTenantPagedAsync(_tenantId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ErpSyncLog>().AsReadOnly());

        var result = await CreateSut().Handle(new GetErpSettingsQuery(_tenantId), CancellationToken.None);

        result.ActiveProvider.Should().Be(ErpProvider.None);
        result.IsConnected.Should().BeFalse();
        result.AutoSyncStock.Should().BeTrue(); // default
        result.StockSyncPeriodMinutes.Should().Be(30); // default
    }

    [Fact]
    public async Task Handle_WithSettings_MapsProviderAndSync()
    {
        var settings = new CompanySettings
        {
            TenantId = _tenantId,
            ErpProvider = ErpProvider.Parasut,
            IsErpConnected = true,
            AutoSyncStock = false,
            StockSyncPeriodMinutes = 15
        };
        _settingsRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);
        _syncLogRepo.Setup(r => r.GetByTenantPagedAsync(_tenantId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ErpSyncLog>().AsReadOnly());

        var result = await CreateSut().Handle(new GetErpSettingsQuery(_tenantId), CancellationToken.None);

        result.ActiveProvider.Should().Be(ErpProvider.Parasut);
        result.IsConnected.Should().BeTrue();
        result.AutoSyncStock.Should().BeFalse();
        result.StockSyncPeriodMinutes.Should().Be(15);
    }
}

// ═══════════════════════════════════════════════════════
// GetFulfillmentSettingsHandler Tests
// ═══════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetFulfillmentSettingsHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Constructor_NullRepo_Throws()
    {
        var act = () => new GetFulfillmentSettingsHandler(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_NoStores_ReturnsUnconfigured()
    {
        _storeRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>().AsReadOnly());

        var sut = new GetFulfillmentSettingsHandler(_storeRepo.Object);
        var result = await sut.Handle(new GetFulfillmentSettingsQuery(_tenantId), CancellationToken.None);

        result.AmazonFba.IsConfigured.Should().BeFalse();
        result.Hepsilojistik.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithAmazonStore_ReturnsConfiguredFba()
    {
        var store = new Store { TenantId = _tenantId, PlatformType = PlatformType.Amazon, IsActive = true };
        _storeRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store> { store }.AsReadOnly());

        var sut = new GetFulfillmentSettingsHandler(_storeRepo.Object);
        var result = await sut.Handle(new GetFulfillmentSettingsQuery(_tenantId), CancellationToken.None);

        result.AmazonFba.IsConfigured.Should().BeTrue();
        result.AmazonFba.ConnectionStatus.Should().Contain("aktif");
    }
}

// ═══════════════════════════════════════════════════════
// GetImportSettingsHandler Tests
// ═══════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetImportSettingsHandlerTests
{
    private readonly Mock<IImportTemplateRepository> _repo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Constructor_NullRepo_Throws()
    {
        var act = () => new GetImportSettingsHandler(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_NoTemplates_ReturnsZeroCount()
    {
        _repo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImportTemplate>().AsReadOnly());

        var sut = new GetImportSettingsHandler(_repo.Object);
        var result = await sut.Handle(new GetImportSettingsQuery(_tenantId), CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Templates.Should().BeEmpty();
    }
}

// ═══════════════════════════════════════════════════════
// SaveFulfillmentSettingsHandler Tests
// ═══════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class SaveFulfillmentSettingsHandlerTests
{
    private readonly Mock<IStoreRepository> _storeRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private SaveFulfillmentSettingsHandler CreateSut() => new(
        _storeRepo.Object, _uow.Object, NullLogger<SaveFulfillmentSettingsHandler>.Instance);

    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var act = () => CreateSut().Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccess()
    {
        var cmd = new SaveFulfillmentSettingsCommand(_tenantId, true, false);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UowThrows_ReturnsFailure()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        var cmd = new SaveFulfillmentSettingsCommand(_tenantId, false, false);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("DB error");
    }
}
