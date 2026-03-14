using FluentAssertions;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Tests.Unit.Domain.Crm;

/// <summary>
/// Tenant izolasyonu: A tenant'ının entity'si B tenant'ına ait değil.
/// </summary>
public class CrmMultiTenantTests
{
    [Fact]
    public void Lead_TenantId_ShouldMatchCreatorTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var leadA = Lead.Create(tenantA, "Tenant A Lead", LeadSource.Manual);
        var leadB = Lead.Create(tenantB, "Tenant B Lead", LeadSource.Web);

        leadA.TenantId.Should().Be(tenantA);
        leadB.TenantId.Should().Be(tenantB);
        leadA.TenantId.Should().NotBe(leadB.TenantId);
    }

    [Fact]
    public void Deal_TenantId_ShouldMatchCreatorTenant()
    {
        var tenantA = Guid.NewGuid();
        var pipelineId = Guid.NewGuid();
        var stageId = Guid.NewGuid();

        var deal = Deal.Create(tenantA, "Tenant A Deal", pipelineId, stageId, 1000m);
        deal.TenantId.Should().Be(tenantA);
    }

    [Fact]
    public void CrmContact_TenantId_ShouldMatchCreatorTenant()
    {
        var tenantA = Guid.NewGuid();
        var contact = CrmContact.Create(tenantA, "Ali Veli", ContactType.Individual);
        contact.TenantId.Should().Be(tenantA);
    }
}
