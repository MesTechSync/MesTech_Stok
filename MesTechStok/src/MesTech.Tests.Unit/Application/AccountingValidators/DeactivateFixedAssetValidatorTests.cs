using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.DeactivateFixedAsset;

namespace MesTech.Tests.Unit.Application.AccountingValidators;

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class DeactivateFixedAssetValidatorTests
{
    private readonly DeactivateFixedAssetValidator _validator = new();

    private static DeactivateFixedAssetCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        TenantId: Guid.NewGuid()
    );

    [Fact]
    public async Task ValidCommand_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyId_FailsValidation()
    {
        var cmd = ValidCommand() with { Id = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task EmptyTenantId_FailsValidation()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}
