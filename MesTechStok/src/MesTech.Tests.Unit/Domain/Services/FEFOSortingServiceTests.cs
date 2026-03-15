using FluentAssertions;
using MesTech.Domain.Services;

namespace MesTech.Tests.Unit.Domain.Services;

/// <summary>
/// FEFO (First Expired First Out) siralama servisi testleri.
/// Sort() ve PickForConsumption() icin kapsamli senaryo dogrulamalari.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "FEFO")]
[Trait("Phase", "Dalga12")]
public class FEFOSortingServiceTests
{
    private readonly FEFOSortingService _sut = new();

    // ══════════════════════════════════════════════════════════════════════════
    // Sort Tests
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Sort — items ordered by earliest expiration date first")]
    public void Sort_MultipleExpirationDates_OrdersByEarliestFirst()
    {
        // Arrange
        var items = new[]
        {
            CreateItem("SKU-C", expirationDate: DateTime.UtcNow.AddDays(30), quantity: 10),
            CreateItem("SKU-A", expirationDate: DateTime.UtcNow.AddDays(5), quantity: 20),
            CreateItem("SKU-B", expirationDate: DateTime.UtcNow.AddDays(15), quantity: 15),
        };

        // Act
        var sorted = _sut.Sort(items);

        // Assert
        sorted.Should().HaveCount(3);
        sorted[0].SKU.Should().Be("SKU-A"); // 5 days — earliest
        sorted[1].SKU.Should().Be("SKU-B"); // 15 days
        sorted[2].SKU.Should().Be("SKU-C"); // 30 days — latest
    }

    [Fact(DisplayName = "Sort — null expiration dates placed at end")]
    public void Sort_NullExpirationDates_PlacedAtEnd()
    {
        // Arrange
        var items = new[]
        {
            CreateItem("SKU-NO-EXPIRY", expirationDate: null, quantity: 50),
            CreateItem("SKU-EXPIRING", expirationDate: DateTime.UtcNow.AddDays(10), quantity: 30),
        };

        // Act
        var sorted = _sut.Sort(items);

        // Assert
        sorted.Should().HaveCount(2);
        sorted[0].SKU.Should().Be("SKU-EXPIRING");
        sorted[1].SKU.Should().Be("SKU-NO-EXPIRY");
    }

    [Fact(DisplayName = "Sort — same expiration date, ordered by location alphabetically")]
    public void Sort_SameExpirationDate_OrderedByLocation()
    {
        // Arrange
        var sameDate = DateTime.UtcNow.AddDays(10);
        var items = new[]
        {
            CreateItem("SKU-1", expirationDate: sameDate, quantity: 10, location: "C-Shelf"),
            CreateItem("SKU-2", expirationDate: sameDate, quantity: 10, location: "A-Shelf"),
            CreateItem("SKU-3", expirationDate: sameDate, quantity: 10, location: "B-Shelf"),
        };

        // Act
        var sorted = _sut.Sort(items);

        // Assert
        sorted[0].Location.Should().Be("A-Shelf");
        sorted[1].Location.Should().Be("B-Shelf");
        sorted[2].Location.Should().Be("C-Shelf");
    }

    [Fact(DisplayName = "Sort — zero quantity items filtered out")]
    public void Sort_ZeroQuantityItems_FilteredOut()
    {
        // Arrange
        var items = new[]
        {
            CreateItem("SKU-EMPTY", expirationDate: DateTime.UtcNow.AddDays(5), quantity: 0),
            CreateItem("SKU-FULL", expirationDate: DateTime.UtcNow.AddDays(10), quantity: 25),
        };

        // Act
        var sorted = _sut.Sort(items);

        // Assert
        sorted.Should().HaveCount(1);
        sorted[0].SKU.Should().Be("SKU-FULL");
    }

    [Fact(DisplayName = "Sort — empty input returns empty list")]
    public void Sort_EmptyInput_ReturnsEmptyList()
    {
        // Act
        var sorted = _sut.Sort(Array.Empty<FEFOStockItem>());

        // Assert
        sorted.Should().BeEmpty();
    }

