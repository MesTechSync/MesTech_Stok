using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Common;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Integration.Regression;

/// <summary>
/// 5.D1-05: New protection tests — guards against architectural regressions.
/// These tests ensure domain contracts, config cleanliness, and adapter interfaces
/// remain intact as the codebase evolves.
/// </summary>
[Trait("Category", "Regression")]
[Trait("Category", "Protection")]
public class ProtectionTests
{
    // ── Config Protection ──

    [Fact]
    public void AppSettings_ShouldNotContain_SqlServerReferences()
    {
        var srcRoot = FindSrcRoot();
        var appSettingsFiles = Directory.GetFiles(srcRoot, "appsettings*.json", SearchOption.AllDirectories)
            .Where(f => !f.Contains("obj") && !f.Contains("bin") && !f.Contains("publish"))
            .ToArray();

        foreach (var file in appSettingsFiles)
        {
            var content = File.ReadAllText(file);
            content.Should().NotContainEquivalentOf("SqlServer",
                $"'{Path.GetFileName(file)}' should not reference SqlServer (migrated to PostgreSQL)");
            content.Should().NotContainEquivalentOf("UseSqlServer",
                $"'{Path.GetFileName(file)}' should not reference UseSqlServer");
        }
    }

    [Fact]
    public void AppSettings_ShouldNotContain_SQLiteReferences()
    {
        var srcRoot = FindSrcRoot();
        var desktopSettings = Directory.GetFiles(srcRoot, "appsettings*.json", SearchOption.AllDirectories)
            .Where(f => !f.Contains("obj") && !f.Contains("bin") && !f.Contains("publish")
                     && (f.Contains("Desktop") || f.Contains("desktop")))
            .ToArray();

        foreach (var file in desktopSettings)
        {
            var content = File.ReadAllText(file);
            content.Should().NotContainEquivalentOf("UseSqlite",
                $"'{Path.GetFileName(file)}' should not reference UseSqlite (migrated to PostgreSQL)");
        }
    }

    // ── IIntegratorAdapter Contract ──

    [Fact]
    public void IIntegratorAdapter_ShouldHaveAllRequiredMembers()
    {
        var adapterType = typeof(IIntegratorAdapter);
        var members = adapterType.GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToArray();

        members.Should().Contain("PlatformCode", "IIntegratorAdapter must expose PlatformCode");
        members.Should().Contain("SupportsStockUpdate");
        members.Should().Contain("SupportsPriceUpdate");
        members.Should().Contain("SupportsShipment");
        members.Should().Contain("PushProductAsync");
        members.Should().Contain("PullProductsAsync");
        members.Should().Contain("PushStockUpdateAsync");
        members.Should().Contain("PushPriceUpdateAsync");
        members.Should().Contain("TestConnectionAsync");
        members.Should().Contain("GetCategoriesAsync");
    }

    [Fact]
    public void TrendyolAdapter_ShouldImplementIIntegratorAdapter()
    {
        var adapterType = typeof(MesTech.Infrastructure.Integration.Adapters.TrendyolAdapter);
        adapterType.Should().Implement<IIntegratorAdapter>(
            "TrendyolAdapter must implement the base adapter interface");
    }

    [Fact]
    public void OpenCartAdapter_ShouldImplementIIntegratorAdapter()
    {
        var adapterType = typeof(MesTech.Infrastructure.Integration.Adapters.OpenCartAdapter);
        adapterType.Should().Implement<IIntegratorAdapter>(
            "OpenCartAdapter must implement the base adapter interface");
    }

    // ── BaseEntity Contract ──

    [Fact]
    public void BaseEntity_ShouldHaveRequiredProperties()
    {
        var entityType = typeof(BaseEntity);
        var props = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToArray();

        props.Should().Contain("Id");
        props.Should().Contain("CreatedAt");
        props.Should().Contain("CreatedBy");
        props.Should().Contain("UpdatedAt");
        props.Should().Contain("UpdatedBy");
        props.Should().Contain("IsDeleted");
        props.Should().Contain("DeletedAt");
        props.Should().Contain("DeletedBy");
    }

    // ── PlatformType Enum ──

    [Fact]
    public void PlatformType_ShouldHave11Members()
    {
        // OpenCart=0, Trendyol=1, N11=2, Hepsiburada=3, Amazon=4,
        // Ciceksepeti=5, Pazarama=6, PttAVM=7, Ozon=8, eBay=9, Etsy=10
        var values = Enum.GetValues<PlatformType>();
        values.Length.Should().Be(11,
            "PlatformType enum should have 11 members (0-10). " +
            $"Current: {string.Join(", ", values)}");
    }

    [Theory]
    [InlineData(PlatformType.OpenCart, 0)]
    [InlineData(PlatformType.Trendyol, 1)]
    [InlineData(PlatformType.N11, 2)]
    [InlineData(PlatformType.Hepsiburada, 3)]
    [InlineData(PlatformType.Amazon, 4)]
    [InlineData(PlatformType.Ciceksepeti, 5)]
    [InlineData(PlatformType.Pazarama, 6)]
    [InlineData(PlatformType.PttAVM, 7)]
    [InlineData(PlatformType.Ozon, 8)]
    [InlineData(PlatformType.eBay, 9)]
    [InlineData(PlatformType.Etsy, 10)]
    public void PlatformType_MemberValues_ShouldBeStable(PlatformType platform, int expectedValue)
    {
        ((int)platform).Should().Be(expectedValue,
            $"{platform} value must remain {expectedValue} for backward compatibility");
    }

    // ── Product Entity Business Rules ──

    [Fact]
    public void Product_IsLowStock_ShouldReturnTrueWhenBelowMinimum()
    {
        var product = new Product { Stock = 3, MinimumStock = 10 };
        product.IsLowStock().Should().BeTrue();
    }

    [Fact]
    public void Product_IsOutOfStock_ShouldReturnTrueWhenZero()
    {
        var product = new Product { Stock = 0 };
        product.IsOutOfStock().Should().BeTrue();
    }

    [Fact]
    public void Product_IsOverStock_ShouldReturnTrueWhenAboveMaximum()
    {
        var product = new Product { Stock = 1500, MaximumStock = 1000 };
        product.IsOverStock().Should().BeTrue();
    }

    [Fact]
    public void Product_NeedsReorder_ShouldReturnTrueAtReorderLevel()
    {
        var product = new Product { Stock = 10, ReorderLevel = 10 };
        product.NeedsReorder().Should().BeTrue();
    }

    [Fact]
    public void Product_ProfitMargin_ShouldCalculateCorrectly()
    {
        var product = new Product { PurchasePrice = 50m, SalePrice = 100m };
        product.ProfitMargin.Should().Be(50m); // (100-50)/100 * 100 = 50%
    }

    [Fact]
    public void Product_TotalValue_ShouldCalculateCorrectly()
    {
        var product = new Product { Stock = 20, PurchasePrice = 50m };
        product.TotalValue.Should().Be(1000m);
    }

    [Fact]
    public void Product_AdjustStock_ShouldUpdateStockAndTimestamp()
    {
        var product = new Product { Stock = 100 };
        product.AdjustStock(50, StockMovementType.Purchase, "Test");
        product.Stock.Should().Be(150);
        product.LastStockUpdate.Should().NotBeNull();
    }

    private static string FindSrcRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
            if (Directory.Exists(Path.Combine(dir, "MesTech.Domain")))
                return dir;
        }
        throw new DirectoryNotFoundException("src/ root not found");
    }
}
