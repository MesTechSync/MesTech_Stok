using FluentAssertions;
using MesTech.Infrastructure.Integration.ERP;
using Xunit;

namespace MesTech.Tests.Unit.Infrastructure.Erp;

/// <summary>
/// I-14 T-03: ERP sync conflict resolution tests.
/// Validates bidirectional conflict rules:
///   Stock/Price  -> ERP wins
///   Order/Invoice -> MesTech wins
///   Account      -> last-updated wins
/// </summary>
public class ErpSyncConflictTests
{
    private readonly ErpConflictResolver _resolver = new();

    [Fact]
    public void StockConflict_ErpWins()
    {
        var result = _resolver.Resolve(
            SyncEntityType.Stock,
            "SKU-001",
            mestechValue: "50",
            erpValue: "42");

        result.Winner.Should().Be(SyncSource.Erp);
        result.WinnerValue.Should().Be("42");
    }

    [Fact]
    public void PriceConflict_ErpWins()
    {
        var result = _resolver.Resolve(
            SyncEntityType.Price,
            "SKU-002",
            mestechValue: "199.99",
            erpValue: "189.50");

        result.Winner.Should().Be(SyncSource.Erp);
        result.WinnerValue.Should().Be("189.50");
    }

    [Fact]
    public void OrderConflict_MesTechWins()
    {
        var result = _resolver.Resolve(
            SyncEntityType.Order,
            "ORD-1001",
            mestechValue: "Shipped",
            erpValue: "Processing");

        result.Winner.Should().Be(SyncSource.MesTech);
        result.WinnerValue.Should().Be("Shipped");
    }

    [Fact]
    public void InvoiceConflict_MesTechWins()
    {
        var result = _resolver.Resolve(
            SyncEntityType.Invoice,
            "INV-2024-001",
            mestechValue: "1250.00",
            erpValue: "1200.00");

        result.Winner.Should().Be(SyncSource.MesTech);
        result.WinnerValue.Should().Be("1250.00");
    }

    [Fact]
    public void AccountConflict_NewerErpWins()
    {
        var erpTime = DateTimeOffset.UtcNow;
        var mestechTime = erpTime.AddMinutes(-5);

        var result = _resolver.Resolve(
            SyncEntityType.Account,
            "ACC-100",
            mestechValue: "Eski Unvan Ltd.",
            erpValue: "Yeni Unvan A.S.",
            mestechUpdatedAt: mestechTime,
            erpUpdatedAt: erpTime);

        result.Winner.Should().Be(SyncSource.Erp);
        result.WinnerValue.Should().Be("Yeni Unvan A.S.");
    }

    [Fact]
    public void AccountConflict_NewerMesTechWins()
    {
        var mestechTime = DateTimeOffset.UtcNow;
        var erpTime = mestechTime.AddMinutes(-10);

        var result = _resolver.Resolve(
            SyncEntityType.Account,
            "ACC-200",
            mestechValue: "Guncel Adres",
            erpValue: "Eski Adres",
            mestechUpdatedAt: mestechTime,
            erpUpdatedAt: erpTime);

        result.Winner.Should().Be(SyncSource.MesTech);
        result.WinnerValue.Should().Be("Guncel Adres");
    }

    [Fact]
    public void AccountConflict_BothNull_ErpWins()
    {
        var result = _resolver.Resolve(
            SyncEntityType.Account,
            "ACC-300",
            mestechValue: "MesTech Value",
            erpValue: "ERP Value",
            mestechUpdatedAt: null,
            erpUpdatedAt: null);

        result.Winner.Should().Be(SyncSource.Erp, "when both timestamps are null, ERP wins as fallback");
        result.WinnerValue.Should().Be("ERP Value");
    }

    [Fact]
    public void Resolution_Contains_CorrectValues()
    {
        var result = _resolver.Resolve(
            SyncEntityType.Stock,
            "SKU-999",
            mestechValue: "100",
            erpValue: "80");

        result.EntityType.Should().Be(SyncEntityType.Stock);
        result.EntityCode.Should().Be("SKU-999");
        result.MestechValue.Should().Be("100");
        result.ErpValue.Should().Be("80");
        result.Winner.Should().Be(SyncSource.Erp);
        result.WinnerValue.Should().Be("80");
        result.Resolution.Should().Be("Auto");
        result.ResolvedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
