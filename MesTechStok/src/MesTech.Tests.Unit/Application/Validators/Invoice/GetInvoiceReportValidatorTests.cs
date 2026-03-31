using FluentAssertions;
using MesTech.Application.Features.Invoice.Queries;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Invoice;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetInvoiceReportValidatorTests
{
    private readonly GetInvoiceReportValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    private static GetInvoiceReportQuery CreateValidQuery() => new(From: DateTime.UtcNow.AddMonths(-1), To: DateTime.UtcNow, Platform: null);
}
