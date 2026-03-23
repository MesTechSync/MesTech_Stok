using FluentAssertions;
using MesTech.Application.Features.EInvoice.Commands;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.EInvoice;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateEInvoiceValidatorTests
{
    private readonly CreateEInvoiceCommandValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyBuyerVkn_ShouldFail()
    {
        var cmd = CreateValidCommand() with { BuyerVkn = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BuyerVkn");
    }

    [Fact]
    public async Task BuyerVknTooShort_ShouldFail()
    {
        var cmd = CreateValidCommand() with { BuyerVkn = "123456789" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BuyerVkn");
    }

    [Fact]
    public async Task BuyerVknTooLong_ShouldFail()
    {
        var cmd = CreateValidCommand() with { BuyerVkn = "123456789012" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BuyerVkn");
    }

    [Fact]
    public async Task BuyerVknWithLetters_ShouldFail()
    {
        var cmd = CreateValidCommand() with { BuyerVkn = "12345ABCDE" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BuyerVkn");
    }

    [Fact]
    public async Task BuyerVkn10Digits_ShouldPass()
    {
        var cmd = CreateValidCommand() with { BuyerVkn = "1234567890" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task BuyerVkn11Digits_ShouldPass()
    {
        var cmd = CreateValidCommand() with { BuyerVkn = "12345678901" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyBuyerTitle_ShouldFail()
    {
        var cmd = CreateValidCommand() with { BuyerTitle = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BuyerTitle");
    }

    [Fact]
    public async Task BuyerTitleExceeds512Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { BuyerTitle = new string('T', 513) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BuyerTitle");
    }

    [Fact]
    public async Task EArsivWithoutBuyerEmail_ShouldFail()
    {
        var cmd = CreateValidCommand() with
        {
            Scenario = EInvoiceScenario.EARSIVFATURA,
            BuyerEmail = null
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BuyerEmail");
    }

    [Fact]
    public async Task EArsivWithValidBuyerEmail_ShouldPass()
    {
        var cmd = CreateValidCommand() with
        {
            Scenario = EInvoiceScenario.EARSIVFATURA,
            BuyerEmail = "alici@example.com"
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TemelFaturaWithoutBuyerEmail_ShouldPass()
    {
        var cmd = CreateValidCommand() with
        {
            Scenario = EInvoiceScenario.TEMELFATURA,
            BuyerEmail = null
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyLines_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Lines = Array.Empty<CreateEInvoiceLineRequest>() };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Lines");
    }

    [Fact]
    public async Task LineWithZeroQuantity_ShouldFail()
    {
        var cmd = CreateValidCommand() with
        {
            Lines = new[]
            {
                new CreateEInvoiceLineRequest("Urun", 0, "C62", 100, 20, 0, null)
            }
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task LineWithZeroUnitPrice_ShouldFail()
    {
        var cmd = CreateValidCommand() with
        {
            Lines = new[]
            {
                new CreateEInvoiceLineRequest("Urun", 1, "C62", 0, 20, 0, null)
            }
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task LineWithInvalidTaxPercent_ShouldFail()
    {
        var cmd = CreateValidCommand() with
        {
            Lines = new[]
            {
                new CreateEInvoiceLineRequest("Urun", 1, "C62", 100, 15, 0, null)
            }
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(18)]
    [InlineData(20)]
    public async Task LineWithValidTaxPercent_ShouldPass(int taxPercent)
    {
        var cmd = CreateValidCommand() with
        {
            Lines = new[]
            {
                new CreateEInvoiceLineRequest("Urun", 1, "C62", 100, taxPercent, 0, null)
            }
        };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyProviderId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { ProviderId = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProviderId");
    }

    [Fact]
    public async Task IssueDateFarFuture_ShouldFail()
    {
        var cmd = CreateValidCommand() with { IssueDate = DateTime.UtcNow.AddDays(10) };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IssueDate");
    }

    [Fact]
    public async Task IssueDateToday_ShouldPass()
    {
        var cmd = CreateValidCommand() with { IssueDate = DateTime.UtcNow.Date };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    private static CreateEInvoiceCommand CreateValidCommand() => new(
        OrderId: Guid.NewGuid(),
        BuyerVkn: "1234567890",
        BuyerTitle: "Test Ticaret A.S.",
        BuyerEmail: null,
        Scenario: EInvoiceScenario.TEMELFATURA,
        Type: EInvoiceType.SATIS,
        IssueDate: DateTime.UtcNow.Date,
        CurrencyCode: "TRY",
        Lines: new[]
        {
            new CreateEInvoiceLineRequest("Laptop", 1, "C62", 25000, 20, 0, null)
        },
        ProviderId: "sovos"
    );
}
