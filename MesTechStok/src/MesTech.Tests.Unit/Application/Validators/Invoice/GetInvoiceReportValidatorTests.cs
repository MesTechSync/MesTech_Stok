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

    [Fact]
    public async Task ToBeforeFrom_ShouldFail()
    {
        var now = DateTime.UtcNow;
        var input = new GetInvoiceReportQuery(From: now, To: now.AddMonths(-1), Platform: null);
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "To");
    }

    private static GetInvoiceReportQuery CreateValidQuery() => new(From: DateTime.UtcNow.AddMonths(-1), To: DateTime.UtcNow, Platform: null);
}
