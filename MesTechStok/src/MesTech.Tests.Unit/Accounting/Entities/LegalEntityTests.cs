using FluentAssertions;
using MesTech.Domain.Accounting.Entities;

namespace MesTech.Tests.Unit.Accounting.Entities;

[Trait("Category", "Unit")]
public class LegalEntityTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var entity = LegalEntity.Create(
            _tenantId, "MesTech A.S.", "1234567890",
            "Istanbul", "+905001112233", "info@mestech.com");

        entity.Should().NotBeNull();
        entity.Name.Should().Be("MesTech A.S.");
        entity.TaxNumber.Should().Be("1234567890");
        entity.Address.Should().Be("Istanbul");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => LegalEntity.Create(_tenantId, "", "1234567890");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyTaxNumber_ShouldThrow()
    {
        var act = () => LegalEntity.Create(_tenantId, "Test", "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetAsDefault_ShouldSetIsDefaultTrue()
    {
        var entity = LegalEntity.Create(_tenantId, "Test", "1234567890");

        entity.SetAsDefault();

        entity.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Update_ShouldUpdateFields()
    {
        var entity = LegalEntity.Create(_tenantId, "Old", "1234567890");

        entity.Update("New Name", "New Address", "+90555", "new@mail.com");

        entity.Name.Should().Be("New Name");
        entity.Address.Should().Be("New Address");
        entity.Phone.Should().Be("+90555");
        entity.Email.Should().Be("new@mail.com");
    }

    [Fact]
    public void Update_WithEmptyName_ShouldThrow()
    {
        var entity = LegalEntity.Create(_tenantId, "Test", "1234567890");

        var act = () => entity.Update("", null, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithIsDefaultFalse_ShouldBeDefault()
    {
        var entity = LegalEntity.Create(_tenantId, "Test", "1234567890");

        entity.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Create_WithIsDefaultTrue_ShouldSetCorrectly()
    {
        var entity = LegalEntity.Create(
            _tenantId, "Test", "1234567890", isDefault: true);

        entity.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        var e1 = LegalEntity.Create(_tenantId, "Test 1", "1111111111");
        var e2 = LegalEntity.Create(_tenantId, "Test 2", "2222222222");

        e1.Id.Should().NotBe(e2.Id);
    }

    [Fact]
    public void Update_ShouldUpdateUpdatedAt()
    {
        var entity = LegalEntity.Create(_tenantId, "Test", "1234567890");

        entity.Update("Updated", null, null, null);

        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
