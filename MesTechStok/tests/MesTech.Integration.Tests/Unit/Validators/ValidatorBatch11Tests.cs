using FluentAssertions;
using MesTech.Application.Features.Notifications.Commands.SendNotification;
using MesTech.Application.Features.Notifications.Commands.MarkNotificationRead;
using MesTech.Application.Features.Notifications.Commands.UpdateNotificationSettings;
using MesTech.Application.Features.Platform.Commands.FetchProductFromUrl;
using MesTech.Application.Features.Platform.Commands.TestStoreConnection;
using MesTech.Application.Features.Platform.Commands.TriggerSync;
using MesTech.Application.Features.Erp.Commands.SyncOrderToErp;
using MesTech.Application.Features.Stores.Commands.DeleteStoreCredential;
using MesTech.Application.Features.Stores.Commands.TestStoreCredential;
using MesTech.Application.Features.Finance.Commands.MarkExpensePaid;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Validators;

#region SendNotification

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class SendNotificationValidatorTests
{
    private readonly SendNotificationValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new SendNotificationCommand(Guid.NewGuid(), "Email", "user@test.com", "OrderShipped", "Sipariş kargoya verildi");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new SendNotificationCommand(Guid.Empty, "Email", "user@test.com", "Test", "Test");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Empty_Channel_Fails()
    {
        var cmd = new SendNotificationCommand(Guid.NewGuid(), "", "user@test.com", "Test", "Test");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Channel");
    }

    [Fact]
    public void Empty_Recipient_Fails()
    {
        var cmd = new SendNotificationCommand(Guid.NewGuid(), "SMS", "", "Test", "Test");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Recipient");
    }

    [Fact]
    public void Empty_Content_Fails()
    {
        var cmd = new SendNotificationCommand(Guid.NewGuid(), "Push", "device", "Alert", "");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Content");
    }
}

#endregion

#region MarkNotificationRead

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class MarkNotificationReadValidatorTests
{
    private readonly MarkNotificationReadValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new MarkNotificationReadCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new MarkNotificationReadCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_NotificationId_Fails()
    {
        var cmd = new MarkNotificationReadCommand(Guid.NewGuid(), Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}

#endregion

#region UpdateNotificationSettings

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class UpdateNotificationSettingsValidatorTests
{
    private readonly UpdateNotificationSettingsValidator _validator = new();

    private static UpdateNotificationSettingsCommand ValidCommand() => new(
        TenantId: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        ChannelAddress: "user@test.com",
        LowStockThreshold: 10,
        QuietHoursStart: "22:00",
        QuietHoursEnd: "08:00",
        PreferredLanguage: "tr",
        DigestTime: "09:00");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = ValidCommand() with { TenantId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_UserId_Fails()
    {
        var cmd = ValidCommand() with { UserId = Guid.Empty };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Negative_LowStockThreshold_Fails()
    {
        var cmd = ValidCommand() with { LowStockThreshold = -1 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_PreferredLanguage_Fails()
    {
        var cmd = ValidCommand() with { PreferredLanguage = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Null_Optional_Fields_Pass()
    {
        var cmd = ValidCommand() with
        {
            ChannelAddress = null, QuietHoursStart = null,
            QuietHoursEnd = null, DigestTime = null
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }
}

#endregion

#region FetchProductFromUrl

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class FetchProductFromUrlValidatorTests
{
    private readonly FetchProductFromUrlValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new FetchProductFromUrlCommand("https://www.trendyol.com/product/12345");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ProductUrl_Fails()
    {
        var cmd = new FetchProductFromUrlCommand("");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductUrl");
    }
}

#endregion

#region TestStoreConnection + TestStoreCredential + DeleteStoreCredential

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class TestStoreConnectionValidatorTests
{
    private readonly TestStoreConnectionValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(new TestStoreConnectionCommand(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_StoreId_Fails()
    {
        var result = _validator.Validate(new TestStoreConnectionCommand(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class TestStoreCredentialValidatorTests
{
    private readonly TestStoreCredentialValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(new TestStoreCredentialCommand(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_StoreId_Fails()
    {
        var result = _validator.Validate(new TestStoreCredentialCommand(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }
}

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class DeleteStoreCredentialValidatorTests
{
    private readonly DeleteStoreCredentialValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(new DeleteStoreCredentialCommand(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_StoreId_Fails()
    {
        var result = _validator.Validate(new DeleteStoreCredentialCommand(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }
}

#endregion

#region TriggerSync

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class TriggerSyncValidatorTests
{
    private readonly TriggerSyncValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new TriggerSyncCommand(Guid.NewGuid(), "Trendyol");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new TriggerSyncCommand(Guid.Empty, "Trendyol");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_PlatformCode_Fails()
    {
        var cmd = new TriggerSyncCommand(Guid.NewGuid(), "");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void PlatformCode_Over_50_Chars_Fails()
    {
        var cmd = new TriggerSyncCommand(Guid.NewGuid(), new string('P', 51));
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}

#endregion

#region SyncOrderToErp

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class SyncOrderToErpValidatorTests
{
    private readonly SyncOrderToErpValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new SyncOrderToErpCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_TenantId_Fails()
    {
        var cmd = new SyncOrderToErpCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_OrderId_Fails()
    {
        var cmd = new SyncOrderToErpCommand(Guid.NewGuid(), Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}

#endregion

#region MarkExpensePaid

[Trait("Category", "Unit")]
[Trait("Layer", "Validation")]
public class MarkExpensePaidValidatorTests
{
    private readonly MarkExpensePaidValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var cmd = new MarkExpensePaidCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_ExpenseId_Fails()
    {
        var cmd = new MarkExpensePaidCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_BankAccountId_Fails()
    {
        var cmd = new MarkExpensePaidCommand(Guid.NewGuid(), Guid.Empty);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}

#endregion
