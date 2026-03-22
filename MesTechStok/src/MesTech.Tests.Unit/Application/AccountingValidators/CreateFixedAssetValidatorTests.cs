using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.CreateFixedAsset;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class CreateFixedAssetValidatorTests
{
    private readonly CreateFixedAssetValidator _validator = new();

    private static CreateFixedAssetCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        Name: "Dell Latitude 5540 Laptop",
        AssetCode: "255.01",
        AcquisitionCost: 45_000m,
        AcquisitionDate: new DateTime(2026, 3, 1),
        UsefulLifeYears: 3,
        Method: DepreciationMethod.StraightLine,
        Description: "IT departmani dizustu bilgisayar"
    );

    [Fact]
    public async Task ValidCommand_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_FailsValidation()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyName_FailsValidation()
    {
        var cmd = ValidCommand() with { Name = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task NameTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Name = new string('N', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task EmptyAssetCode_FailsValidation()
    {
        var cmd = ValidCommand() with { AssetCode = "" };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AssetCode");
    }

    [Fact]
    public async Task AssetCodeTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { AssetCode = new string('A', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AssetCode");
    }

    [Fact]
    public async Task NegativeAcquisitionCost_FailsValidation()
    {
        var cmd = ValidCommand() with { AcquisitionCost = -1m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AcquisitionCost");
    }

    [Fact]
    public async Task ZeroAcquisitionCost_PassesValidation()
    {
        var cmd = ValidCommand() with { AcquisitionCost = 0m };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DescriptionTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Description = new string('D', 501) };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task NullDescription_PassesValidation()
    {
        var cmd = ValidCommand() with { Description = null };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
