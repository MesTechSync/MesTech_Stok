using System.Xml.Linq;
using FluentAssertions;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MesTech.Tests.Unit.Infrastructure.Adapters;

/// <summary>
/// DEV 5 — Dalga 11 Task 5.3: Amazon EU adapter stub tests.
/// Tests PlatformCode, MarketplaceIds constants, SupportsStockUpdate,
/// and unconfigured adapter guard (EnsureConfigured).
/// Depends on: DEV 3 Task 3.1 (AmazonEuAdapter) + DEV 1 Task 1.1 (PlatformType.AmazonEu).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "AmazonEU")]
[Trait("Phase", "Dalga11")]
public class AmazonEuAdapterTests
{
    private static AmazonEuAdapter CreateAdapter()
        => new(new System.Net.Http.HttpClient(),
            NullLogger<AmazonEuAdapter>.Instance);

    [Theory]
    [InlineData(AmazonEuAdapter.MarketplaceIds.DE, "A1PA6795UKMFR9")]
    [InlineData(AmazonEuAdapter.MarketplaceIds.FR, "A13V1IB3VIYZZH")]
    [InlineData(AmazonEuAdapter.MarketplaceIds.IT, "APJ6JRA9NG5V4")]
    public void MarketplaceIds_ShouldHaveCorrectValues(string actual, string expected)
    {
        actual.Should().Be(expected);
    }

    [Fact]
    public void PlatformCode_ShouldBeAmazonEu()
    {
        var adapter = CreateAdapter();

        adapter.PlatformCode.Should().Be(nameof(PlatformType.AmazonEu));
    }

    [Fact]
    public void SupportsStockUpdate_ShouldBeTrue()
    {
        var adapter = CreateAdapter();

        adapter.SupportsStockUpdate.Should().BeTrue();
    }

    [Fact]
    public async Task PullOrdersAsync_WithoutConfiguration_ShouldThrow()
    {
        // Adapter not configured (TestConnectionAsync never called)
        var adapter = CreateAdapter();

        // Credential yoksa InvalidOperationException bekleniyor
        var act = () => adapter.PullOrdersAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ══════════════════════════════════════
    // DEV 3 — Dalga 12 Task 3.4: Feed XML builder contract tests
    // PushStockUpdate + PushPriceUpdate via Feeds API
    // ══════════════════════════════════════

    /// <summary>
    /// Helper: creates a basic adapter using the 2-param constructor (HttpClient + ILogger).
    /// Used for internal XML builder tests that don't need IStoreCredentialRepository.
    /// </summary>
    private static AmazonEuAdapter CreateBasicAdapter()
        => new(new System.Net.Http.HttpClient(),
            NullLogger<AmazonEuAdapter>.Instance);

    [Fact]
    [Trait("Phase", "Dalga12")]
    public void BuildInventoryFeed_ShouldProduceValidXml_WithCorrectStructure()
    {
        // Arrange
        var adapter = CreateBasicAdapter();
        var sku = "EU-SKU-001";
        var quantity = 42;

        // Act
        var xml = adapter.BuildInventoryFeed(sku, quantity);

        // Assert — validate XDocument structure matches Amazon Inventory feed spec
        xml.Should().NotBeNull();
        xml.Root.Should().NotBeNull();
        xml.Root!.Name.LocalName.Should().Be("AmazonEnvelope");

        // Header
        var header = xml.Root.Element("Header");
        header.Should().NotBeNull();
        header!.Element("DocumentVersion")!.Value.Should().Be("1.01");

        // MessageType must be "Inventory"
        var messageType = xml.Root.Element("MessageType")!.Value;
        messageType.Should().Be("Inventory");

        // Message > Inventory > SKU + Quantity
        var message = xml.Root.Element("Message");
        message.Should().NotBeNull();
        message!.Element("MessageID")!.Value.Should().Be("1");
        message.Element("OperationType")!.Value.Should().Be("Update");

        var inventory = message.Element("Inventory");
        inventory.Should().NotBeNull();
        inventory!.Element("SKU")!.Value.Should().Be(sku);
        inventory.Element("Quantity")!.Value.Should().Be("42");
    }

    [Fact]
    [Trait("Phase", "Dalga12")]
    public void BuildPricingFeed_ShouldProduceValidXml_WithEurCurrency()
    {
        // Arrange — default marketplace is DE which uses EUR
        var adapter = CreateBasicAdapter();
        var sku = "EU-SKU-002";
        var price = 29.99m;

        // Act
        var xml = adapter.BuildPricingFeed(sku, price);

        // Assert — validate XDocument structure matches Amazon Pricing feed spec
        xml.Should().NotBeNull();
        xml.Root.Should().NotBeNull();
        xml.Root!.Name.LocalName.Should().Be("AmazonEnvelope");

        // Header
        var header = xml.Root.Element("Header");
        header.Should().NotBeNull();
        header!.Element("DocumentVersion")!.Value.Should().Be("1.01");

        // MessageType must be "Price"
        var messageType = xml.Root.Element("MessageType")!.Value;
        messageType.Should().Be("Price");

        // Message > Price > SKU + StandardPrice with currency attribute
        var message = xml.Root.Element("Message");
        message.Should().NotBeNull();
        message!.Element("MessageID")!.Value.Should().Be("1");
        message.Element("OperationType")!.Value.Should().Be("Update");

        var priceEl = message.Element("Price");
        priceEl.Should().NotBeNull();
        priceEl!.Element("SKU")!.Value.Should().Be(sku);

        var standardPrice = priceEl.Element("StandardPrice");
        standardPrice.Should().NotBeNull();
        standardPrice!.Value.Should().Be("29.99");
        standardPrice.Attribute("currency")!.Value.Should().Be("EUR",
            "DE marketplace defaults to EUR currency");
    }

    [Fact]
    [Trait("Phase", "Dalga12")]
    public async Task PushStockUpdateAsync_WithoutConfiguration_ShouldThrow()
    {
        // Arrange — adapter not configured (TestConnectionAsync never called)
        var adapter = CreateBasicAdapter();

        // Act
        var act = () => adapter.PushStockUpdateAsync(Guid.NewGuid(), 10);

        // Assert — should throw InvalidOperationException because EnsureConfigured fails
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*yapilandirilmadi*");
    }

    [Fact]
    [Trait("Phase", "Dalga12")]
    public async Task PushPriceUpdateAsync_WithoutConfiguration_ShouldThrow()
    {
        // Arrange — adapter not configured
        var adapter = CreateBasicAdapter();

        // Act
        var act = () => adapter.PushPriceUpdateAsync(Guid.NewGuid(), 99.99m);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*yapilandirilmadi*");
    }
}
