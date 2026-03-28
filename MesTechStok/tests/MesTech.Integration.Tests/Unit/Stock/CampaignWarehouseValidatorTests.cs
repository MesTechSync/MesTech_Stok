using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.CreateCampaign;
using MesTech.Application.Features.Crm.Commands.DeactivateCampaign;
using MesTech.Application.Commands.CreateWarehouse;
using MesTech.Application.Commands.DeleteWarehouse;
using MesTech.Application.Commands.UpdateWarehouse;
using MesTech.Application.Features.Platform.Commands.CreateStore;
using MesTech.Application.Commands.SyncPlatform;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CampaignWarehouseValidatorTests
{
    // ═══ CreateCampaign ═══

    [Fact]
    public void CreateCampaign_Valid_Passes()
    {
        var v = new CreateCampaignValidator();
        var cmd = new CreateCampaignCommand(Guid.NewGuid(), "Yaz Kampanyası",
            new DateTime(2026, 6, 1), new DateTime(2026, 8, 31), 15m);
        v.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateCampaign_EmptyTenantId_Fails()
    {
        var v = new CreateCampaignValidator();
        var cmd = new CreateCampaignCommand(Guid.Empty, "Test",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 10m);
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateCampaign_EmptyName_Fails()
    {
        var v = new CreateCampaignValidator();
        var cmd = new CreateCampaignCommand(Guid.NewGuid(), "",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 10m);
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateCampaign_NameOver300_Fails()
    {
        var v = new CreateCampaignValidator();
        var cmd = new CreateCampaignCommand(Guid.NewGuid(), new string('K', 301),
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 10m);
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateCampaign_DiscountZero_Fails()
    {
        var v = new CreateCampaignValidator();
        var cmd = new CreateCampaignCommand(Guid.NewGuid(), "Test",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 0m);
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateCampaign_DiscountOver100_Fails()
    {
        var v = new CreateCampaignValidator();
        var cmd = new CreateCampaignCommand(Guid.NewGuid(), "Test",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 101m);
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateCampaign_EndBeforeStart_Fails()
    {
        var v = new CreateCampaignValidator();
        var cmd = new CreateCampaignCommand(Guid.NewGuid(), "Test",
            new DateTime(2026, 6, 1), new DateTime(2026, 1, 1), 10m);
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    // ═══ DeactivateCampaign ═══

    [Fact]
    public void DeactivateCampaign_Valid_Passes()
    {
        var v = new DeactivateCampaignValidator();
        v.Validate(new DeactivateCampaignCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    }

    [Fact]
    public void DeactivateCampaign_EmptyId_Fails()
    {
        var v = new DeactivateCampaignValidator();
        v.Validate(new DeactivateCampaignCommand(Guid.Empty)).IsValid.Should().BeFalse();
    }

    // ═══ CreateWarehouse ═══

    [Fact]
    public void CreateWarehouse_Valid_Passes()
    {
        var v = new CreateWarehouseValidator();
        var cmd = new CreateWarehouseCommand("Ana Depo", "WH-001", TenantId: Guid.NewGuid());
        v.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateWarehouse_EmptyName_Fails()
    {
        var v = new CreateWarehouseValidator();
        var cmd = new CreateWarehouseCommand("", "WH-001", TenantId: Guid.NewGuid());
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateWarehouse_EmptyCode_Fails()
    {
        var v = new CreateWarehouseValidator();
        var cmd = new CreateWarehouseCommand("Ana Depo", "", TenantId: Guid.NewGuid());
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateWarehouse_EmptyTenantId_Fails()
    {
        var v = new CreateWarehouseValidator();
        var cmd = new CreateWarehouseCommand("Ana Depo", "WH-001");
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    // ═══ DeleteWarehouse ═══

    [Fact]
    public void DeleteWarehouse_Valid_Passes()
    {
        var v = new DeleteWarehouseValidator();
        v.Validate(new DeleteWarehouseCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
    }

    [Fact]
    public void DeleteWarehouse_EmptyTenantId_Fails()
    {
        var v = new DeleteWarehouseValidator();
        v.Validate(new DeleteWarehouseCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DeleteWarehouse_EmptyWarehouseId_Fails()
    {
        var v = new DeleteWarehouseValidator();
        v.Validate(new DeleteWarehouseCommand(Guid.NewGuid(), Guid.Empty)).IsValid.Should().BeFalse();
    }

    // ═══ UpdateWarehouse ═══

    [Fact]
    public void UpdateWarehouse_Valid_Passes()
    {
        var v = new UpdateWarehouseValidator();
        var cmd = new UpdateWarehouseCommand(Guid.NewGuid(), Guid.NewGuid(), "Ana Depo", "WH-001", null, "Standard", true);
        v.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateWarehouse_EmptyName_Fails()
    {
        var v = new UpdateWarehouseValidator();
        var cmd = new UpdateWarehouseCommand(Guid.NewGuid(), Guid.NewGuid(), "", "WH-001", null, "Standard", true);
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateWarehouse_NameOver200_Fails()
    {
        var v = new UpdateWarehouseValidator();
        var cmd = new UpdateWarehouseCommand(Guid.NewGuid(), Guid.NewGuid(), new string('D', 201), "WH-001", null, "Standard", true);
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateWarehouse_CodeOver50_Fails()
    {
        var v = new UpdateWarehouseValidator();
        var cmd = new UpdateWarehouseCommand(Guid.NewGuid(), Guid.NewGuid(), "Depo", new string('C', 51), null, "Standard", true);
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    // ═══ CreateStore ═══

    [Fact]
    public void CreateStore_Valid_Passes()
    {
        var v = new CreateStoreValidator();
        var cmd = new CreateStoreCommand(Guid.NewGuid(), "Trendyol Magaza", PlatformType.Trendyol, new Dictionary<string, string>());
        v.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateStore_EmptyTenantId_Fails()
    {
        var v = new CreateStoreValidator();
        var cmd = new CreateStoreCommand(Guid.Empty, "Test", PlatformType.Trendyol, new Dictionary<string, string>());
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateStore_EmptyName_Fails()
    {
        var v = new CreateStoreValidator();
        var cmd = new CreateStoreCommand(Guid.NewGuid(), "", PlatformType.Trendyol, new Dictionary<string, string>());
        v.Validate(cmd).IsValid.Should().BeFalse();
    }

    // ═══ SyncPlatform ═══

    [Fact]
    public void SyncPlatform_Valid_Passes()
    {
        var v = new SyncPlatformValidator();
        var cmd = new SyncPlatformCommand("Trendyol", SyncDirection.Pull);
        v.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void SyncPlatform_EmptyCode_Fails()
    {
        var v = new SyncPlatformValidator();
        var cmd = new SyncPlatformCommand("", SyncDirection.Pull);
        v.Validate(cmd).IsValid.Should().BeFalse();
    }
}
