using FluentAssertions;
using MesTech.Application.Features.EInvoice.Queries;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.EInvoice;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetEInvoiceByIdValidatorTests
{
    private readonly GetEInvoiceByIdValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyEInvoiceId_ShouldFail()
    {
        var input = CreateValidQuery() with { EInvoiceId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EInvoiceId");
    }

    private static GetEInvoiceByIdQuery CreateValidQuery() => new(EInvoiceId: Guid.NewGuid());
}
