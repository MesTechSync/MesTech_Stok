using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain.Entities;

/// <summary>
/// CRM domain entity tests: Customer, CustomerAccount, Lead, Deal, CrmContact,
/// Campaign, CampaignProduct, LoyaltyProgram, LoyaltyTransaction,
/// Pipeline, PipelineStage.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "CrmEntities")]
[Trait("Phase", "Dalga15")]
public class CrmEntityTests
{
    // ═══════════════════════════════════════════
    // Customer
    // ═══════════════════════════════════════════

    [Fact]
    public void Customer_Creation_SetsDefaults()
    {
        var customer = new Customer();

        customer.Id.Should().NotBe(Guid.Empty);
        customer.CustomerType.Should().Be("INDIVIDUAL");
        customer.Currency.Should().Be("TRY");
        customer.IsActive.Should().BeTrue();
        customer.IsBlocked.Should().BeFalse();
        customer.IsVip.Should().BeFalse();
    }

    [Fact]
    public void Customer_Block_WithReason_SetsBlockedState()
    {
        var customer = new Customer();
        customer.Block("Odeme sorunu");

        customer.IsBlocked.Should().BeTrue();
        customer.BlockReason.Should().Be("Odeme sorunu");
    }

    [Fact]
    public void Customer_Block_EmptyReason_Throws()
    {
        var customer = new Customer();
        var act = () => customer.Block("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Customer_Unblock_ClearsBlockedState()
    {
        var customer = new Customer();
        customer.Block("Sebep");
        customer.Unblock();

        customer.IsBlocked.Should().BeFalse();
        customer.BlockReason.Should().BeNull();
    }

    [Fact]
    public void Customer_AdjustBalance_AccumulatesCorrectly()
    {
        var customer = new Customer();
        customer.AdjustBalance(1000m);
        customer.AdjustBalance(-300m);

        customer.CurrentBalance.Should().Be(700m);
    }

    [Fact]
    public void Customer_RecordOrderPlaced_SetsLastOrderDate()
    {
        var customer = new Customer();
        customer.RecordOrderPlaced();

        customer.LastOrderDate.Should().NotBeNull();
        customer.LastOrderDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Customer_PromoteAndDemoteVip_TogglesFlag()
    {
        var customer = new Customer();
        customer.PromoteToVip();
        customer.IsVip.Should().BeTrue();

        customer.DemoteFromVip();
        customer.IsVip.Should().BeFalse();
    }

    [Fact]
    public void Customer_HasExceededCreditLimit_TrueWhenExceeded()
    {
        var customer = new Customer { CreditLimit = 500m };
        customer.AdjustBalance(600m);

        customer.HasExceededCreditLimit.Should().BeTrue();
    }

    [Fact]
    public void Customer_HasExceededCreditLimit_FalseWithinLimit()
    {
        var customer = new Customer { CreditLimit = 500m };
        customer.AdjustBalance(300m);

        customer.HasExceededCreditLimit.Should().BeFalse();
    }

    [Fact]
    public void Customer_HasExceededCreditLimit_FalseWhenNoLimit()
    {
        var customer = new Customer();
        customer.AdjustBalance(999999m);

        customer.HasExceededCreditLimit.Should().BeFalse();
    }

    [Fact]
    public void Customer_DisplayName_FallsBackToName()
    {
        var customer = new Customer { Name = "Firma A" };
        customer.DisplayName.Should().Be("Firma A");
    }

    [Fact]
    public void Customer_DisplayName_IncludesContactPerson()
    {
        var customer = new Customer { Name = "Firma A", ContactPerson = "Ali" };
        customer.DisplayName.Should().Be("Firma A (Ali)");
    }

    // ═══════════════════════════════════════════
    // CustomerAccount
    // ═══════════════════════════════════════════

    [Fact]
    public void CustomerAccount_Creation_SetsDefaults()
    {
        var ca = new CustomerAccount();
        ca.Id.Should().NotBe(Guid.Empty);
        ca.Currency.Should().Be("TRY");
        ca.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CustomerAccount_SetCreditLimit_ValidValue_Sets()
    {
        var ca = new CustomerAccount();
        ca.SetCreditLimit(10000m);
        ca.CreditLimit.Should().Be(10000m);
    }

    [Fact]
    public void CustomerAccount_SetCreditLimit_Negative_Throws()
    {
        var ca = new CustomerAccount();
        var act = () => ca.SetCreditLimit(-1m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CustomerAccount_ActivateDeactivate_TogglesState()
    {
        var ca = new CustomerAccount();
        ca.Deactivate();
        ca.IsActive.Should().BeFalse();
        ca.Activate();
        ca.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CustomerAccount_RecordSale_CreatesDebitTransaction()
    {
        var ca = new CustomerAccount { TenantId = Guid.NewGuid() };
        var invoiceId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var tx = ca.RecordSale(invoiceId, orderId, 1000m, "FTR-001");

        tx.Type.Should().Be(TransactionType.SalesInvoice);
        tx.DebitAmount.Should().Be(1000m);
        tx.CreditAmount.Should().Be(0m);
        tx.DocumentNumber.Should().Be("FTR-001");
        ca.Transactions.Should().HaveCount(1);
    }

    [Fact]
    public void CustomerAccount_RecordCollection_CreatesCreditTransaction()
    {
        var ca = new CustomerAccount { TenantId = Guid.NewGuid() };
        var tx = ca.RecordCollection(500m, "TAH-001");

        tx.Type.Should().Be(TransactionType.Collection);
        tx.CreditAmount.Should().Be(500m);
        tx.DebitAmount.Should().Be(0m);
    }

    [Fact]
    public void CustomerAccount_RecordReturn_CreatesCreditTransaction()
    {
        var ca = new CustomerAccount { TenantId = Guid.NewGuid() };
        var returnId = Guid.NewGuid();
        var tx = ca.RecordReturn(returnId, 200m, PlatformType.Trendyol);

        tx.Type.Should().Be(TransactionType.SalesReturn);
        tx.CreditAmount.Should().Be(200m);
        tx.Platform.Should().Be(PlatformType.Trendyol);
    }

    [Fact]
    public void CustomerAccount_RecordCommission_CreatesDebitTransaction()
    {
        var ca = new CustomerAccount { TenantId = Guid.NewGuid() };
        var orderId = Guid.NewGuid();
        var tx = ca.RecordCommission(orderId, 50m, PlatformType.Hepsiburada);

        tx.Type.Should().Be(TransactionType.PlatformCommission);
        tx.DebitAmount.Should().Be(50m);
    }

    [Fact]
    public void CustomerAccount_Balance_ComputedFromTransactions()
    {
        var ca = new CustomerAccount { TenantId = Guid.NewGuid() };
        ca.RecordSale(Guid.NewGuid(), Guid.NewGuid(), 1000m, "FTR-001");
        ca.RecordCollection(400m);

        ca.Balance.Should().Be(600m);
    }

    [Fact]
    public void CustomerAccount_HasExceededCreditLimit_WhenBalanceExceedsLimit()
    {
        var ca = new CustomerAccount { TenantId = Guid.NewGuid() };
        ca.SetCreditLimit(500m);
        ca.RecordSale(Guid.NewGuid(), Guid.NewGuid(), 600m, "FTR-001");

        ca.HasExceededCreditLimit.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // Lead
    // ═══════════════════════════════════════════

    [Fact]
    public void Lead_Create_SetsProperties()
    {
        var tenantId = Guid.NewGuid();
        var lead = Lead.Create(tenantId, "Ali Veli", LeadSource.Web, "ali@test.com");

        lead.Id.Should().NotBe(Guid.Empty);
        lead.TenantId.Should().Be(tenantId);
        lead.FullName.Should().Be("Ali Veli");
        lead.Source.Should().Be(LeadSource.Web);
        lead.Status.Should().Be(LeadStatus.New);
        lead.Email.Should().Be("ali@test.com");
    }

    [Fact]
    public void Lead_Create_EmptyName_Throws()
    {
        var act = () => Lead.Create(Guid.NewGuid(), "", LeadSource.Web);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Lead_MarkAsContacted_FromNew_Succeeds()
    {
        var lead = Lead.Create(Guid.NewGuid(), "Test", LeadSource.Web);
        lead.MarkAsContacted("Gorustuk");

        lead.Status.Should().Be(LeadStatus.Contacted);
        lead.ContactedAt.Should().NotBeNull();
        lead.Notes.Should().Be("Gorustuk");
    }

    [Fact]
    public void Lead_MarkAsContacted_FromConverted_Throws()
    {
        var lead = Lead.Create(Guid.NewGuid(), "Test", LeadSource.Web);
        lead.Convert();

        var act = () => lead.MarkAsContacted();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Lead_Qualify_SetsStatusAndNotes()
    {
        var lead = Lead.Create(Guid.NewGuid(), "Test", LeadSource.Web);
        lead.Qualify("Buyuk musteri potansiyeli");

        lead.Status.Should().Be(LeadStatus.Qualified);
        lead.Notes.Should().Be("Buyuk musteri potansiyeli");
    }

    [Fact]
    public void Lead_Qualify_EmptyNotes_Throws()
    {
        var lead = Lead.Create(Guid.NewGuid(), "Test", LeadSource.Web);
        var act = () => lead.Qualify("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Lead_MarkAsLost_SetsStatusAndReason()
    {
        var lead = Lead.Create(Guid.NewGuid(), "Test", LeadSource.Web);
        lead.MarkAsLost("Butce yok");

        lead.Status.Should().Be(LeadStatus.Lost);
        lead.Notes.Should().Be("Butce yok");
    }

    [Fact]
    public void Lead_Convert_ReturnsContactIdAndRaisesEvent()
    {
        var lead = Lead.Create(Guid.NewGuid(), "Test", LeadSource.Web);
        var contactId = lead.Convert();

        contactId.Should().NotBe(Guid.Empty);
        lead.Status.Should().Be(LeadStatus.Converted);
        lead.ConvertedAt.Should().NotBeNull();
        lead.ConvertedToCrmContactId.Should().Be(contactId);
        lead.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void Lead_Convert_AlreadyConverted_Throws()
    {
        var lead = Lead.Create(Guid.NewGuid(), "Test", LeadSource.Web);
        lead.Convert();

        var act = () => lead.Convert();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Lead_Convert_Lost_Throws()
    {
        var lead = Lead.Create(Guid.NewGuid(), "Test", LeadSource.Web);
        lead.MarkAsLost("Sebep");

        var act = () => lead.Convert();
        act.Should().Throw<InvalidOperationException>();
    }

    // ═══════════════════════════════════════════
    // Deal
    // ═══════════════════════════════════════════

    [Fact]
    public void Deal_Create_SetsProperties()
    {
        var tenantId = Guid.NewGuid();
        var pipelineId = Guid.NewGuid();
        var stageId = Guid.NewGuid();

        var deal = Deal.Create(tenantId, "Buyuk Satis", pipelineId, stageId, 50000m);

        deal.Id.Should().NotBe(Guid.Empty);
        deal.Title.Should().Be("Buyuk Satis");
        deal.Amount.Should().Be(50000m);
        deal.Status.Should().Be(DealStatus.Open);
        deal.PipelineId.Should().Be(pipelineId);
        deal.StageId.Should().Be(stageId);
    }

    [Fact]
    public void Deal_Create_EmptyTitle_Throws()
    {
        var act = () => Deal.Create(Guid.NewGuid(), "", Guid.NewGuid(), Guid.NewGuid(), 100m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deal_Create_NegativeAmount_Throws()
    {
        var act = () => Deal.Create(Guid.NewGuid(), "Test", Guid.NewGuid(), Guid.NewGuid(), -1m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Deal_MoveToStage_FromOpen_ChangesStageAndRaisesEvent()
    {
        var deal = Deal.Create(Guid.NewGuid(), "Test", Guid.NewGuid(), Guid.NewGuid(), 100m);
        var newStageId = Guid.NewGuid();

        deal.MoveToStage(newStageId);

        deal.StageId.Should().Be(newStageId);
        deal.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void Deal_MoveToStage_FromWon_Throws()
    {
        var deal = Deal.Create(Guid.NewGuid(), "Test", Guid.NewGuid(), Guid.NewGuid(), 100m);
        deal.MarkAsWon();

        var act = () => deal.MoveToStage(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Deal_MarkAsWon_SetsStatusAndRaisesEvent()
    {
        var deal = Deal.Create(Guid.NewGuid(), "Test", Guid.NewGuid(), Guid.NewGuid(), 100m);
        var orderId = Guid.NewGuid();
        deal.MarkAsWon(orderId);

        deal.Status.Should().Be(DealStatus.Won);
        deal.OrderId.Should().Be(orderId);
        deal.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void Deal_MarkAsWon_NotOpen_Throws()
    {
        var deal = Deal.Create(Guid.NewGuid(), "Test", Guid.NewGuid(), Guid.NewGuid(), 100m);
        deal.MarkAsWon();

        var act = () => deal.MarkAsWon();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Deal_MarkAsLost_SetsStatusAndReasonAndRaisesEvent()
    {
        var deal = Deal.Create(Guid.NewGuid(), "Test", Guid.NewGuid(), Guid.NewGuid(), 100m);
        deal.MarkAsLost("Rakip kazandi");

        deal.Status.Should().Be(DealStatus.Lost);
        deal.LostReason.Should().Be("Rakip kazandi");
        deal.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void Deal_MarkAsLost_EmptyReason_Throws()
    {
        var deal = Deal.Create(Guid.NewGuid(), "Test", Guid.NewGuid(), Guid.NewGuid(), 100m);
        var act = () => deal.MarkAsLost("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deal_UpdateAmount_ValidValue_SetsAmount()
    {
        var deal = Deal.Create(Guid.NewGuid(), "Test", Guid.NewGuid(), Guid.NewGuid(), 100m);
        deal.UpdateAmount(200m);
        deal.Amount.Should().Be(200m);
    }

    [Fact]
    public void Deal_UpdateAmount_Negative_Throws()
    {
        var deal = Deal.Create(Guid.NewGuid(), "Test", Guid.NewGuid(), Guid.NewGuid(), 100m);
        var act = () => deal.UpdateAmount(-1m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Deal_LinkOrder_SetsOrderId()
    {
        var deal = Deal.Create(Guid.NewGuid(), "Test", Guid.NewGuid(), Guid.NewGuid(), 100m);
        var orderId = Guid.NewGuid();
        deal.LinkOrder(orderId);
        deal.OrderId.Should().Be(orderId);
    }

    // ═══════════════════════════════════════════
    // CrmContact
    // ═══════════════════════════════════════════

    [Fact]
    public void CrmContact_Create_SetsProperties()
    {
        var tenantId = Guid.NewGuid();
        var contact = CrmContact.Create(tenantId, "Ahmet Yilmaz", ContactType.Individual, "ahmet@test.com");

        contact.Id.Should().NotBe(Guid.Empty);
        contact.FullName.Should().Be("Ahmet Yilmaz");
        contact.Type.Should().Be(ContactType.Individual);
        contact.Email.Should().Be("ahmet@test.com");
        contact.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CrmContact_Create_EmptyName_Throws()
    {
        var act = () => CrmContact.Create(Guid.NewGuid(), "", ContactType.Individual);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CrmContact_CreateFromLead_CopiesLeadData()
    {
        var lead = Lead.Create(Guid.NewGuid(), "Ali Veli", LeadSource.Web, "ali@test.com", "555", "Firma X");
        var contactId = Guid.NewGuid();

        var contact = CrmContact.CreateFromLead(lead, contactId);

        contact.Id.Should().Be(contactId);
        contact.FullName.Should().Be("Ali Veli");
        contact.Email.Should().Be("ali@test.com");
        contact.Phone.Should().Be("555");
        contact.Company.Should().Be("Firma X");
        contact.Type.Should().Be(ContactType.Company);
        contact.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CrmContact_CreateFromLead_NoCompany_SetsIndividual()
    {
        var lead = Lead.Create(Guid.NewGuid(), "Ali", LeadSource.Web);
        var contact = CrmContact.CreateFromLead(lead, Guid.NewGuid());
        contact.Type.Should().Be(ContactType.Individual);
    }

    [Fact]
    public void CrmContact_LinkToCustomer_SetsCustomerId()
    {
        var contact = CrmContact.Create(Guid.NewGuid(), "Test", ContactType.Individual);
        var customerId = Guid.NewGuid();
        contact.LinkToCustomer(customerId);
        contact.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public void CrmContact_Deactivate_SetsInactive()
    {
        var contact = CrmContact.Create(Guid.NewGuid(), "Test", ContactType.Individual);
        contact.Deactivate();
        contact.IsActive.Should().BeFalse();
    }

    [Fact]
    public void CrmContact_UpdateNotes_SetsNotes()
    {
        var contact = CrmContact.Create(Guid.NewGuid(), "Test", ContactType.Individual);
        contact.UpdateNotes("Onemli musteri");
        contact.Notes.Should().Be("Onemli musteri");
    }

    // ═══════════════════════════════════════════
    // Campaign
    // ═══════════════════════════════════════════

    [Fact]
    public void Campaign_Create_SetsProperties()
    {
        var start = DateTime.UtcNow;
        var end = start.AddDays(30);

        var campaign = Campaign.Create(Guid.NewGuid(), "Yaz Kampanyasi", start, end, 15m);

        campaign.Id.Should().NotBe(Guid.Empty);
        campaign.Name.Should().Be("Yaz Kampanyasi");
        campaign.DiscountPercent.Should().Be(15m);
        campaign.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Campaign_Create_EmptyName_Throws()
    {
        var act = () => Campaign.Create(Guid.NewGuid(), "", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Campaign_Create_EndBeforeStart_Throws()
    {
        var start = DateTime.UtcNow;
        var act = () => Campaign.Create(Guid.NewGuid(), "Test", start, start.AddSeconds(-1), 10m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Campaign_Create_ZeroDiscount_Throws()
    {
        var act = () => Campaign.Create(Guid.NewGuid(), "Test", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 0m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Campaign_Create_OverHundredDiscount_Throws()
    {
        var act = () => Campaign.Create(Guid.NewGuid(), "Test", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 101m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Campaign_AddProduct_IncreasesCount()
    {
        var campaign = Campaign.Create(Guid.NewGuid(), "Test", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10m);
        var product = CampaignProduct.Create(campaign.Id, Guid.NewGuid());

        campaign.AddProduct(product);

        campaign.Products.Should().HaveCount(1);
    }

    [Fact]
    public void Campaign_Deactivate_SetsInactive()
    {
        var campaign = Campaign.Create(Guid.NewGuid(), "Test", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 10m);
        campaign.Deactivate();
        campaign.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Campaign_IsCurrentlyActive_TrueWhenWithinDateRange()
    {
        var campaign = Campaign.Create(
            Guid.NewGuid(), "Test",
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1),
            10m);

        campaign.IsCurrentlyActive().Should().BeTrue();
    }

    [Fact]
    public void Campaign_IsCurrentlyActive_FalseWhenExpired()
    {
        var campaign = Campaign.Create(
            Guid.NewGuid(), "Test",
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow.AddDays(-1),
            10m);

        campaign.IsCurrentlyActive().Should().BeFalse();
    }

    [Fact]
    public void Campaign_IsCurrentlyActive_FalseWhenDeactivated()
    {
        var campaign = Campaign.Create(
            Guid.NewGuid(), "Test",
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1),
            10m);
        campaign.Deactivate();

        campaign.IsCurrentlyActive().Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // CampaignProduct
    // ═══════════════════════════════════════════

    [Fact]
    public void CampaignProduct_Create_SetsProperties()
    {
        var campaignId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var cp = CampaignProduct.Create(campaignId, productId);

        cp.Id.Should().NotBe(Guid.Empty);
        cp.CampaignId.Should().Be(campaignId);
        cp.ProductId.Should().Be(productId);
    }

    // ═══════════════════════════════════════════
    // LoyaltyProgram
    // ═══════════════════════════════════════════

    [Fact]
    public void LoyaltyProgram_Create_SetsProperties()
    {
        var lp = LoyaltyProgram.Create(Guid.NewGuid(), "Sadakat Plus", 2.5m, 100);

        lp.Id.Should().NotBe(Guid.Empty);
        lp.Name.Should().Be("Sadakat Plus");
        lp.PointsPerPurchase.Should().Be(2.5m);
        lp.MinRedeemPoints.Should().Be(100);
        lp.IsActive.Should().BeTrue();
    }

    [Fact]
    public void LoyaltyProgram_Create_EmptyName_Throws()
    {
        var act = () => LoyaltyProgram.Create(Guid.NewGuid(), "", 1m, 10);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LoyaltyProgram_Create_ZeroPoints_Throws()
    {
        var act = () => LoyaltyProgram.Create(Guid.NewGuid(), "Test", 0m, 10);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LoyaltyProgram_Create_ZeroMinRedeem_Throws()
    {
        var act = () => LoyaltyProgram.Create(Guid.NewGuid(), "Test", 1m, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LoyaltyProgram_UpdateRules_SetsNewValues()
    {
        var lp = LoyaltyProgram.Create(Guid.NewGuid(), "Test", 1m, 50);
        lp.UpdateRules(3m, 200);

        lp.PointsPerPurchase.Should().Be(3m);
        lp.MinRedeemPoints.Should().Be(200);
    }

    [Fact]
    public void LoyaltyProgram_UpdateRules_ZeroPoints_Throws()
    {
        var lp = LoyaltyProgram.Create(Guid.NewGuid(), "Test", 1m, 50);
        var act = () => lp.UpdateRules(0m, 50);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LoyaltyProgram_Deactivate_SetsInactive()
    {
        var lp = LoyaltyProgram.Create(Guid.NewGuid(), "Test", 1m, 50);
        lp.Deactivate();
        lp.IsActive.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // LoyaltyTransaction
    // ═══════════════════════════════════════════

    [Fact]
    public void LoyaltyTransaction_Create_SetsProperties()
    {
        var lt = LoyaltyTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 50, LoyaltyTransactionType.Earn, "Satin alma");

        lt.Id.Should().NotBe(Guid.Empty);
        lt.Points.Should().Be(50);
        lt.Type.Should().Be(LoyaltyTransactionType.Earn);
        lt.Description.Should().Be("Satin alma");
    }

    [Fact]
    public void LoyaltyTransaction_Create_ZeroPoints_Throws()
    {
        var act = () => LoyaltyTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, LoyaltyTransactionType.Earn);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ═══════════════════════════════════════════
    // Pipeline
    // ═══════════════════════════════════════════

    [Fact]
    public void Pipeline_Create_SetsProperties()
    {
        var pipeline = Pipeline.Create(Guid.NewGuid(), "Satis Pipeline", true, 1);

        pipeline.Id.Should().NotBe(Guid.Empty);
        pipeline.Name.Should().Be("Satis Pipeline");
        pipeline.IsDefault.Should().BeTrue();
        pipeline.Position.Should().Be(1);
    }

    [Fact]
    public void Pipeline_Create_EmptyName_Throws()
    {
        var act = () => Pipeline.Create(Guid.NewGuid(), "", false, 0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Pipeline_Rename_SetsNewName()
    {
        var pipeline = Pipeline.Create(Guid.NewGuid(), "Eski", false, 0);
        pipeline.Rename("Yeni Pipeline");
        pipeline.Name.Should().Be("Yeni Pipeline");
    }

    [Fact]
    public void Pipeline_Rename_EmptyName_Throws()
    {
        var pipeline = Pipeline.Create(Guid.NewGuid(), "Test", false, 0);
        var act = () => pipeline.Rename("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Pipeline_SetAsDefault_SetsFlag()
    {
        var pipeline = Pipeline.Create(Guid.NewGuid(), "Test", false, 0);
        pipeline.SetAsDefault();
        pipeline.IsDefault.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // PipelineStage
    // ═══════════════════════════════════════════

    [Fact]
    public void PipelineStage_Create_SetsProperties()
    {
        var stage = PipelineStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Teklif", 2, 50m, StageType.Normal, "#FF0000");

        stage.Id.Should().NotBe(Guid.Empty);
        stage.Name.Should().Be("Teklif");
        stage.Position.Should().Be(2);
        stage.Probability.Should().Be(50m);
        stage.Color.Should().Be("#FF0000");
    }

    [Fact]
    public void PipelineStage_Create_EmptyName_Throws()
    {
        var act = () => PipelineStage.Create(Guid.NewGuid(), Guid.NewGuid(), "", 0, null, StageType.Normal);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PipelineStage_Create_NegativeProbability_Throws()
    {
        var act = () => PipelineStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test", 0, -1m, StageType.Normal);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PipelineStage_Create_ProbabilityOver100_Throws()
    {
        var act = () => PipelineStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test", 0, 101m, StageType.Normal);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PipelineStage_UpdatePosition_SetsNewPosition()
    {
        var stage = PipelineStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test", 1, null, StageType.Normal);
        stage.UpdatePosition(5);
        stage.Position.Should().Be(5);
    }
}
