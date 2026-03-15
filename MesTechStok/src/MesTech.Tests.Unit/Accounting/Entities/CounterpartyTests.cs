using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class CounterpartyTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var cp = Counterparty.Create(
            _tenantId, "Trendyol", CounterpartyType.Platform,
            vkn: "1234567890", phone: "+905001112233",
            email: "info@trendyol.com", platform: "Trendyol");

        cp.Should().NotBeNull();
        cp.Name.Should().Be("Trendyol");
        cp.CounterpartyType.Should().Be(CounterpartyType.Platform);
        cp.VKN.Should().Be("1234567890");
        cp.Phone.Should().Be("+905001112233");
        cp.Email.Should().Be("info@trendyol.com");
        cp.Platform.Should().Be("Trendyol");
    }

    [Fact]
    public void Create_ShouldSetIsActiveToTrue()
    {
        var cp = Counterparty.Create(_tenantId, "Test", CounterpartyType.Customer);

        cp.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(CounterpartyType.Platform)]
    [InlineData(CounterpartyType.Bank)]
    [InlineData(CounterpartyType.Carrier)]
    [InlineData(CounterpartyType.Supplier)]
    [InlineData(CounterpartyType.Customer)]
    public void Create_WithDifferentTypes_ShouldSetCorrectly(CounterpartyType type)
    {
        var cp = Counterparty.Create(_tenantId, "Test", type);

        cp.CounterpartyType.Should().Be(type);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => Counterparty.Create(_tenantId, "", CounterpartyType.Customer);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullName_ShouldThrow()
    {
        var act = () => Counterparty.Create(_tenantId, null!, CounterpartyType.Customer);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var cp = Counterparty.Create(_tenantId, "Test", CounterpartyType.Customer);

        cp.Deactivate();

        cp.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_ShouldUpdateUpdatedAt()
    {
        var cp = Counterparty.Create(_tenantId, "Test", CounterpartyType.Customer);

        cp.Deactivate();

        cp.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var cp = Counterparty.Create(_tenantId, "Test", CounterpartyType.Customer);
        cp.Deactivate();

        cp.Activate();

        cp.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Update_ShouldUpdateFields()
    {
        var cp = Counterparty.Create(_tenantId, "Old Name", CounterpartyType.Customer);

        cp.Update("New Name", "9876543210", "+905559998877",
            "new@example.com", "Yeni Adres", "Hepsiburada");

        cp.Name.Should().Be("New Name");
        cp.VKN.Should().Be("9876543210");
        cp.Phone.Should().Be("+905559998877");
        cp.Email.Should().Be("new@example.com");
        cp.Address.Should().Be("Yeni Adres");
        cp.Platform.Should().Be("Hepsiburada");
    }

    [Fact]
    public void Update_WithEmptyName_ShouldThrow()
    {
        var cp = Counterparty.Create(_tenantId, "Test", CounterpartyType.Customer);

        var act = () => cp.Update("", null, null, null, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ShouldUpdateUpdatedAt()
    {
        var cp = Counterparty.Create(_tenantId, "Test", CounterpartyType.Customer);

        cp.Update("Updated", null, null, null, null, null);

        cp.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithNullOptionalFields_ShouldAllowNull()
    {
        var cp = Counterparty.Create(_tenantId, "Test", CounterpartyType.Customer);

        cp.VKN.Should().BeNull();
        cp.Phone.Should().BeNull();
        cp.Email.Should().BeNull();
        cp.Address.Should().BeNull();
        cp.Platform.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var cp1 = Counterparty.Create(_tenantId, "Test 1", CounterpartyType.Customer);
        var cp2 = Counterparty.Create(_tenantId, "Test 2", CounterpartyType.Customer);

        cp1.Id.Should().NotBe(cp2.Id);
    }

    [Fact]
    public void Create_ShouldSetTenantId()
    {
        var cp = Counterparty.Create(_tenantId, "Test", CounterpartyType.Bank);

        cp.TenantId.Should().Be(_tenantId);
    }
}
