using FluentAssertions;
using MesTech.Application.Features.EInvoice.Commands;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class CreateEInvoiceCommandValidatorTests
{
    private readonly CreateEInvoiceCommandValidator _validator = new();

    private static CreateEInvoiceCommand ValidCommand() => new(
        OrderId: null,
        BuyerVkn: "1234567890",
        BuyerTitle: "Test Firma A.S.",
        BuyerEmail: null,
        Scenario: EInvoiceScenario.TEMELFATURA,
        Type: EInvoiceType.SATIS,
        IssueDate: DateTime.UtcNow.Date,
        CurrencyCode: "TRY",
        Lines: new List<CreateEInvoiceLineRequest>
        {
            new("Urun A", 1m, "C62", 100m, 18, 0m, null)
        },
        ProviderId: "provider-1"
    );

    [Fact]
    public async Task Valid_Command_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Invalid_Vkn_5Digits_FailsValidation()
    {
        var command = ValidCommand() with { BuyerVkn = "12345" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Invalid_Vkn_12Digits_FailsValidation()
    {
        var command = ValidCommand() with { BuyerVkn = "123456789012" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Empty_Vkn_FailsValidation()
    {
        var command = ValidCommand() with { BuyerVkn = "" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(18)]
    [InlineData(20)]
    public async Task Valid_TaxPercent_PassesValidation(int taxPercent)
    {
        var line = new CreateEInvoiceLineRequest("Urun", 1m, "C62", 100m, taxPercent, 0m, null);
        var command = ValidCommand() with
        {
            Lines = new List<CreateEInvoiceLineRequest> { line }
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(99)]
    [InlineData(-1)]
    public async Task Invalid_TaxPercent_FailsValidation(int taxPercent)
    {
        var line = new CreateEInvoiceLineRequest("Urun", 1m, "C62", 100m, taxPercent, 0m, null);
        var command = ValidCommand() with
        {
            Lines = new List<CreateEInvoiceLineRequest> { line }
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task EmptyLines_FailsValidation()
    {
        var command = ValidCommand() with
        {
            Lines = new List<CreateEInvoiceLineRequest>()
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("fatura satiri"));
    }

    [Fact]
    public async Task FutureDate_Over7Days_FailsValidation()
    {
        var command = ValidCommand() with { IssueDate = DateTime.UtcNow.Date.AddDays(8) };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("7 gun"));
    }

    [Fact]
    public async Task EmptyProviderId_FailsValidation()
    {
        var command = ValidCommand() with { ProviderId = "" };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }
}
