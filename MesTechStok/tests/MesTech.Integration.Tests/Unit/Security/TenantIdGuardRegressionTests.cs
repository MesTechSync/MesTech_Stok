using FluentAssertions;
using FluentValidation;
using MesTech.Application.Commands.PlaceOrder;
using MesTech.Application.Commands.CreateProduct;
using MesTech.Application.Commands.AddStock;
using MesTech.Application.Commands.RemoveStock;
using MesTech.Application.Commands.TransferStock;
using MesTech.Application.Features.Accounting.Commands.CreateJournalEntry;
using MesTech.Application.Features.Accounting.Commands.RecordCommission;
using MesTech.Application.Features.Accounting.Commands.CreateChartOfAccount;
using MesTech.Application.Features.Accounting.Commands.CreatePlatformCommissionRate;
using MesTech.Application.Features.Accounting.Commands.CloseAccountingPeriod;
using MesTech.Application.Features.Billing.Commands.ChangeSubscriptionPlan;
using MesTech.Application.Features.Platform.Commands.CreateStore;
using MesTech.Application.Features.System.Kvkk.Commands.DeletePersonalData;
using MesTech.Domain.Enums;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Security;

/// <summary>
/// G028: TenantId != Guid.Empty regression guard testi.
/// Tüm tenant-scoped validator'ların Guid.Empty TenantId'yi reject ettiğini doğrular.
/// Her yeni validator eklenmesinde buraya da eklenmeli.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Security")]
[Trait("Group", "TenantIdGuard")]
public class TenantIdGuardRegressionTests
{
    [Theory]
    [MemberData(nameof(TenantValidators))]
    public void Validator_RejectEmptyTenantId(string name, IValidator validator, object command)
    {
        var context = new ValidationContext<object>(command);
        var result = validator.Validate(context);

        result.IsValid.Should().BeFalse(
            $"{name} should reject Guid.Empty TenantId — tenant isolation risk");
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId",
            $"{name} should have TenantId error");
    }

    public static IEnumerable<object[]> TenantValidators()
    {
        // Accounting
        yield return V("CreateJournalEntry", new CreateJournalEntryValidator(),
            new CreateJournalEntryCommand(Guid.Empty, DateTime.UtcNow, "test", null,
                new List<JournalLineInput> { new(Guid.NewGuid(), 100, 0, "d"), new(Guid.NewGuid(), 0, 100, "c") }));

        yield return V("RecordCommission", new RecordCommissionValidator(),
            new RecordCommissionCommand(Guid.Empty, "Trendyol", 1000m, 0.15m, 150m, 5m));

        yield return V("CreateChartOfAccount", new CreateChartOfAccountValidator(),
            new CreateChartOfAccountCommand(Guid.Empty, "100", "Kasa", MesTech.Domain.Accounting.Enums.AccountType.Asset));

        yield return V("CreatePlatformCommissionRate", new CreatePlatformCommissionRateValidator(),
            new CreatePlatformCommissionRateCommand(Guid.Empty, PlatformType.Trendyol, 15m));

        yield return V("CloseAccountingPeriod", new CloseAccountingPeriodValidator(),
            new CloseAccountingPeriodCommand(Guid.Empty, 2026, 3, "admin"));

        // Billing
        yield return V("ChangeSubscriptionPlan", new ChangeSubscriptionPlanValidator(),
            new ChangeSubscriptionPlanCommand(Guid.Empty, Guid.NewGuid()));

        // Platform
        yield return V("CreateStore", new CreateStoreValidator(),
            new CreateStoreCommand(Guid.Empty, "Store", PlatformType.Trendyol, new Dictionary<string, string>()));

        // KVKK
        yield return V("DeletePersonalData", new DeletePersonalDataValidator(),
            new DeletePersonalDataCommand(Guid.Empty, Guid.NewGuid(), "KVKK talebi"));
    }

    private static object[] V(string name, IValidator validator, object command)
        => new object[] { name, validator, command };
}
