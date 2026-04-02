using FluentAssertions;
using MesTech.Application.Features.Invoice.Commands;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ApproveInvoiceValidatorTests
{
    private readonly ApproveInvoiceValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new ApproveInvoiceCommand(Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_InvoiceId_Fails()
    {
        var cmd = new ApproveInvoiceCommand(Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "InvoiceId");
    }
}
