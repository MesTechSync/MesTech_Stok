using FluentAssertions;
using MesTech.Application.Features.AI.Commands.GenerateProductDescription;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class GenerateProductDescriptionValidatorTests
{
    private readonly GenerateProductDescriptionValidator _validator = new();

    private static GenerateProductDescriptionCommand MakeCmd(
        Guid? productId = null, Guid? tenantId = null,
        string name = "Test Product", string lang = "tr") =>
        new(productId ?? Guid.NewGuid(), tenantId ?? Guid.NewGuid(),
            name, "Elektronik", "TestBrand", ["Özellik1"], lang);

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(MakeCmd());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ProductId_Fails()
    {
        var result = _validator.Validate(MakeCmd(productId: Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var result = _validator.Validate(MakeCmd(tenantId: Guid.Empty));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_ProductName_Fails()
    {
        var result = _validator.Validate(MakeCmd(name: ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductName");
    }

    [Fact]
    public void Empty_Language_Fails()
    {
        var result = _validator.Validate(MakeCmd(lang: ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Language");
    }

    [Fact]
    public void TooLong_Language_Fails()
    {
        var result = _validator.Validate(MakeCmd(lang: "toolong"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Language");
    }
}
