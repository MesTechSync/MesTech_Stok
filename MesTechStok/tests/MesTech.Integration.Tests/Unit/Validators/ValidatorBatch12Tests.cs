using FluentAssertions;
using MesTech.Application.Features.Stores.Commands.SaveStoreCredential;
using MesTech.Application.Features.Reporting.Commands.DeleteSavedReport;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

#region SaveStoreCredential

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class SaveStoreCredentialValidatorTests
{
    private readonly SaveStoreCredentialValidator _validator = new();

    private static SaveStoreCredentialCommand ValidCommand() => new(
        StoreId: Guid.NewGuid(),
        TenantId: Guid.NewGuid(),
        Platform: "Trendyol",
        CredentialType: "ApiKey",
        Fields: new Dictionary<string, string>
        {
            ["SupplierId"] = "123456",
            ["ApiKey"] = "test-api-key",
            ["ApiSecret"] = "test-secret"
        });

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_StoreId_Fails()
    {
        var cmd = ValidCommand() with { StoreId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StoreId");
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_Platform_Fails()
    {
        var cmd = ValidCommand() with { Platform = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Platform");
    }

    [Fact]
    public void Empty_Fields_Fails()
    {
        var cmd = ValidCommand() with { Fields = new Dictionary<string, string>() };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Fields");
    }
}

#endregion

#region DeleteSavedReport

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class DeleteSavedReportValidatorTests
{
    private readonly DeleteSavedReportValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new DeleteSavedReportCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new DeleteSavedReportCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_ReportId_Fails()
    {
        var cmd = new DeleteSavedReportCommand(Guid.NewGuid(), Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReportId");
    }
}

#endregion
