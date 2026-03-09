using FluentAssertions;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Adapters;

namespace MesTech.Tests.Unit.Integration;

/// <summary>
/// OpenCart bidirectional sync capability testleri.
/// ICustomerSyncCapable ve ICategorySyncCapable interface uyumlulugu + DTO dogrulamasi.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "OpenCartSync")]
[Trait("Phase", "Dalga4")]
public class OpenCartSyncCapabilityTests
{
    // ===========================================================================
    // 1. OpenCartAdapter implements ICustomerSyncCapable
    // ===========================================================================

    [Fact]
    public void OpenCartAdapter_Implements_ICustomerSyncCapable()
    {
        // Assert
        typeof(ICustomerSyncCapable).IsAssignableFrom(typeof(OpenCartAdapter))
            .Should().BeTrue("OpenCartAdapter must implement ICustomerSyncCapable");
    }

    // ===========================================================================
    // 2. OpenCartAdapter implements ICategorySyncCapable
    // ===========================================================================

    [Fact]
    public void OpenCartAdapter_Implements_ICategorySyncCapable()
    {
        // Assert
        typeof(ICategorySyncCapable).IsAssignableFrom(typeof(OpenCartAdapter))
            .Should().BeTrue("OpenCartAdapter must implement ICategorySyncCapable");
    }

    // ===========================================================================
    // 3. CustomerSyncDto has required properties
    // ===========================================================================

    [Fact]
    public void CustomerSyncDto_HasRequiredProperties()
    {
        // Arrange & Act
        var dto = new CustomerSyncDto
        {
            Id = "42",
            FirstName = "Ahmet",
            LastName = "Yilmaz",
            Email = "ahmet@example.com",
            Phone = "+905551234567",
            Address = "Ataturk Cad. No:1",
            City = "Istanbul",
            Country = "Turkey",
            DateModified = new DateTime(2026, 3, 9, 12, 0, 0, DateTimeKind.Utc)
        };

        // Assert
        dto.Id.Should().Be("42");
        dto.FirstName.Should().Be("Ahmet");
        dto.LastName.Should().Be("Yilmaz");
        dto.Email.Should().Be("ahmet@example.com");
        dto.Phone.Should().Be("+905551234567");
        dto.Address.Should().Be("Ataturk Cad. No:1");
        dto.City.Should().Be("Istanbul");
        dto.Country.Should().Be("Turkey");
        dto.DateModified.Should().Be(new DateTime(2026, 3, 9, 12, 0, 0, DateTimeKind.Utc));
    }

    // ===========================================================================
    // 4. CategorySyncDto + CategoryTreeSyncDto have required properties
    // ===========================================================================

    [Fact]
    public void CategorySyncDto_HasRequiredProperties()
    {
        // Arrange & Act — CategorySyncDto (push)
        var pushDto = new CategorySyncDto
        {
            Id = "10",
            ParentId = "5",
            Name = "Elektronik",
            Description = "Elektronik urunler",
            SortOrder = 1,
            Status = true,
            ImageUrl = "https://example.com/img/elektronik.jpg"
        };

        // Assert
        pushDto.Id.Should().Be("10");
        pushDto.ParentId.Should().Be("5");
        pushDto.Name.Should().Be("Elektronik");
        pushDto.Description.Should().Be("Elektronik urunler");
        pushDto.SortOrder.Should().Be(1);
        pushDto.Status.Should().BeTrue();
        pushDto.ImageUrl.Should().Be("https://example.com/img/elektronik.jpg");

        // Arrange & Act — CategoryTreeSyncDto (pull)
        var child = new CategoryTreeSyncDto
        {
            Id = "20",
            ParentId = "10",
            Name = "Telefonlar",
            SortOrder = 2,
            Status = true
        };

        var treeDto = new CategoryTreeSyncDto
        {
            Id = "10",
            ParentId = null,
            Name = "Elektronik",
            SortOrder = 1,
            Status = true,
            Children = new List<CategoryTreeSyncDto> { child }
        };

        // Assert
        treeDto.Id.Should().Be("10");
        treeDto.ParentId.Should().BeNull();
        treeDto.Name.Should().Be("Elektronik");
        treeDto.SortOrder.Should().Be(1);
        treeDto.Status.Should().BeTrue();
        treeDto.Children.Should().HaveCount(1);
        treeDto.Children[0].Name.Should().Be("Telefonlar");
        treeDto.Children[0].ParentId.Should().Be("10");
    }
}
