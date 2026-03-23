using FluentAssertions;
using MesTech.Application.Features.EInvoice.Commands;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.EInvoice;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CancelEInvoiceValidatorTests
{
    private readonly CancelEInvoiceValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new CancelEInvoiceCommand(Guid.NewGuid(), "Yanlis kesildi");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyEInvoiceId_ShouldFail()
    {
        var cmd = new CancelEInvoiceCommand(Guid.Empty, "Yanlis kesildi");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EInvoiceId");
    }

    [Fact]
    public async Task EmptyReason_ShouldFail()
    {
        var cmd = new CancelEInvoiceCommand(Guid.NewGuid(), "");
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public async Task ReasonExceeds500Chars_ShouldFail()
    {
        var cmd = new CancelEInvoiceCommand(Guid.NewGuid(), new string('R', 501));
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }
}
