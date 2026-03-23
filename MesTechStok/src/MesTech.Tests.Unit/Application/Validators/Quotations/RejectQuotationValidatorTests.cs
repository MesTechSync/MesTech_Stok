using FluentAssertions;
using MesTech.Application.Commands.RejectQuotation;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Quotations;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class RejectQuotationValidatorTests
{
    private readonly RejectQuotationValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new RejectQuotationCommand(QuotationId: Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task QuotationId_WhenEmpty_ShouldFail()
    {
        var cmd = new RejectQuotationCommand(QuotationId: Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "QuotationId");
    }
}
