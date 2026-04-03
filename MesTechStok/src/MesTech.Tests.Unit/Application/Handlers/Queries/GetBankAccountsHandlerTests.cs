using FluentAssertions;
using MesTech.Application.Features.Finance.Queries.GetBankAccounts;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetBankAccountsHandlerTests
{
    private readonly Mock<IBankAccountRepository> _bankRepo = new();

    private GetBankAccountsHandler CreateHandler() => new(_bankRepo.Object);

    [Fact]
    public void Constructor_NullRepository_ShouldThrow()
    {
        var act = () => new GetBankAccountsHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("bankRepo");
    }

    [Fact]
    public async Task Handle_WithAccounts_ShouldReturnMappedDtos()
    {
        var tenantId = Guid.NewGuid();
        var account1 = BankAccount.Create(tenantId, "Main Account", "TRY",
            bankName: "Garanti", iban: "TR123456", accountNumber: "001");
        var account2 = BankAccount.Create(tenantId, "USD Account", "USD",
            bankName: "Ziraat", iban: "TR789012", accountNumber: "002");

        var accounts = new List<BankAccount> { account1, account2 }.AsReadOnly();

        _bankRepo.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        var handler = CreateHandler();
        var query = new GetBankAccountsQuery(tenantId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].BankName.Should().Be("Garanti");
        result[0].CurrencyCode.Should().Be("TRY");
        result[1].BankName.Should().Be("Ziraat");
        result[1].CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_EmptyResult_ShouldReturnEmptyList()
    {
        var tenantId = Guid.NewGuid();
        _bankRepo.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankAccount>().AsReadOnly());

        var handler = CreateHandler();
        var query = new GetBankAccountsQuery(tenantId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldMapAllProperties()
    {
        var tenantId = Guid.NewGuid();
        var account = BankAccount.Create(tenantId, "Test", "EUR",
            bankName: "ING", iban: "TR111", accountNumber: "999", isDefault: true);

        _bankRepo.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BankAccount> { account }.AsReadOnly());

        var handler = CreateHandler();
        var query = new GetBankAccountsQuery(tenantId);

        var result = await handler.Handle(query, CancellationToken.None);

        var dto = result.Single();
        dto.Id.Should().Be(account.Id);
        dto.BankName.Should().Be("ING");
        dto.IBAN.Should().Be("TR111");
        dto.AccountNumber.Should().Be("999");
        dto.CurrencyCode.Should().Be("EUR");
        dto.Balance.Should().Be(0m);
        dto.IsActive.Should().BeTrue();
    }
}
