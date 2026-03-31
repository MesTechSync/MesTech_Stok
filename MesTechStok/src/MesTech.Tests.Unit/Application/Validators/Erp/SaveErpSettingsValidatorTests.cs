using FluentAssertions;
using MesTech.Application.Features.Settings.Commands.SaveErpSettings;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Erp;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class SaveErpSettingsValidatorTests
{
    private readonly SaveErpSettingsValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TenantId_Empty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task Provider_InvalidEnum_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ErpProvider = (ErpProvider)999 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ErpProvider");
    }

    [Fact]
    public async Task ErpProvider_Parasut_ShouldPass()
    {
        var cmd = CreateValidCommand() with { ErpProvider = ErpProvider.Parasut };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ErpProvider_Logo_ShouldPass()
    {
        var cmd = CreateValidCommand() with { ErpProvider = ErpProvider.Logo };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ErpProvider_Netsis_ShouldPass()
    {
        var cmd = CreateValidCommand() with { ErpProvider = ErpProvider.Netsis };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task StockSyncPeriodMinutes_Zero_ShouldFail()
    {
        var cmd = CreateValidCommand() with { StockSyncPeriodMinutes = 0 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StockSyncPeriodMinutes");
    }

    [Fact]
    public async Task StockSyncPeriodMinutes_Negative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { StockSyncPeriodMinutes = -5 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StockSyncPeriodMinutes");
    }

    [Fact]
    public async Task StockSyncPeriodMinutes_One_ShouldPass()
    {
        var cmd = CreateValidCommand() with { StockSyncPeriodMinutes = 1 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task PriceSyncPeriodMinutes_Zero_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PriceSyncPeriodMinutes = 0 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PriceSyncPeriodMinutes");
    }

    [Fact]
    public async Task PriceSyncPeriodMinutes_Negative_ShouldFail()
    {
        var cmd = CreateValidCommand() with { PriceSyncPeriodMinutes = -10 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PriceSyncPeriodMinutes");
    }

    [Fact]
    public async Task PriceSyncPeriodMinutes_One_ShouldPass()
    {
        var cmd = CreateValidCommand() with { PriceSyncPeriodMinutes = 1 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AutoSyncStock_False_ShouldStillPass()
    {
        var cmd = CreateValidCommand() with { AutoSyncStock = false };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AutoSyncInvoice_False_ShouldStillPass()
    {
        var cmd = CreateValidCommand() with { AutoSyncInvoice = false };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task AllInvalid_ShouldFail_WithMultipleErrors()
    {
        var cmd = CreateValidCommand() with
        {
            TenantId = Guid.Empty,
            ErpProvider = (ErpProvider)999,
            StockSyncPeriodMinutes = 0,
            PriceSyncPeriodMinutes = -1
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    private static SaveErpSettingsCommand CreateValidCommand() =>
        new(Guid.NewGuid(), ErpProvider.Parasut, true, true, 15, 30);
}
