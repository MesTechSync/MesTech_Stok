using FluentAssertions;
using MesTech.Application.Features.Invoice.Commands;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Invoice;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class ApproveInvoiceValidatorTests
{
    private readonly ApproveInvoiceValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new ApproveInvoiceCommand(Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyInvoiceId_ShouldFail()
    {
        var cmd = new ApproveInvoiceCommand(Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "InvoiceId");
    }
}
