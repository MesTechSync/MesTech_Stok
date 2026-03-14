using FluentAssertions;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Events.Crm;
using Xunit;

namespace MesTech.Tests.Unit.Domain.Crm;

/// <summary>
/// Lead -&gt; CrmContact -&gt; Deal -&gt; kazanildi tam akis domain testi.
/// Altyapi yok — sadece domain nesneleri.
/// DEV 5 — H27-5.5 (emirname Task 5.5 uyarlanmis gercek entity'ye gore)
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "Crm")]
public class CrmLeadToDealFlowTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _pipelineId = Guid.NewGuid();
    private static readonly Guid _stageId1 = Guid.NewGuid();
    private static readonly Guid _stageId2 = Guid.NewGuid();
    private static readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void FullFlow_Lead_Qualify_Convert_CreateDeal_Win_ShouldSucceed()
    {
        // 1. Lead olustur
        var lead = Lead.Create(_tenantId, "Mehmet Yildiz", LeadSource.WhatsApp,
            email: "mehmet@example.com", phone: "0532 111 2233");
        lead.Status.Should().Be(LeadStatus.New);

        // 2. Iletisime gec
        lead.MarkAsContacted("WhatsApp'tan mesaj attim, olumlu geri donus bekliyorum.");
        lead.Status.Should().Be(LeadStatus.Contacted);
        lead.ContactedAt.Should().NotBeNull();

        // 3. Niteliklendir
        lead.Qualify("Butcesi var, Mart sonuna kadar karar verecek. B2B musteri.");
        lead.Status.Should().Be(LeadStatus.Qualified);

        // 4. Convert -> CrmContact
        var contactId = lead.Convert();
        lead.Status.Should().Be(LeadStatus.Converted);
        lead.ConvertedToCrmContactId.Should().Be(contactId);
        lead.DomainEvents.Should().ContainSingle(e => e is LeadConvertedEvent);

        var contact = CrmContact.CreateFromLead(lead, contactId);
        contact.Id.Should().Be(contactId);
        contact.FullName.Should().Be("Mehmet Yildiz");
        contact.Email.Should().Be("mehmet@example.com");

        // 5. Deal olustur
        var deal = Deal.Create(_tenantId, "MesTech Lisans — Mehmet Yildiz",
            _pipelineId, _stageId1, 12000m, contactId);
        deal.Status.Should().Be(DealStatus.Open);
        deal.CrmContactId.Should().Be(contactId);

        // 6. Stage ilerlet
        deal.MoveToStage(_stageId2);
        deal.StageId.Should().Be(_stageId2);
        deal.DomainEvents.OfType<DealStageChangedEvent>().Should().HaveCount(1);

        // 7. Deal kazanildi
        var orderId = Guid.NewGuid();
        deal.MarkAsWon(orderId);
        deal.Status.Should().Be(DealStatus.Won);
        deal.OrderId.Should().Be(orderId);
        deal.DomainEvents.OfType<DealWonEvent>().Single().Amount.Should().Be(12000m);
    }

    [Fact]
    public void FullFlow_Lead_Lost_ShouldNotBeConvertible()
    {
        var lead = Lead.Create(_tenantId, "Ali Demir", LeadSource.Web);
        lead.MarkAsContacted();
        lead.MarkAsLost("Rakibi tercih etti.");

        lead.Status.Should().Be(LeadStatus.Lost);
        var act = () => lead.Convert();
        act.Should().Throw<InvalidOperationException>("Lost lead donusturulemez");
    }

    [Fact]
    public void Deal_LostAfterMultipleStages_ShouldPreserveHistory()
    {
        var deal = Deal.Create(_tenantId, "Buyuk Proje", _pipelineId, _stageId1, 50000m);
        deal.MoveToStage(_stageId2);
        deal.MarkAsLost("Proje iptal edildi.");

        deal.Status.Should().Be(DealStatus.Lost);
        deal.LostReason.Should().Contain("iptal");
        deal.DomainEvents.OfType<DealStageChangedEvent>().Should().HaveCount(1);
        deal.DomainEvents.OfType<DealLostEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void Activity_ShouldLinkToMultipleEntities()
    {
        var lead = Lead.Create(_tenantId, "Test", LeadSource.Manual);
        var contactId = lead.Convert();
        var deal = Deal.Create(_tenantId, "Test Deal", _pipelineId, _stageId1, 1000m, contactId);

        var activity = Activity.Create(
            _tenantId, ActivityType.Call, "Ilk gorusme",
            createdByUserId: _userId, crmContactId: contactId,
            dealId: deal.Id, leadId: lead.Id);

        activity.CrmContactId.Should().Be(contactId);
        activity.DealId.Should().Be(deal.Id);
        activity.LeadId.Should().Be(lead.Id);
    }

    [Fact]
    public void Lead_Convert_ShouldRaiseLeadConvertedEvent()
    {
        var lead = Lead.Create(_tenantId, "Test User", LeadSource.Web);
        lead.MarkAsContacted();
        lead.Convert();

        lead.DomainEvents.OfType<LeadConvertedEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void Deal_MarkAsWon_ClosedDeal_ShouldThrow()
    {
        var deal = Deal.Create(_tenantId, "Already Won Deal", _pipelineId, _stageId1, 5000m);
        deal.MarkAsWon();

        var act = () => deal.MarkAsWon();
        act.Should().Throw<InvalidOperationException>("Kapali deal tekrar kazanilabilir olmamali");
    }
}
