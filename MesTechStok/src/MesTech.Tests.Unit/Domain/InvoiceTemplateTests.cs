using MesTech.Domain.Common;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

public class InvoiceTemplateTests
{
    [Fact]
    public void InvoiceTemplate_Should_Implement_ITenantEntity()
    {
        var template = new InvoiceTemplate();
        Assert.IsAssignableFrom<ITenantEntity>(template);
    }

    [Fact]
    public void InvoiceTemplate_Should_Inherit_BaseEntity()
    {
        var template = new InvoiceTemplate();
        Assert.IsAssignableFrom<BaseEntity>(template);
    }

    [Fact]
    public void InvoiceTemplate_Default_Values()
    {
        var template = new InvoiceTemplate();
        Assert.False(template.ShowKargoBarkodu);
        Assert.False(template.ShowFaturaTutariYaziyla);
        Assert.False(template.IsDefault);
        Assert.Null(template.LogoImage);
        Assert.Null(template.SignatureImage);
    }
}
