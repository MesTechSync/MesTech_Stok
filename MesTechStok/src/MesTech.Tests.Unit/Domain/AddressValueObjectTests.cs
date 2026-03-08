using FluentAssertions;
using MesTech.Domain.ValueObjects;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
public class AddressValueObjectTests
{
    [Fact]
    public void FullAddress_ShouldCombineAllFields()
    {
        var address = new Address
        {
            Street = "Ataturk Cad. No:5",
            District = "Kadikoy",
            City = "Istanbul",
            PostalCode = "34710",
            Country = "TR"
        };

        address.FullAddress.Should().Be("Ataturk Cad. No:5, Kadikoy, Istanbul 34710, TR");
    }

    [Fact]
    public void DefaultCountry_ShouldBeTR()
    {
        var address = new Address();
        address.Country.Should().Be("TR");
    }

    [Fact]
    public void Address_ShouldSupportValueEquality()
    {
        var a = new Address { Street = "A", City = "B", District = "C", PostalCode = "1" };
        var b = new Address { Street = "A", City = "B", District = "C", PostalCode = "1" };
        a.Should().Be(b);
    }
}
