using FluentAssertions;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// NotificationCategory enum testleri.
/// Deger sayisi ve baslangic deger kontrolu.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Notification")]
[Trait("Phase", "I-11")]
public class NotificationCategoryEnumTests
{
    [Fact(DisplayName = "NotificationCategory — should have exactly 10 values")]
    public void NotificationCategory_ShouldHave10Values()
    {
        // Act
        var values = Enum.GetValues<NotificationCategory>();

        // Assert
        values.Should().HaveCount(10);
    }

    [Theory(DisplayName = "NotificationCategory — values should start from 1 with correct mapping")]
    [InlineData(NotificationCategory.Order, 1)]
    [InlineData(NotificationCategory.Stock, 2)]
    [InlineData(NotificationCategory.Invoice, 3)]
    [InlineData(NotificationCategory.Payment, 4)]
    [InlineData(NotificationCategory.System, 5)]
    [InlineData(NotificationCategory.CRM, 6)]
    [InlineData(NotificationCategory.AI, 7)]
    [InlineData(NotificationCategory.Report, 8)]
    [InlineData(NotificationCategory.Tax, 9)]
    [InlineData(NotificationCategory.Buybox, 10)]
    public void NotificationCategory_Values_ShouldStartFrom1(NotificationCategory category, int expected)
    {
        ((int)category).Should().Be(expected);
    }
}
