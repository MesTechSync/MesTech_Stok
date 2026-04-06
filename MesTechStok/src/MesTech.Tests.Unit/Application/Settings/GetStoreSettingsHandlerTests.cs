using FluentAssertions;
using MesTech.Application.Features.Settings.Queries.GetStoreSettings;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Settings;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetStoreSettingsHandlerTests
{
    private readonly Mock<ICompanySettingsRepository> _settingsRepo = new();
    private readonly Mock<IStoreRepository> _storeRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetStoreSettingsHandler CreateSut() => new(_settingsRepo.Object, _storeRepo.Object);

    [Fact]
    public async Task Handle_NullSettings_ReturnsEmptyCompanyName()
    {
        _settingsRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanySettings?)null);
        _storeRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>().AsReadOnly());

        var result = await CreateSut().Handle(new GetStoreSettingsQuery(_tenantId), CancellationToken.None);

        result.CompanyName.Should().BeEmpty();
        result.Stores.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithSettings_MapsAllFields()
    {
        var settings = new CompanySettings
        {
            TenantId = _tenantId,
            CompanyName = "MesTech AŞ",
            TaxNumber = "1234567890",
            Phone = "+905551112233",
            Email = "info@mestech.com",
            Address = "İstanbul"
        };
        _settingsRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);
        _storeRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>().AsReadOnly());

        var result = await CreateSut().Handle(new GetStoreSettingsQuery(_tenantId), CancellationToken.None);

        result.CompanyName.Should().Be("MesTech AŞ");
        result.TaxNumber.Should().Be("1234567890");
        result.Phone.Should().Be("+905551112233");
        result.Email.Should().Be("info@mestech.com");
    }

    [Fact]
    public async Task Handle_WithStores_MapsStoreInfo()
    {
        _settingsRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanySettings?)null);

        var store = new Store { TenantId = _tenantId, PlatformType = PlatformType.Trendyol, StoreName = "Ana Mağaza", IsActive = true };
        _storeRepo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store> { store }.AsReadOnly());

        var result = await CreateSut().Handle(new GetStoreSettingsQuery(_tenantId), CancellationToken.None);

        result.Stores.Should().HaveCount(1);
        result.Stores[0].StoreName.Should().Be("Ana Mağaza");
        result.Stores[0].PlatformType.Should().Be("Trendyol");
        result.Stores[0].IsActive.Should().BeTrue();
    }
}
