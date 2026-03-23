using FluentAssertions;
using MesTech.Application.Commands.ConvertQuotationToInvoice;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Quotations;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class ConvertQuotationToInvoiceValidatorTests
{
    private readonly ConvertQuotationToInvoiceValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task QuotationId_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { QuotationId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "QuotationId");
    }

    [Fact]
    public async Task InvoiceNumber_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { InvoiceNumber = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "InvoiceNumber");
    }

    [Fact]
    public async Task InvoiceNumber_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { InvoiceNumber = new string('I', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "InvoiceNumber");
    }

    [Fact]
    public async Task InvoiceNumber_WhenExactly500Chars_ShouldPass()
    {
        var cmd = CreateValidCommand() with { InvoiceNumber = new string('I', 500) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static ConvertQuotationToInvoiceCommand CreateValidCommand() => new(
        QuotationId: Guid.NewGuid(),
        InvoiceNumber: "FTR-2026-0001"
    );
}
