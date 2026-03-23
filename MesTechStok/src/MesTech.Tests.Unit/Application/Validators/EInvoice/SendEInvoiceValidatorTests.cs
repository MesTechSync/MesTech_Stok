using FluentAssertions;
using MesTech.Application.Features.EInvoice.Commands;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.EInvoice;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class SendEInvoiceValidatorTests
{
    private readonly SendEInvoiceValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new SendEInvoiceCommand(Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyEInvoiceId_ShouldFail()
    {
        var cmd = new SendEInvoiceCommand(Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EInvoiceId");
    }
}