    [Fact(DisplayName = "Sort — null input throws ArgumentNullException")]
    public void Sort_NullInput_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.Sort(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PickForConsumption Tests
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "PickForConsumption — exact quantity from single item")]
    public void PickForConsumption_ExactQuantityFromSingleItem()
    {
        // Arrange
        var items = new[]
        {
            CreateItem("SKU-A", expirationDate: DateTime.UtcNow.AddDays(5), quantity: 50),
        };

        // Act
        var picks = _sut.PickForConsumption(items, 50m);

        // Assert
        picks.Should().HaveCount(1);
        picks[0].Item.SKU.Should().Be("SKU-A");
        picks[0].PickQuantity.Should().Be(50m);
    }

    [Fact(DisplayName = "PickForConsumption — partial quantity from multiple items")]
    public void PickForConsumption_PartialFromMultipleItems()
    {
        // Arrange
        var items = new[]
        {
            CreateItem("SKU-A", expirationDate: DateTime.UtcNow.AddDays(3), quantity: 20),
            CreateItem("SKU-B", expirationDate: DateTime.UtcNow.AddDays(10), quantity: 30),
            CreateItem("SKU-C", expirationDate: DateTime.UtcNow.AddDays(20), quantity: 50),
        };

        // Act — need 35 units total
        var picks = _sut.PickForConsumption(items, 35m);

        // Assert — should pick 20 from A (earliest) then 15 from B
        picks.Should().HaveCount(2);
        picks[0].Item.SKU.Should().Be("SKU-A");
        picks[0].PickQuantity.Should().Be(20m);
        picks[1].Item.SKU.Should().Be("SKU-B");
        picks[1].PickQuantity.Should().Be(15m);
        picks.Sum(p => p.PickQuantity).Should().Be(35m);
    }

    [Fact(DisplayName = "PickForConsumption — overflow: requested more than available")]
    public void PickForConsumption_OverflowRequestedMoreThanAvailable()
    {
        // Arrange
        var items = new[]
        {
            CreateItem("SKU-A", expirationDate: DateTime.UtcNow.AddDays(5), quantity: 10),
            CreateItem("SKU-B", expirationDate: DateTime.UtcNow.AddDays(15), quantity: 20),
        };

        // Act — requesting 50 but only 30 available
        var picks = _sut.PickForConsumption(items, 50m);

        // Assert — picks everything available
        picks.Should().HaveCount(2);
        picks.Sum(p => p.PickQuantity).Should().Be(30m);
    }

    [Fact(DisplayName = "PickForConsumption — zero or negative quantity throws")]
    public void PickForConsumption_ZeroOrNegativeQuantity_Throws()
    {
        var items = new[] { CreateItem("SKU-A", DateTime.UtcNow.AddDays(5), 10) };

        var actZero = () => _sut.PickForConsumption(items, 0m);
        var actNegative = () => _sut.PickForConsumption(items, -5m);

        actZero.Should().Throw<ArgumentOutOfRangeException>();
        actNegative.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "PickForConsumption — null items throws ArgumentNullException")]
    public void PickForConsumption_NullItems_ThrowsArgumentNullException()
    {
        var act = () => _sut.PickForConsumption(null!, 10m);
        act.Should().Throw<ArgumentNullException>();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Helper
    // ══════════════════════════════════════════════════════════════════════════

    private static FEFOStockItem CreateItem(
        string sku,
        DateTime? expirationDate,
        decimal quantity,
        string location = "Warehouse-A",
        string? lotNumber = null)
    {
        return new FEFOStockItem(
            ProductId: Guid.NewGuid(),
            SKU: sku,
            ExpirationDate: expirationDate,
            Quantity: quantity,
            Location: location,
            LotNumber: lotNumber ?? $"LOT-{sku}");
    }
}
