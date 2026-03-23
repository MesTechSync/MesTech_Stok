using FluentAssertions;
using MesTech.Application.Features.Billing.Commands.CreateBillingInvoice;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Billing;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class CreateBillingInvoiceValidatorTests
{
    private readonly CreateBillingInvoiceValidator _sut = new();

    [Fact]
    public async Task ValidCommand_ShouldPass()
    {
        var cmd = CreateValidCommand();
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptySubscriptionId_ShouldFail()
    {
        var cmd = CreateValidCommand() with { SubscriptionId = Guid.Empty };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SubscriptionId");
    }

    [Fact]
    public async Task ZeroAmount_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Amount = 0 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public async Task NegativeAmount_ShouldFail()
    {
        var cmd = CreateValidCommand() with { Amount = -100 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public async Task EmptyCurrencyCode_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CurrencyCode = "" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrencyCode");
    }

    [Fact]
    public async Task CurrencyCodeNot3Chars_ShouldFail()
    {
        var cmd = CreateValidCommand() with { CurrencyCode = "US" };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrencyCode");
    }

    [Fact]
    public async Task TaxRateAbove1_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TaxRate = 1.1m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxRate");
    }

    [Fact]
    public async Task NegativeTaxRate_ShouldFail()
    {
        var cmd = CreateValidCommand() with { TaxRate = -0.1m };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxRate");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.20)]
    [InlineData(1)]
    public async Task TaxRateInRange_ShouldPass(double taxRate)
    {
        var cmd = CreateValidCommand() with { TaxRate = (decimal)taxRate };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ZeroDueDays_ShouldFail()
    {
        var cmd = CreateValidCommand() with { DueDays = 0 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DueDays");
    }

    [Fact]
    public async Task NegativeDueDays_ShouldFail()
    {
        var cmd = CreateValidCommand() with { DueDays = -1 };
        var result = await _sut.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DueDays");
    }

    private static CreateBillingInvoiceCommand CreateValidCommand() => new(
        TenantId: Guid.NewGuid(),
        SubscriptionId: Guid.NewGuid(),
        Amount: 299.99m,
        CurrencyCode: "TRY",
        TaxRate: 0.20m,
        DueDays: 7
    );
}
