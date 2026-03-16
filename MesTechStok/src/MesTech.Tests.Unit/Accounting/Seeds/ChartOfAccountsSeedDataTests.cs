using FluentAssertions;
using MesTech.Domain.Accounting.Enums;
using MesTech.Infrastructure.Persistence.Accounting.Seeds;

namespace MesTech.Tests.Unit.Accounting.Seeds;

[Trait("Category", "Unit")]
public class ChartOfAccountsSeedDataTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void GetDefaultAccounts_ShouldContainAccount127_ShupheliAlacaklar()
    {
        var accounts = ChartOfAccountsSeedData.GetDefaultAccounts(_tenantId);

        accounts.Should().Contain(a => a.Code == "127" && a.Name.Contains("Supheli"));
    }

    [Fact]
    public void GetDefaultAccounts_ShouldContainAccount128_ShupheliAlacakKarsiligi()
    {
        var accounts = ChartOfAccountsSeedData.GetDefaultAccounts(_tenantId);

        accounts.Should().Contain(a => a.Code == "128" && a.Name.Contains("Karsiligi"));
    }

    [Fact]
    public void GetDefaultAccounts_ShouldContainAccount329_DigerTicariBorclar()
    {
        var accounts = ChartOfAccountsSeedData.GetDefaultAccounts(_tenantId);

        accounts.Should().Contain(a => a.Code == "329" && a.Name.Contains("Diger Ticari Borclar"));
    }

    [Fact]
    public void GetDefaultAccounts_Account127_ShouldBeAsset()
    {
        var accounts = ChartOfAccountsSeedData.GetDefaultAccounts(_tenantId);
        var account127 = accounts.First(a => a.Code == "127");

        account127.AccountType.Should().Be(AccountType.Asset);
    }

    [Fact]
    public void GetDefaultAccounts_Account329_ShouldBeLiability()
    {
        var accounts = ChartOfAccountsSeedData.GetDefaultAccounts(_tenantId);
        var account329 = accounts.First(a => a.Code == "329");

        account329.AccountType.Should().Be(AccountType.Liability);
    }

    [Fact]
    public void GetDefaultAccounts_ShouldContainExistingAccounts()
    {
        var accounts = ChartOfAccountsSeedData.GetDefaultAccounts(_tenantId);

        // Pre-existing accounts should still be there
        accounts.Should().Contain(a => a.Code == "120" && a.Name.Contains("Alicilar"));
        accounts.Should().Contain(a => a.Code == "121" && a.Name.Contains("Alacak Senetleri"));
        accounts.Should().Contain(a => a.Code == "126" && a.Name.Contains("Depozito"));
        accounts.Should().Contain(a => a.Code == "320" && a.Name.Contains("Saticilar"));
        accounts.Should().Contain(a => a.Code == "321" && a.Name.Contains("Borc Senetleri"));
        accounts.Should().Contain(a => a.Code == "400" && a.Name.Contains("Banka Kredileri"));
    }

    [Fact]
    public void GetDefaultAccounts_AllAccountsShouldHaveTenantId()
    {
        var accounts = ChartOfAccountsSeedData.GetDefaultAccounts(_tenantId);

        accounts.Should().AllSatisfy(a => a.TenantId.Should().Be(_tenantId));
    }

    [Fact]
    public void GetDefaultAccounts_AccountCodes_ShouldBeUnique()
    {
        var accounts = ChartOfAccountsSeedData.GetDefaultAccounts(_tenantId);
        var codes = accounts.Select(a => a.Code).ToList();

        codes.Should().OnlyHaveUniqueItems();
    }
}
