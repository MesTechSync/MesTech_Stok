using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetBankTransactions;
using Xunit;

namespace MesTech.Tests.Unit.Application.Validators.Accounting;

[Trait("Category", "Unit")]
[Trait("Feature", "Validators")]
public class GetBankTransactionsValidatorTests
{
    private readonly GetBankTransactionsValidator _sut = new();

    [Fact]
    public async Task ValidInput_ShouldPass()
    {
        var input = CreateValidQuery();
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EmptyTenantId_ShouldFail()
    {
        var input = CreateValidQuery() with { TenantId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public async Task EmptyBankAccountId_ShouldFail()
    {
        var input = CreateValidQuery() with { BankAccountId = Guid.Empty };
        var result = await _sut.ValidateAsync(input);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BankAccountId");
    }

    private static GetBankTransactionsQuery CreateValidQuery() => new(TenantId: Guid.NewGuid(), BankAccountId: Guid.NewGuid());
}
