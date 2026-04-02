using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Application.Features.Dropshipping.Commands.ImportFromFeed;
using MesTech.Application.Features.Dropshipping.Commands.PreviewFeed;
using MesTech.Application.Features.Dropshipping.Commands.SyncDropshipProducts;
using MesTech.Application.Features.Dropshipping.Commands.SyncSupplierPrices;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

// ═══════════════════════════════════════════════════════════════
// BATCH 15: Dropshipping validators — 14 validator tümü
// ═══════════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ExportPoolProductsToCsvValidatorTests
{
    private readonly ExportPoolProductsToCsvValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new ExportPoolProductsToCsvCommand(Guid.NewGuid(), new List<Guid> { Guid.NewGuid() }, 10m, false)).IsValid.Should().BeTrue();
    [Fact] public void Empty_PoolId() => _v.Validate(new ExportPoolProductsToCsvCommand(Guid.Empty, new List<Guid> { Guid.NewGuid() }, 10m, false)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ExportPoolProductsToPlatformValidatorTests
{
    private readonly ExportPoolProductsToPlatformValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new ExportPoolProductsToPlatformCommand(Guid.NewGuid(), new List<Guid> { Guid.NewGuid() }, "Trendyol", 10m, false)).IsValid.Should().BeTrue();
    [Fact] public void Empty_PoolId() => _v.Validate(new ExportPoolProductsToPlatformCommand(Guid.Empty, new List<Guid> { Guid.NewGuid() }, "T", 10m, false)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ExportPoolProductsToXmlValidatorTests
{
    private readonly ExportPoolProductsToXmlValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new ExportPoolProductsToXmlCommand(Guid.NewGuid(), new List<Guid> { Guid.NewGuid() }, 10m, false)).IsValid.Should().BeTrue();
    [Fact] public void Empty_PoolId() => _v.Validate(new ExportPoolProductsToXmlCommand(Guid.Empty, new List<Guid> { Guid.NewGuid() }, 10m, false)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ImportFromFeedValidatorTests
{
    private readonly ImportFromFeedValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new ImportFromFeedCommand(Guid.NewGuid(), new List<string> { "SKU-001" }, 1.0m)).IsValid.Should().BeTrue();
    [Fact] public void Empty_FeedSourceId() => _v.Validate(new ImportFromFeedCommand(Guid.Empty, new List<string> { "SKU-001" }, 1.0m)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class PreviewFeedValidatorTests
{
    private readonly PreviewFeedValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new PreviewFeedCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_FeedSourceId() => _v.Validate(new PreviewFeedCommand(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class PullProductFromPoolValidatorTests
{
    private readonly PullProductFromPoolValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new PullProductFromPoolCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_PoolProductId() => _v.Validate(new PullProductFromPoolCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class RemoveProductFromPoolValidatorTests
{
    private readonly RemoveProductFromPoolValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new RemoveProductFromPoolCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_PoolProductId() => _v.Validate(new RemoveProductFromPoolCommand(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class ShareProductToPoolValidatorTests
{
    private readonly ShareProductToPoolValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new ShareProductToPoolCommand(Guid.NewGuid(), Guid.NewGuid(), 50m, null)).IsValid.Should().BeTrue();
    [Fact] public void Empty_ProductId() => _v.Validate(new ShareProductToPoolCommand(Guid.Empty, Guid.NewGuid(), 50m, null)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class SyncDropshipProductsValidatorTests
{
    private readonly SyncDropshipProductsValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new SyncDropshipProductsCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_SupplierId() => _v.Validate(new SyncDropshipProductsCommand(Guid.NewGuid(), Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class SyncSupplierPricesValidatorTests
{
    private readonly SyncSupplierPricesValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new SyncSupplierPricesCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_SupplierId() => _v.Validate(new SyncSupplierPricesCommand(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class TriggerFeedImportValidatorTests
{
    private readonly TriggerFeedImportValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new TriggerFeedImportCommand(Guid.NewGuid())).IsValid.Should().BeTrue();
    [Fact] public void Empty_FeedId() => _v.Validate(new TriggerFeedImportCommand(Guid.Empty)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateFeedSourceValidatorTests
{
    private readonly UpdateFeedSourceValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdateFeedSourceCommand(Guid.NewGuid(), "Feed v2", "https://feed.com/v2.xml", FeedFormat.Xml, 10m, 0m, 60, null, true, true)).IsValid.Should().BeTrue();
    [Fact] public void Empty_Id() => _v.Validate(new UpdateFeedSourceCommand(Guid.Empty, "N", "https://feed.com", FeedFormat.Xml, 0m, 0m, 60, null, false, true)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdatePoolProductReliabilityValidatorTests
{
    private readonly UpdatePoolProductReliabilityValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdatePoolProductReliabilityCommand(Guid.NewGuid(), 0.95m, ReliabilityColor.Green)).IsValid.Should().BeTrue();
    [Fact] public void Empty_PoolProductId() => _v.Validate(new UpdatePoolProductReliabilityCommand(Guid.Empty, 0.5m, ReliabilityColor.Yellow)).IsValid.Should().BeFalse();
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdatePoolProductStockValidatorTests
{
    private readonly UpdatePoolProductStockValidator _v = new();
    [Fact] public void Valid() => _v.Validate(new UpdatePoolProductStockCommand(Guid.NewGuid(), 100m)).IsValid.Should().BeTrue();
    [Fact] public void Empty_PoolProductId() => _v.Validate(new UpdatePoolProductStockCommand(Guid.Empty, 0m)).IsValid.Should().BeFalse();
}
