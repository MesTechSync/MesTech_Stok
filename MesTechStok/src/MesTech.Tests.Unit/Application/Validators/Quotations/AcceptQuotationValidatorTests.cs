using FluentAssertions;
using MesTech.Application.Commands.AcceptQuotation;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Quotations;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class AcceptQuotationValidatorTests
{
    private readonly AcceptQuotationValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = new AcceptQuotationCommand(QuotationId: Guid.NewGuid());
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task QuotationId_WhenEmpty_ShouldFail()
    {
        var cmd = new AcceptQuotationCommand(QuotationId: Guid.Empty);
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "QuotationId");
    }
}
