using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.UpdateDealStage;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateDealStageValidatorTests
{
    private readonly UpdateDealStageValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new UpdateDealStageCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_DealId_Fails()
    {
        var cmd = new UpdateDealStageCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DealId");
    }

    [Fact]
    public void Empty_NewStageId_Fails()
    {
        var cmd = new UpdateDealStageCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewStageId");
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new UpdateDealStageCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}
