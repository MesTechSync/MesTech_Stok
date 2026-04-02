using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Entities.Documents;
using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class BillingInvoiceTests
{
    [Fact]
    public void Create_ValidInput_ShouldSetProperties()
    {
        var invoice = BillingInvoice.Create(Guid.NewGuid(), Guid.NewGuid(), "INV-001", 500m);
        invoice.Should().NotBeNull();
        invoice.Amount.Should().Be(500m);
    }

    [Fact]
    public void Create_ZeroAmount_ShouldThrow()
    {
        var act = () => BillingInvoice.Create(Guid.NewGuid(), Guid.NewGuid(), "INV-002", 0m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NegativeAmount_ShouldThrow()
    {
        var act = () => BillingInvoice.Create(Guid.NewGuid(), Guid.NewGuid(), "INV-003", -100m);
        act.Should().Throw<ArgumentException>();
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class DocumentFolderTests
{
    [Fact]
    public void Create_ValidInput_ShouldSetName()
    {
        var folder = DocumentFolder.Create(Guid.NewGuid(), "Faturalar");
        folder.Name.Should().Be("Faturalar");
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class ImportTemplateTests
{
    [Fact]
    public void Create_ValidInput_ShouldSetProperties()
    {
        var template = ImportTemplate.Create(Guid.NewGuid(), "Trendyol Import", "xlsx");
        template.Name.Should().Be("Trendyol Import");
    }

    [Fact]
    public void Create_EmptyTenantId_ShouldThrow()
    {
        var act = () => ImportTemplate.Create(Guid.Empty, "Test", "csv");
        act.Should().Throw<ArgumentException>();
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
public class ErpAccountMappingTests
{
    [Fact]
    public void Create_ValidInput_ShouldBeActive()
    {
        var mapping = ErpAccountMapping.Create(
            Guid.NewGuid(), ErpProvider.Parasut,
            "120", "Alicilar", "Asset", "120.01", "Musteriler");
        mapping.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetInactive()
    {
        var mapping = ErpAccountMapping.Create(
            Guid.NewGuid(), ErpProvider.Parasut, "120", "Test", "Asset", "120.01", "ERP");
        mapping.Deactivate();
        mapping.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivate_ShouldSetActive()
    {
        var mapping = ErpAccountMapping.Create(
            Guid.NewGuid(), ErpProvider.Parasut, "120", "Test", "Asset", "120.01", "ERP");
        mapping.Deactivate();
        mapping.Activate();
        mapping.IsActive.Should().BeTrue();
    }
}
