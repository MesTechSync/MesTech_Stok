using FluentAssertions;
using MesTech.Application.Commands.CreateQuotation;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Quotations;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateQuotationValidatorTests
{
    private readonly CreateQuotationValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task QuotationNumber_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { QuotationNumber = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "QuotationNumber");
    }

    [Fact]
    public async Task QuotationNumber_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { QuotationNumber = new string('Q', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "QuotationNumber");
    }

    [Fact]
    public async Task CustomerName_WhenEmpty_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CustomerName = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerName");
    }

    [Fact]
    public async Task CustomerName_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CustomerName = new string('C', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerName");
    }

    [Fact]
    public async Task CustomerTaxNumber_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CustomerTaxNumber = new string('1', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerTaxNumber");
    }

    [Fact]
    public async Task CustomerTaxNumber_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { CustomerTaxNumber = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CustomerTaxOffice_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CustomerTaxOffice = new string('T', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerTaxOffice");
    }

    [Fact]
    public async Task CustomerTaxOffice_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { CustomerTaxOffice = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CustomerAddress_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CustomerAddress = new string('A', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerAddress");
    }

    [Fact]
    public async Task CustomerAddress_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { CustomerAddress = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CustomerEmail_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CustomerEmail = new string('e', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerEmail");
    }

    [Fact]
    public async Task CustomerEmail_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { CustomerEmail = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Notes_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Notes = new string('N', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    [Fact]
    public async Task Notes_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Notes = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Terms_WhenExceeds500Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Terms = new string('T', 501) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Terms");
    }

    [Fact]
    public async Task Terms_WhenNull_ShouldPass()
    {
        var cmd = CreateValidCommand() with { Terms = null };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static CreateQuotationCommand CreateValidCommand() => new(
        QuotationNumber: "TKL-2026-0001",
        ValidUntil: DateTime.UtcNow.AddDays(30),
        CustomerId: Guid.NewGuid(),
        CustomerName: "Test Musteri A.S.",
        CustomerTaxNumber: "1234567890",
        CustomerTaxOffice: "Kadikoy VD",
        CustomerAddress: "Istanbul",
        CustomerEmail: "musteri@example.com",
        Notes: "Ozel teklif",
        Terms: "30 gun vade"
    );
}
