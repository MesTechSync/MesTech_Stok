using FluentAssertions;
using MesTech.Application.Features.Invoice.Commands;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class BulkCreateInvoiceValidatorTests
{
    private readonly BulkCreateInvoiceValidator _validator = new();

    private static BulkCreateInvoiceCommand ValidCommand() => new(
        OrderIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
        Provider: InvoiceProvider.Sovos);

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_OrderIds_Fails()
    {
        var cmd = ValidCommand() with { OrderIds = new List<Guid>() };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderIds");
    }

    [Fact]
    public void Over_100_OrderIds_Fails()
    {
        var ids = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var cmd = ValidCommand() with { OrderIds = ids };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderIds");
    }

    [Fact]
    public void Exactly_100_OrderIds_Passes()
    {
        var ids = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var cmd = ValidCommand() with { OrderIds = ids };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_Provider_Enum_Fails()
    {
        var cmd = ValidCommand() with { Provider = (InvoiceProvider)999 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Provider");
    }
}
