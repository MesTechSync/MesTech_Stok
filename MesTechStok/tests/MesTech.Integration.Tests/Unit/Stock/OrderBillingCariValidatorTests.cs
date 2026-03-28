using FluentAssertions;
using MesTech.Application.Commands.CreateOrder;
using MesTech.Application.Features.Shipping.Commands.AutoShipOrder;
using MesTech.Application.Features.Shipping.Commands.BatchShipOrders;
using MesTech.Application.Features.Billing.Commands.CancelSubscription;
using MesTech.Application.Features.Billing.Commands.CreateSubscription;
using MesTech.Application.Features.Billing.Commands.ChangeSubscriptionPlan;
using MesTech.Application.Commands.CreateCariHesap;
using MesTech.Application.Commands.CreateCariHareket;
using MesTech.Application.Features.Fulfillment.Commands.CreateInboundShipment;
using MesTech.Application.Commands.CreateBarcodeScanLog;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class OrderBillingCariValidatorTests
{
    // ═══ CreateOrder ═══
    [Fact] public void CreateOrder_Valid_Passes() { var v = new CreateOrderValidator(); v.Validate(new CreateOrderCommand(Guid.NewGuid(), "Müşteri", null, "MANUAL")).IsValid.Should().BeTrue(); }
    [Fact] public void CreateOrder_EmptyCustomerId_Fails() { var v = new CreateOrderValidator(); v.Validate(new CreateOrderCommand(Guid.Empty, "M", null, "MANUAL")).IsValid.Should().BeFalse(); }
    [Fact] public void CreateOrder_EmptyName_Fails() { var v = new CreateOrderValidator(); v.Validate(new CreateOrderCommand(Guid.NewGuid(), "", null, "MANUAL")).IsValid.Should().BeFalse(); }
    [Fact] public void CreateOrder_NameOver200_Fails() { var v = new CreateOrderValidator(); v.Validate(new CreateOrderCommand(Guid.NewGuid(), new string('M', 201), null, "MANUAL")).IsValid.Should().BeFalse(); }
    [Fact] public void CreateOrder_EmptyOrderType_Fails() { var v = new CreateOrderValidator(); v.Validate(new CreateOrderCommand(Guid.NewGuid(), "M", null, "")).IsValid.Should().BeFalse(); }

    // ═══ AutoShipOrder ═══
    [Fact] public void AutoShipOrder_Valid_Passes() { var v = new AutoShipOrderValidator(); v.Validate(new AutoShipOrderCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue(); }
    [Fact] public void AutoShipOrder_EmptyTenantId_Fails() { var v = new AutoShipOrderValidator(); v.Validate(new AutoShipOrderCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse(); }
    [Fact] public void AutoShipOrder_EmptyOrderId_Fails() { var v = new AutoShipOrderValidator(); v.Validate(new AutoShipOrderCommand(Guid.NewGuid(), Guid.Empty)).IsValid.Should().BeFalse(); }

    // ═══ BatchShipOrders ═══
    [Fact] public void BatchShipOrders_Valid_Passes() { var v = new BatchShipOrdersValidator(); v.Validate(new BatchShipOrdersCommand(Guid.NewGuid(), new List<Guid> { Guid.NewGuid() })).IsValid.Should().BeTrue(); }
    [Fact] public void BatchShipOrders_EmptyTenantId_Fails() { var v = new BatchShipOrdersValidator(); v.Validate(new BatchShipOrdersCommand(Guid.Empty, new List<Guid>())).IsValid.Should().BeFalse(); }

    // ═══ CancelSubscription ═══
    [Fact] public void CancelSubscription_Valid_Passes() { var v = new CancelSubscriptionValidator(); v.Validate(new CancelSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue(); }
    [Fact] public void CancelSubscription_EmptyTenantId_Fails() { var v = new CancelSubscriptionValidator(); v.Validate(new CancelSubscriptionCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse(); }
    [Fact] public void CancelSubscription_EmptySubId_Fails() { var v = new CancelSubscriptionValidator(); v.Validate(new CancelSubscriptionCommand(Guid.NewGuid(), Guid.Empty)).IsValid.Should().BeFalse(); }
    [Fact] public void CancelSubscription_ReasonOver500_Fails() { var v = new CancelSubscriptionValidator(); v.Validate(new CancelSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), new string('R', 501))).IsValid.Should().BeFalse(); }

    // ═══ CreateSubscription ═══
    [Fact] public void CreateSubscription_Valid_Passes() { var v = new CreateSubscriptionValidator(); v.Validate(new CreateSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue(); }
    [Fact] public void CreateSubscription_EmptyTenantId_Fails() { var v = new CreateSubscriptionValidator(); v.Validate(new CreateSubscriptionCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse(); }
    [Fact] public void CreateSubscription_EmptyPlanId_Fails() { var v = new CreateSubscriptionValidator(); v.Validate(new CreateSubscriptionCommand(Guid.NewGuid(), Guid.Empty)).IsValid.Should().BeFalse(); }

    // ═══ ChangeSubscriptionPlan ═══
    [Fact] public void ChangeSubscriptionPlan_Valid_Passes() { var v = new ChangeSubscriptionPlanValidator(); v.Validate(new ChangeSubscriptionPlanCommand(Guid.NewGuid(), Guid.NewGuid())).IsValid.Should().BeTrue(); }
    [Fact] public void ChangeSubscriptionPlan_EmptyTenantId_Fails() { var v = new ChangeSubscriptionPlanValidator(); v.Validate(new ChangeSubscriptionPlanCommand(Guid.Empty, Guid.NewGuid())).IsValid.Should().BeFalse(); }
    [Fact] public void ChangeSubscriptionPlan_EmptyPlanId_Fails() { var v = new ChangeSubscriptionPlanValidator(); v.Validate(new ChangeSubscriptionPlanCommand(Guid.NewGuid(), Guid.Empty)).IsValid.Should().BeFalse(); }

    // ═══ CreateCariHesap ═══
    [Fact] public void CreateCariHesap_Valid_Passes() { var v = new CreateCariHesapValidator(); v.Validate(new CreateCariHesapCommand(Guid.NewGuid(), "ABC Ltd.", null, CariHesapType.Customer, null, null, null)).IsValid.Should().BeTrue(); }
    [Fact] public void CreateCariHesap_EmptyName_Fails() { var v = new CreateCariHesapValidator(); v.Validate(new CreateCariHesapCommand(Guid.NewGuid(), "", null, CariHesapType.Customer, null, null, null)).IsValid.Should().BeFalse(); }
    [Fact] public void CreateCariHesap_NameOver500_Fails() { var v = new CreateCariHesapValidator(); v.Validate(new CreateCariHesapCommand(Guid.NewGuid(), new string('A', 501), null, CariHesapType.Customer, null, null, null)).IsValid.Should().BeFalse(); }

    // ═══ CreateCariHareket ═══
    [Fact] public void CreateCariHareket_Valid_Passes() { var v = new CreateCariHareketValidator(); v.Validate(new CreateCariHareketCommand(Guid.NewGuid(), Guid.NewGuid(), 1000m, CariDirection.Borc, "Test", null, null, null)).IsValid.Should().BeTrue(); }
    [Fact] public void CreateCariHareket_EmptyDescription_Fails() { var v = new CreateCariHareketValidator(); v.Validate(new CreateCariHareketCommand(Guid.NewGuid(), Guid.NewGuid(), 100m, CariDirection.Borc, "", null, null, null)).IsValid.Should().BeFalse(); }
    [Fact] public void CreateCariHareket_NegativeAmount_Fails() { var v = new CreateCariHareketValidator(); v.Validate(new CreateCariHareketCommand(Guid.NewGuid(), Guid.NewGuid(), -1m, CariDirection.Borc, "Test", null, null, null)).IsValid.Should().BeFalse(); }

    // ═══ CreateInboundShipment ═══
    [Fact] public void CreateInboundShipment_EmptyName_Fails() { var v = new CreateInboundShipmentValidator(); v.Validate(new CreateInboundShipmentCommand(FulfillmentCenter.AmazonFba, "", new List<InboundItem>())).IsValid.Should().BeFalse(); }

    // ═══ CreateBarcodeScanLog ═══
    [Fact] public void CreateBarcodeScanLog_Valid_Passes() { var v = new CreateBarcodeScanLogValidator(); v.Validate(new CreateBarcodeScanLogCommand("8680000000001", "EAN-13", "Scanner")).IsValid.Should().BeTrue(); }
    [Fact] public void CreateBarcodeScanLog_EmptyBarcode_Fails() { var v = new CreateBarcodeScanLogValidator(); v.Validate(new CreateBarcodeScanLogCommand("", "EAN-13", "Scanner")).IsValid.Should().BeFalse(); }
    [Fact] public void CreateBarcodeScanLog_EmptyFormat_Fails() { var v = new CreateBarcodeScanLogValidator(); v.Validate(new CreateBarcodeScanLogCommand("123", "", "Scanner")).IsValid.Should().BeFalse(); }
    [Fact] public void CreateBarcodeScanLog_EmptySource_Fails() { var v = new CreateBarcodeScanLogValidator(); v.Validate(new CreateBarcodeScanLogCommand("123", "EAN-13", "")).IsValid.Should().BeFalse(); }
}
