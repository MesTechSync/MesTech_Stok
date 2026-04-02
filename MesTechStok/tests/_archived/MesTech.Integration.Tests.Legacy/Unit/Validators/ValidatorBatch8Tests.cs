using FluentAssertions;
using MesTech.Application.Features.Billing.Commands.CreateSubscription;
using MesTech.Application.Features.Tenant.Commands.CreateTenant;
using MesTech.Application.Features.Tasks.Commands.CreateWorkTask;
using MesTech.Application.Features.Crm.Commands.DeactivateCampaign;
using MesTech.Application.Features.Accounting.Commands.DeactivateFixedAsset;
using MesTech.Application.Features.Calendar.Commands.DeleteCalendarEvent;
using MesTech.Application.Features.Accounting.Commands.DeleteChartOfAccount;
using MesTech.Application.Commands.DeleteCategory;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

#region CreateSubscription

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateSubscriptionValidatorTests
{
    private readonly CreateSubscriptionValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new CreateSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), BillingPeriod.Monthly, true);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new CreateSubscriptionCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_PlanId_Fails()
    {
        var cmd = new CreateSubscriptionCommand(Guid.NewGuid(), Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PlanId");
    }

    [Fact]
    public void Invalid_Period_Enum_Fails()
    {
        var cmd = new CreateSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), (BillingPeriod)999);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Period");
    }
}

#endregion

#region CreateTenant

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateTenantValidatorTests
{
    private readonly CreateTenantValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new CreateTenantCommand("MesTech Demo", "1234567890");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        var cmd = new CreateTenantCommand("", null);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Name_Over_200_Chars_Fails()
    {
        var cmd = new CreateTenantCommand(new string('T', 201), null);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void TaxNumber_Over_20_Chars_Fails()
    {
        var cmd = new CreateTenantCommand("Test Firma", new string('9', 21));
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxNumber");
    }

    [Fact]
    public void Null_TaxNumber_Passes()
    {
        var cmd = new CreateTenantCommand("Test Firma", null);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region CreateWorkTask

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class CreateWorkTaskValidatorTests
{
    private readonly CreateWorkTaskValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new CreateWorkTaskCommand(
            TenantId: Guid.NewGuid(),
            Title: "Stok sayımı yap");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new CreateWorkTaskCommand(Guid.Empty, "Test görev");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_Title_Fails()
    {
        var cmd = new CreateWorkTaskCommand(Guid.NewGuid(), "");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Title_Over_500_Chars_Fails()
    {
        var cmd = new CreateWorkTaskCommand(Guid.NewGuid(), new string('G', 501));
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }
}

#endregion

#region DeactivateCampaign (2 validators)

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class DeactivateCampaignValidatorTests
{
    private readonly DeactivateCampaignValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new DeactivateCampaignCommand(Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_CampaignId_Fails()
    {
        var cmd = new DeactivateCampaignCommand(Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CampaignId");
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class DeactivateCampaignCommandValidatorTests
{
    private readonly DeactivateCampaignCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new DeactivateCampaignCommand(Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_CampaignId_Fails()
    {
        var cmd = new DeactivateCampaignCommand(Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CampaignId");
    }
}

#endregion

#region DeactivateFixedAsset

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class DeactivateFixedAssetValidatorTests
{
    private readonly DeactivateFixedAssetValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new DeactivateFixedAssetCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Id_Fails()
    {
        var cmd = new DeactivateFixedAssetCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new DeactivateFixedAssetCommand(Guid.NewGuid(), Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}

#endregion

#region DeleteCalendarEvent

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class DeleteCalendarEventValidatorTests
{
    private readonly DeleteCalendarEventValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new DeleteCalendarEventCommand(Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Id_Fails()
    {
        var cmd = new DeleteCalendarEventCommand(Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}

#endregion

#region DeleteChartOfAccount

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class DeleteChartOfAccountValidatorTests
{
    private readonly DeleteChartOfAccountValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new DeleteChartOfAccountCommand(Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Id_Fails()
    {
        var cmd = new DeleteChartOfAccountCommand(Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}

#endregion

#region DeleteCategory

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class DeleteCategoryValidatorTests
{
    private readonly DeleteCategoryValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new DeleteCategoryCommand(Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Id_Fails()
    {
        var cmd = new DeleteCategoryCommand(Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}

#endregion
