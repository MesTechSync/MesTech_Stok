using FluentAssertions;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
[Trait("Feature", "Customer")]
[Trait("Phase", "Dalga5")]
public class CustomerDomainTests
{
    [Fact]
    public void DisplayName_NoContactPerson_ReturnsName()
    {
        var customer = new Customer { Name = "ABC Ltd.", Code = "C001", ContactPerson = null };
        customer.DisplayName.Should().Be("ABC Ltd.");
    }

    [Fact]
    public void DisplayName_EmptyContactPerson_ReturnsName()
    {
        var customer = new Customer { Name = "ABC Ltd.", Code = "C001", ContactPerson = "" };
        customer.DisplayName.Should().Be("ABC Ltd.");
    }

    [Fact]
    public void DisplayName_WithContactPerson_IncludesBoth()
    {
        var customer = new Customer { Name = "ABC Ltd.", Code = "C001", ContactPerson = "Ahmet Yılmaz" };
        customer.DisplayName.Should().Be("ABC Ltd. (Ahmet Yılmaz)");
    }

    [Fact]
    public void ToString_ContainsCodeAndName()
    {
        var customer = new Customer { Name = "ABC Ltd.", Code = "C001" };
        customer.ToString().Should().Contain("C001").And.Contain("ABC Ltd.");
    }

    [Fact]
    public void Defaults_CorrectInitialValues()
    {
        var customer = new Customer();
        customer.IsActive.Should().BeTrue();
        customer.IsBlocked.Should().BeFalse();
        customer.IsVip.Should().BeFalse();
        customer.AcceptsMarketing.Should().BeFalse();
        customer.Currency.Should().Be("TRY");
        customer.CustomerType.Should().Be("INDIVIDUAL");
    }

    [Fact]
    public void Orders_IsReadOnlyCollection()
    {
        var customer = new Customer();
        customer.Orders.Should().BeAssignableTo<IReadOnlyCollection<Order>>();
        customer.Orders.Should().BeEmpty();
    }
}

[Trait("Category", "Unit")]
[Trait("Feature", "User")]
[Trait("Phase", "Dalga5")]
public class UserDomainTests
{
    [Fact]
    public void FullName_BothNames_CombinesWithSpace()
    {
        var user = new User { Username = "jdoe", FirstName = "John", LastName = "Doe" };
        user.FullName.Should().Be("John Doe");
    }

    [Fact]
    public void FullName_OnlyFirstName_ReturnsFirstName()
    {
        var user = new User { Username = "jdoe", FirstName = "John", LastName = null };
        user.FullName.Should().Be("John");
    }

    [Fact]
    public void FullName_OnlyLastName_ReturnsLastName()
    {
        var user = new User { Username = "jdoe", FirstName = null, LastName = "Doe" };
        user.FullName.Should().Be("Doe");
    }

    [Fact]
    public void FullName_NoNames_FallsBackToUsername()
    {
        var user = new User { Username = "jdoe", FirstName = null, LastName = null };
        user.FullName.Should().Be("jdoe");
    }

    [Fact]
    public void Defaults_IsActiveTrueEmailNotConfirmed()
    {
        var user = new User();
        user.IsActive.Should().BeTrue();
        user.IsEmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public void UserRoles_IsReadOnlyCollection_IsEmpty()
    {
        var user = new User();
        user.UserRoles.Should().BeAssignableTo<IReadOnlyCollection<UserRole>>();
        user.UserRoles.Should().BeEmpty();
    }
}
