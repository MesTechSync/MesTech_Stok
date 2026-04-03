using FluentAssertions;
using MesTech.Application.Features.Invoice.Queries;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Invoice;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetInvoiceProvidersValidatorTests
{
    private readonly GetInvoiceProvidersValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DefaultQuery_ShouldProduceNoErrors()
    {
        var input = new GetInvoiceProvidersQuery();
        var result = await _sut.ValidateAsync(input);
        result.Errors.Should().BeEmpty();
    }

    private static GetInvoiceProvidersQuery CreateValidQuery() => new();
}
