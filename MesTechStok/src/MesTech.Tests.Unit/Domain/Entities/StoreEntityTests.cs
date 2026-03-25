using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Domain.Entities;

/// <summary>
/// Store/Tenant/Supplier domain entity tests: Store, StoreCredential, Tenant,
/// TenantSubscription, SubscriptionPlan, Supplier, SupplierAccount, SupplierFeed,
/// Category, Brand, ProductVariant, ProductPlatformMapping.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "StoreEntities")]
[Trait("Phase", "Dalga15")]
public class StoreEntityTests
{
    // ═══════════════════════════════════════════
    // Store
    // ═══════════════════════════════════════════

    [Fact]
    public void Store_Creation_SetsDefaults()
    {
        var store = new Store();
        store.Id.Should().NotBe(Guid.Empty);
        store.IsActive.Should().BeTrue();
        store.StoreName.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════
    // StoreCredential
    // ═══════════════════════════════════════════

    [Fact]
    public void StoreCredential_Creation_SetsDefaults()
    {
        var cred = new StoreCredential();
        cred.Id.Should().NotBe(Guid.Empty);
        cred.Key.Should().BeEmpty();
        cred.EncryptedValue.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════
    // Tenant
    // ═══════════════════════════════════════════

    [Fact]
    public void Tenant_Creation_SetsDefaults()
    {
        var tenant = new Tenant();
        tenant.Id.Should().NotBe(Guid.Empty);
        tenant.IsActive.Should().BeTrue();
        tenant.Name.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════
    // TenantSubscription
    // ═══════════════════════════════════════════

    [Fact]
    public void TenantSubscription_StartTrial_SetsTrialState()
    {
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        var sub = TenantSubscription.StartTrial(tenantId, planId, 14);

        sub.Id.Should().NotBe(Guid.Empty);
        sub.TenantId.Should().Be(tenantId);
        sub.PlanId.Should().Be(planId);
        sub.Status.Should().Be(SubscriptionStatus.Trial);
        sub.Period.Should().Be(BillingPeriod.Monthly);
        sub.TrialEndsAt.Should().NotBeNull();
        sub.TrialEndsAt!.Value.Should().BeCloseTo(DateTime.UtcNow.AddDays(14), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void TenantSubscription_Activate_SetsActiveState()
    {
        var sub = TenantSubscription.Activate(Guid.NewGuid(), Guid.NewGuid(), BillingPeriod.Monthly);

        sub.Status.Should().Be(SubscriptionStatus.Active);
        sub.NextBillingDate.Should().NotBeNull();
        sub.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void TenantSubscription_Activate_Annual_SetsYearlyBilling()
    {
        var sub = TenantSubscription.Activate(Guid.NewGuid(), Guid.NewGuid(), BillingPeriod.Annual);

        sub.Period.Should().Be(BillingPeriod.Annual);
        sub.NextBillingDate!.Value.Should().BeCloseTo(DateTime.UtcNow.AddYears(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void TenantSubscription_Renew_SetsActiveAndUpdatesNextBilling()
    {
        var sub = TenantSubscription.Activate(Guid.NewGuid(), Guid.NewGuid(), BillingPeriod.Monthly);
        sub.MarkPastDue();
        sub.Renew();

        sub.Status.Should().Be(SubscriptionStatus.Active);
        sub.NextBillingDate.Should().NotBeNull();
    }

    [Fact]
    public void TenantSubscription_MarkPastDue_SetsStatus()
    {
        var sub = TenantSubscription.Activate(Guid.NewGuid(), Guid.NewGuid(), BillingPeriod.Monthly);
        sub.ClearDomainEvents();
        sub.MarkPastDue();

        sub.Status.Should().Be(SubscriptionStatus.PastDue);
    }

    [Fact]
    public void TenantSubscription_Cancel_SetsStatusAndReason()
    {
        var sub = TenantSubscription.Activate(Guid.NewGuid(), Guid.NewGuid(), BillingPeriod.Monthly);
        sub.ClearDomainEvents();

        sub.Cancel("Cok pahali");

        sub.Status.Should().Be(SubscriptionStatus.Cancelled);
        sub.CancellationReason.Should().Be("Cok pahali");
        sub.CancelledAt.Should().NotBeNull();
        sub.EndDate.Should().NotBeNull();
        sub.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void TenantSubscription_Expire_SetsStatus()
    {
        var sub = TenantSubscription.Activate(Guid.NewGuid(), Guid.NewGuid(), BillingPeriod.Monthly);
        sub.Expire();

        sub.Status.Should().Be(SubscriptionStatus.Expired);
        sub.EndDate.Should().NotBeNull();
    }

    [Fact]
    public void TenantSubscription_ConvertFromTrial_Succeeds()
    {
        var sub = TenantSubscription.StartTrial(Guid.NewGuid(), Guid.NewGuid());
        sub.ConvertFromTrial(BillingPeriod.Annual);

        sub.Status.Should().Be(SubscriptionStatus.Active);
        sub.Period.Should().Be(BillingPeriod.Annual);
        sub.TrialEndsAt.Should().BeNull();
    }

    [Fact]
    public void TenantSubscription_ConvertFromTrial_NotTrial_Throws()
    {
        var sub = TenantSubscription.Activate(Guid.NewGuid(), Guid.NewGuid(), BillingPeriod.Monthly);
        var act = () => sub.ConvertFromTrial(BillingPeriod.Annual);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TenantSubscription_IsExpired_TrueWhenExpiredStatus()
    {
        var sub = TenantSubscription.Activate(Guid.NewGuid(), Guid.NewGuid(), BillingPeriod.Monthly);
        sub.Expire();

        sub.IsExpired.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    // SubscriptionPlan
    // ═══════════════════════════════════════════

    [Fact]
    public void SubscriptionPlan_Create_SetsProperties()
    {
        var plan = SubscriptionPlan.Create("Baslangic", 299m, 2990m, 1, 500, 1);

        plan.Id.Should().NotBe(Guid.Empty);
        plan.Name.Should().Be("Baslangic");
        plan.MonthlyPrice.Should().Be(299m);
        plan.AnnualPrice.Should().Be(2990m);
        plan.MaxStores.Should().Be(1);
        plan.MaxProducts.Should().Be(500);
        plan.MaxUsers.Should().Be(1);
        plan.IsActive.Should().BeTrue();
        plan.TrialDays.Should().Be(14);
    }

    [Fact]
    public void SubscriptionPlan_Create_EmptyName_Throws()
    {
        var act = () => SubscriptionPlan.Create("", 100m, 1000m, 1, 1, 1);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SubscriptionPlan_SeedBasic_ReturnsValidPlan()
    {
        var plan = SubscriptionPlan.SeedBasic();
        plan.Name.Should().Be("Baslangic");
        plan.MaxStores.Should().Be(1);
    }

    [Fact]
    public void SubscriptionPlan_SeedProfessional_ReturnsValidPlan()
    {
        var plan = SubscriptionPlan.SeedProfessional();
        plan.Name.Should().Be("Profesyonel");
        plan.MaxStores.Should().Be(5);
    }

    [Fact]
    public void SubscriptionPlan_SeedEnterprise_ReturnsValidPlan()
    {
        var plan = SubscriptionPlan.SeedEnterprise();
        plan.Name.Should().Be("Kurumsal");
        plan.MaxStores.Should().Be(int.MaxValue);
    }

    [Fact]
    public void SubscriptionPlan_Deactivate_SetsInactive()
    {
        var plan = SubscriptionPlan.Create("Test", 100m, 1000m, 1, 1, 1);
        plan.Deactivate();
        plan.IsActive.Should().BeFalse();
    }

    [Fact]
    public void SubscriptionPlan_UpdatePricing_SetsNewPrices()
    {
        var plan = SubscriptionPlan.Create("Test", 100m, 1000m, 1, 1, 1);
        plan.UpdatePricing(200m, 2000m);

        plan.MonthlyPrice.Should().Be(200m);
        plan.AnnualPrice.Should().Be(2000m);
    }

    // ═══════════════════════════════════════════
    // Supplier
    // ═══════════════════════════════════════════

    [Fact]
    public void Supplier_Creation_SetsDefaults()
    {
        var supplier = new Supplier();
        supplier.Id.Should().NotBe(Guid.Empty);
        supplier.IsActive.Should().BeTrue();
        supplier.IsPreferred.Should().BeFalse();
        supplier.Currency.Should().Be("TRY");
    }

    [Fact]
    public void Supplier_AdjustBalance_AccumulatesCorrectly()
    {
        var supplier = new Supplier();
        supplier.AdjustBalance(5000m);
        supplier.AdjustBalance(-2000m);

        supplier.CurrentBalance.Should().Be(3000m);
    }

    [Fact]
    public void Supplier_RecordPayment_ReducesBalanceAndSetsDate()
    {
        var supplier = new Supplier();
        supplier.AdjustBalance(5000m);
        supplier.RecordPayment(2000m);

        supplier.CurrentBalance.Should().Be(3000m);
        supplier.LastPaymentDate.Should().NotBeNull();
    }

    [Fact]
    public void Supplier_RecordOrderPlaced_SetsDate()
    {
        var supplier = new Supplier();
        supplier.RecordOrderPlaced();
        supplier.LastOrderDate.Should().NotBeNull();
    }

    [Fact]
    public void Supplier_SetRating_ValidRange_SetsValue()
    {
        var supplier = new Supplier();
        supplier.SetRating(4);
        supplier.Rating.Should().Be(4);
    }

    [Fact]
    public void Supplier_SetRating_BelowRange_Throws()
    {
        var supplier = new Supplier();
        var act = () => supplier.SetRating(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Supplier_SetRating_AboveRange_Throws()
    {
        var supplier = new Supplier();
        var act = () => supplier.SetRating(6);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Supplier_MarkPreferred_Toggle()
    {
        var supplier = new Supplier();
        supplier.MarkAsPreferred();
        supplier.IsPreferred.Should().BeTrue();
        supplier.UnmarkAsPreferred();
        supplier.IsPreferred.Should().BeFalse();
    }

    [Fact]
    public void Supplier_HasExceededCreditLimit_TrueWhenExceeded()
    {
        var supplier = new Supplier { CreditLimit = 1000m };
        supplier.AdjustBalance(1500m);
        supplier.HasExceededCreditLimit.Should().BeTrue();
    }

    [Fact]
    public void Supplier_HasExceededCreditLimit_FalseWhenNoLimit()
    {
        var supplier = new Supplier();
        supplier.AdjustBalance(999999m);
        supplier.HasExceededCreditLimit.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // SupplierAccount
    // ═══════════════════════════════════════════

    [Fact]
    public void SupplierAccount_Creation_SetsDefaults()
    {
        var sa = new SupplierAccount();
        sa.Id.Should().NotBe(Guid.Empty);
        sa.Currency.Should().Be("TRY");
        sa.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SupplierAccount_SetPaymentTerms_ValidDays_Sets()
    {
        var sa = new SupplierAccount();
        sa.SetPaymentTerms(30);
        sa.PaymentTermDays.Should().Be(30);
    }

    [Fact]
    public void SupplierAccount_SetPaymentTerms_Negative_Throws()
    {
        var sa = new SupplierAccount();
        var act = () => sa.SetPaymentTerms(-1);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SupplierAccount_ActivateDeactivate_TogglesState()
    {
        var sa = new SupplierAccount();
        sa.Deactivate();
        sa.IsActive.Should().BeFalse();
        sa.Activate();
        sa.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SupplierAccount_RecordPurchase_CreatesCreditTransaction()
    {
        var sa = new SupplierAccount { TenantId = Guid.NewGuid() };
        var tx = sa.RecordPurchase(Guid.NewGuid(), 2000m, "ALF-001");

        tx.Type.Should().Be(TransactionType.PurchaseInvoice);
        tx.CreditAmount.Should().Be(2000m);
        tx.DebitAmount.Should().Be(0m);
        sa.Transactions.Should().HaveCount(1);
    }

    [Fact]
    public void SupplierAccount_RecordPayment_CreatesDebitTransaction()
    {
        var sa = new SupplierAccount { TenantId = Guid.NewGuid() };
        var tx = sa.RecordPayment(1000m, "ODM-001");

        tx.Type.Should().Be(TransactionType.Payment);
        tx.DebitAmount.Should().Be(1000m);
        tx.CreditAmount.Should().Be(0m);
    }

    [Fact]
    public void SupplierAccount_RecordPurchaseReturn_CreatesDebitTransaction()
    {
        var sa = new SupplierAccount { TenantId = Guid.NewGuid() };
        var tx = sa.RecordPurchaseReturn(Guid.NewGuid(), 500m);

        tx.Type.Should().Be(TransactionType.PurchaseReturn);
        tx.DebitAmount.Should().Be(500m);
    }

    [Fact]
    public void SupplierAccount_Balance_ComputedFromTransactions()
    {
        var sa = new SupplierAccount { TenantId = Guid.NewGuid() };
        sa.RecordPurchase(Guid.NewGuid(), 3000m, "ALF-001");
        sa.RecordPayment(1000m);

        // Purchase: 0 debit - 3000 credit = -3000
        // Payment: 1000 debit - 0 credit = 1000
        // Total: -2000 (biz tedarikçiye 2000 borçluyuz)
        sa.Balance.Should().Be(-2000m);
    }

    [Fact]
    public void SupplierAccount_OverdueBalance_FiltersOnDueDate()
    {
        var sa = new SupplierAccount { TenantId = Guid.NewGuid() };
        // No transactions with due dates, so overdue should be 0
        sa.OverdueBalance(DateTime.UtcNow).Should().Be(0m);
    }

    // ═══════════════════════════════════════════
    // SupplierFeed
    // ═══════════════════════════════════════════

    [Fact]
    public void SupplierFeed_Creation_SetsDefaults()
    {
        var feed = new SupplierFeed();
        feed.Id.Should().NotBe(Guid.Empty);
        feed.IsActive.Should().BeTrue();
        feed.SyncIntervalMinutes.Should().Be(60);
        feed.LastSyncStatus.Should().Be(FeedSyncStatus.None);
        feed.UsePercentMarkup.Should().BeTrue();
        feed.AutoDeactivateOnZeroStock.Should().BeTrue();
        feed.AutoActivateOnRestock.Should().BeTrue();
    }

    [Fact]
    public void SupplierFeed_ApplyMarkup_PercentMode_CalculatesCorrectly()
    {
        var feed = new SupplierFeed { UsePercentMarkup = true, PriceMarkupPercent = 20m };
        var result = feed.ApplyMarkup(100m);
        result.Should().Be(120m);
    }

    [Fact]
    public void SupplierFeed_ApplyMarkup_FixedMode_CalculatesCorrectly()
    {
        var feed = new SupplierFeed { UsePercentMarkup = false, PriceMarkupFixed = 25m };
        var result = feed.ApplyMarkup(100m);
        result.Should().Be(125m);
    }

    [Fact]
    public void SupplierFeed_MarkSyncInProgress_SetsStatus()
    {
        var feed = new SupplierFeed();
        feed.MarkSyncInProgress();
        feed.LastSyncStatus.Should().Be(FeedSyncStatus.InProgress);
    }

    [Fact]
    public void SupplierFeed_RecordSyncResult_Success_SetsCompletedStatus()
    {
        var feed = new SupplierFeed { TenantId = Guid.NewGuid(), SupplierId = Guid.NewGuid() };
        feed.RecordSyncResult(100, 50, 5);

        feed.LastSyncStatus.Should().Be(FeedSyncStatus.Completed);
        feed.LastSyncProductCount.Should().Be(100);
        feed.LastSyncUpdatedCount.Should().Be(50);
        feed.LastSyncDeactivatedCount.Should().Be(5);
        feed.LastSyncAt.Should().NotBeNull();
        feed.LastSyncError.Should().BeNull();
        feed.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void SupplierFeed_RecordSyncResult_WithError_SetsPartiallyCompleted()
    {
        var feed = new SupplierFeed { TenantId = Guid.NewGuid(), SupplierId = Guid.NewGuid() };
        feed.RecordSyncResult(100, 30, 0, "Timeout");

        feed.LastSyncStatus.Should().Be(FeedSyncStatus.PartiallyCompleted);
        feed.LastSyncError.Should().Be("Timeout");
    }

    [Fact]
    public void SupplierFeed_SetCredential_SetsEncryptedValue()
    {
        var feed = new SupplierFeed();
        feed.SetCredential("encrypted-blob");

        feed.EncryptedCredential.Should().Be("encrypted-blob");
        feed.HasCredential.Should().BeTrue();
    }

    [Fact]
    public void SupplierFeed_SetCredential_Null_ClearsCredential()
    {
        var feed = new SupplierFeed();
        feed.SetCredential("value");
        feed.SetCredential(null);

        feed.HasCredential.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    // Category
    // ═══════════════════════════════════════════

    [Fact]
    public void Category_Creation_SetsDefaults()
    {
        var cat = new Category();
        cat.Id.Should().NotBe(Guid.Empty);
        cat.IsActive.Should().BeTrue();
        cat.ShowInMenu.Should().BeTrue();
        cat.Name.Should().BeEmpty();
    }

    [Fact]
    public void Category_ToString_ReturnsCodeAndName()
    {
        var cat = new Category { Code = "ELK", Name = "Elektronik" };
        cat.ToString().Should().Be("[ELK] Elektronik");
    }

    // ═══════════════════════════════════════════
    // Brand
    // ═══════════════════════════════════════════

    [Fact]
    public void Brand_Creation_SetsDefaults()
    {
        var brand = new Brand();
        brand.Id.Should().NotBe(Guid.Empty);
        brand.IsActive.Should().BeTrue();
        brand.Name.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════
    // ProductVariant
    // ═══════════════════════════════════════════

    [Fact]
    public void ProductVariant_Create_SetsProperties()
    {
        var productId = Guid.NewGuid();
        var pv = ProductVariant.Create(productId, "SKU-001", 50, 99.99m);

        pv.ProductId.Should().Be(productId);
        pv.SKU.Should().Be("SKU-001");
        pv.Stock.Should().Be(50);
        pv.Price.Should().Be(99.99m);
    }

    [Fact]
    public void ProductVariant_Create_EmptySku_Throws()
    {
        var act = () => ProductVariant.Create(Guid.NewGuid(), "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProductVariant_Create_EmptyProductId_Throws()
    {
        var act = () => ProductVariant.Create(Guid.Empty, "SKU-001");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProductVariant_SetAttribute_StoresValue()
    {
        var pv = ProductVariant.Create(Guid.NewGuid(), "SKU-001");
        pv.SetAttribute("color", "red");

        pv.GetAttribute("color").Should().Be("red");
        pv.Attributes.Should().ContainKey("color");
    }

    [Fact]
    public void ProductVariant_SetAttribute_EmptyKey_Throws()
    {
        var pv = ProductVariant.Create(Guid.NewGuid(), "SKU-001");
        var act = () => pv.SetAttribute("", "value");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProductVariant_RemoveAttribute_RemovesKey()
    {
        var pv = ProductVariant.Create(Guid.NewGuid(), "SKU-001");
        pv.SetAttribute("size", "XL");
        pv.RemoveAttribute("size");

        pv.GetAttribute("size").Should().BeNull();
    }

    [Fact]
    public void ProductVariant_AttributesJson_RoundTripsCorrectly()
    {
        var pv = ProductVariant.Create(Guid.NewGuid(), "SKU-001");
        pv.SetAttribute("color", "blue");
        pv.SetAttribute("size", "M");

        var json = pv.AttributesJson;
        json.Should().Contain("color");
        json.Should().Contain("blue");

        // Simulate EF Core reload
        var pv2 = ProductVariant.Create(Guid.NewGuid(), "SKU-002");
        pv2.AttributesJson = json;

        pv2.GetAttribute("color").Should().Be("blue");
        pv2.GetAttribute("size").Should().Be("M");
    }

    [Fact]
    public void ProductVariant_AttributesJson_EmptyString_SetsEmptyDict()
    {
        var pv = ProductVariant.Create(Guid.NewGuid(), "SKU-001");
        pv.AttributesJson = "";
        pv.Attributes.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════
    // ProductPlatformMapping
    // ═══════════════════════════════════════════

    [Fact]
    public void ProductPlatformMapping_Creation_SetsDefaults()
    {
        var ppm = new ProductPlatformMapping();
        ppm.Id.Should().NotBe(Guid.Empty);
        ppm.SyncStatus.Should().Be(SyncStatus.NotSynced);
        ppm.IsEnabled.Should().BeTrue();
    }
}
