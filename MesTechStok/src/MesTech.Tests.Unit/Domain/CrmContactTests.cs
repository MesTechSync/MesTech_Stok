using FluentAssertions;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class CrmContactTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInput_ReturnsActiveContact()
    {
        var contact = CrmContact.Create(_tenantId, "Ali Yılmaz", ContactType.Individual,
            email: "ali@test.com", phone: "+905551112233");

        contact.TenantId.Should().Be(_tenantId);
        contact.FullName.Should().Be("Ali Yılmaz");
        contact.Email.Should().Be("ali@test.com");
        contact.Type.Should().Be(ContactType.Individual);
        contact.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var act = () => CrmContact.Create(_tenantId, "", ContactType.Individual);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LinkToCustomer_SetsCustomerId()
    {
        var contact = CrmContact.Create(_tenantId, "Test", ContactType.Individual);
        var customerId = Guid.NewGuid();
        contact.LinkToCustomer(customerId);
        contact.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var contact = CrmContact.Create(_tenantId, "Test", ContactType.Company);
        contact.Deactivate();
        contact.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UpdateNotes_SetsNotes()
    {
        var contact = CrmContact.Create(_tenantId, "Test", ContactType.Individual);
        contact.UpdateNotes("VIP müşteri");
        contact.Notes.Should().Be("VIP müşteri");
    }
}
