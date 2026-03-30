using FluentAssertions;
using MesTech.Application.DTOs;

namespace MesTech.Tests.Unit.DTOs;

/// <summary>
/// RowVersionResponse, DeletedCountResponse, SupportedPlatformsResponse record testleri.
/// G516 cross-DEV yardım (DEV6 → DEV5).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "DTO")]
public class ApiResponseDtoTests
{
    // ── RowVersionResponse ──────────────────────────────────────

    [Fact(DisplayName = "RowVersionResponse — construction with byte array preserves value")]
    public void RowVersionResponse_WithByteArray_ShouldPreserveValue()
    {
        // Arrange
        var version = new byte[] { 0x00, 0x01, 0x02, 0xFF };

        // Act
        var sut = new RowVersionResponse(version);

        // Assert
        sut.NewRowVersion.Should().NotBeNull();
        sut.NewRowVersion.Should().BeEquivalentTo(version);
    }

    [Fact(DisplayName = "RowVersionResponse — construction with null yields null")]
    public void RowVersionResponse_WithNull_ShouldBeNull()
    {
        // Act
        var sut = new RowVersionResponse(null);

        // Assert
        sut.NewRowVersion.Should().BeNull();
    }

    [Fact(DisplayName = "RowVersionResponse — property access returns same reference")]
    public void RowVersionResponse_PropertyAccess_ShouldReturnSameReference()
    {
        // Arrange
        var version = new byte[] { 0xAA, 0xBB };
        var sut = new RowVersionResponse(version);

        // Act & Assert
        sut.NewRowVersion.Should().BeSameAs(version);
    }

    // ── DeletedCountResponse ────────────────────────────────────

    [Fact(DisplayName = "DeletedCountResponse — construction with positive int")]
    public void DeletedCountResponse_WithPositiveInt_ShouldPreserveValue()
    {
        // Act
        var sut = new DeletedCountResponse(42);

        // Assert
        sut.DeletedCount.Should().Be(42);
    }

    [Fact(DisplayName = "DeletedCountResponse — construction with zero")]
    public void DeletedCountResponse_WithZero_ShouldBeZero()
    {
        // Act
        var sut = new DeletedCountResponse(0);

        // Assert
        sut.DeletedCount.Should().Be(0);
    }

    [Fact(DisplayName = "DeletedCountResponse — property access is deterministic")]
    public void DeletedCountResponse_PropertyAccess_ShouldBeDeterministic()
    {
        // Arrange
        var sut = new DeletedCountResponse(7);

        // Act & Assert — multiple reads return same value
        sut.DeletedCount.Should().Be(sut.DeletedCount);
        sut.DeletedCount.Should().Be(7);
    }

    // ── SupportedPlatformsResponse ──────────────────────────────

    [Fact(DisplayName = "SupportedPlatformsResponse — construction with list and count")]
    public void SupportedPlatformsResponse_WithListAndCount_ShouldPreserveValues()
    {
        // Arrange
        var platforms = new List<string> { "Trendyol", "HepsiBurada", "N11" }.AsReadOnly();

        // Act
        var sut = new SupportedPlatformsResponse(platforms, 3);

        // Assert
        sut.Platforms.Should().HaveCount(3);
        sut.Count.Should().Be(3);
        sut.Platforms.Should().Contain("Trendyol");
        sut.Platforms.Should().Contain("HepsiBurada");
    }

    [Fact(DisplayName = "SupportedPlatformsResponse — empty list with zero count")]
    public void SupportedPlatformsResponse_EmptyList_ShouldHaveZeroCount()
    {
        // Arrange
        IReadOnlyList<string> empty = Array.Empty<string>();

        // Act
        var sut = new SupportedPlatformsResponse(empty, 0);

        // Assert
        sut.Platforms.Should().BeEmpty();
        sut.Count.Should().Be(0);
    }

    [Fact(DisplayName = "SupportedPlatformsResponse — count matches list length")]
    public void SupportedPlatformsResponse_CountMatchesListLength()
    {
        // Arrange
        var platforms = new List<string> { "Shopify", "WooCommerce" }.AsReadOnly();

        // Act
        var sut = new SupportedPlatformsResponse(platforms, platforms.Count);

        // Assert
        sut.Count.Should().Be(sut.Platforms.Count);
    }
}
