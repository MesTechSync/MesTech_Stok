using FluentAssertions;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Events.Crm;
using Xunit;

namespace MesTech.Tests.Unit.Domain.Crm;

public class LeadTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    // ── CREATE ──────────────────────────────────────────────────────
    [Fact]
    public void Create_WithValidData_ShouldSetStatusToNew()
    {
        var lead = Lead.Create(_tenantId, "Ahmet Yılmaz", LeadSource.WhatsApp);
        lead.Status.Should().Be(LeadStatus.New);
    }

    [Fact]
    public void Create_WithValidData_ShouldGenerateNewId()
    {
        var lead = Lead.Create(_tenantId, "Fatma Kaya", LeadSource.Web);
        lead.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ShouldThrow(string? name)
    {
        var act = () => Lead.Create(_tenantId, name!, LeadSource.Manual);
        act.Should().Throw<ArgumentException>();
    }

    // ── CONTACT ─────────────────────────────────────────────────────
    [Fact]
    public void MarkAsContacted_FromNewStatus_ShouldSucceed()
    {
        var lead = Lead.Create(_tenantId, "Test Lead", LeadSource.Manual);
        lead.MarkAsContacted("Arandı, cevap vermedi.");
        lead.Status.Should().Be(LeadStatus.Contacted);
        lead.ContactedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsContacted_FromConvertedStatus_ShouldThrow()
    {
        var lead = Lead.Create(_tenantId, "Test Lead", LeadSource.Manual);
        lead.Convert();
        var act = () => lead.MarkAsContacted();
        act.Should().Throw<InvalidOperationException>();
    }

    // ── QUALIFY ──────────────────────────────────────────────────────
    [Fact]
    public void Qualify_WithNotes_ShouldSetStatusToQualified()
    {
        var lead = Lead.Create(_tenantId, "Test Lead", LeadSource.Web);
        lead.Qualify("Bütçesi var, karar vericisi o.");
        lead.Status.Should().Be(LeadStatus.Qualified);
        lead.Notes.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Qualify_WithEmptyNotes_ShouldThrow()
    {
        var lead = Lead.Create(_tenantId, "Test Lead", LeadSource.Web);
        var act = () => lead.Qualify(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    // ── CONVERT ──────────────────────────────────────────────────────
    [Fact]
    public void Convert_FromQualifiedStatus_ShouldReturnContactId()
    {
        var lead = Lead.Create(_tenantId, "Test Lead", LeadSource.Web);
        lead.Qualify("Uygun müşteri.");
        var contactId = lead.Convert();
        contactId.Should().NotBeEmpty();
        lead.Status.Should().Be(LeadStatus.Converted);
        lead.ConvertedToCrmContactId.Should().Be(contactId);
    }

    [Fact]
    public void Convert_AlreadyConverted_ShouldThrow()
    {
        var lead = Lead.Create(_tenantId, "Test Lead", LeadSource.Manual);
        lead.Convert();
        var act = () => lead.Convert();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Convert_FromLostStatus_ShouldThrow()
    {
        var lead = Lead.Create(_tenantId, "Test Lead", LeadSource.Manual);
        lead.MarkAsLost("İlgilenmiyor.");
        var act = () => lead.Convert();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Convert_ShouldRaiseLeadConvertedEvent()
    {
        var lead = Lead.Create(_tenantId, "Test Lead", LeadSource.Web);
        lead.Convert();
        lead.DomainEvents.Should().ContainSingle(e => e is LeadConvertedEvent);
    }

    // ── LOST ─────────────────────────────────────────────────────────
    [Fact]
    public void MarkAsLost_WithReason_ShouldSetStatusToLost()
    {
        var lead = Lead.Create(_tenantId, "Test Lead", LeadSource.Manual);
        lead.MarkAsLost("Rakip tercih edildi.");
        lead.Status.Should().Be(LeadStatus.Lost);
        lead.Notes.Should().Contain("Rakip");
    }

    [Fact]
    public void MarkAsLost_WithEmptyReason_ShouldThrow()
    {
        var lead = Lead.Create(_tenantId, "Test Lead", LeadSource.Manual);
        var act = () => lead.MarkAsLost(string.Empty);
        act.Should().Throw<ArgumentException>();
    }
}
