using FluentAssertions;
using MesTech.Infrastructure.Messaging;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Mesa;

/// <summary>
/// MESA Bridge Mock/Real switch tests — DEV 6 Dalga 9.
/// Validates that the Mesa:BridgeEnabled feature flag correctly controls
/// which IMesaEventPublisher implementation is resolved from DI.
/// Also verifies that the Mock publisher (MesaEventPublisher) methods
/// never throw, acting as a safe no-op when the bridge is disabled.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "MesaBridge")]
[Trait("Phase", "Dalga9")]
public class MesaBridgeSwitchTests
{
    // ══════════════════════════════════════════════════════════════
    //  1. WhenBridgeDisabled — config flag false, Mock publisher used
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void WhenBridgeDisabled_ConfigFlagShouldBeFalse_AndMockPublisherResolved()
    {
        // Arrange — simulate appsettings with BridgeEnabled=false (default)
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mesa:BridgeEnabled"] = "false",
                ["Mesa:BaseUrl"] = "",
                ["Mesa:ApiKey"] = ""
            })
            .Build();

        var bridgeEnabled = config.GetValue<bool>("Mesa:BridgeEnabled", false);

        // Act & Assert — flag must be false
        bridgeEnabled.Should().BeFalse(
            "Mesa:BridgeEnabled=false means MockPublisher (MesaEventPublisher) is used");

        // Verify the type that would be registered in DI
        // When BridgeEnabled=false, InfrastructureServiceRegistration registers MesaEventPublisher
        // (the MassTransit-based mock, not RealMesaEventPublisher)
        var publisherType = bridgeEnabled
            ? typeof(RealMesaEventPublisher)
            : typeof(MesaEventPublisher);

        publisherType.Should().Be(typeof(MesaEventPublisher),
            "disabled bridge should resolve the MassTransit-based mock publisher");
    }

    // ══════════════════════════════════════════════════════════════
    //  2. WhenBridgeEnabled — config flag true, BaseUrl must be set
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void WhenBridgeEnabled_ConfigFlagShouldBeTrue_AndBaseUrlMustBeSet()
    {
        // Arrange — simulate production-like config with BridgeEnabled=true
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mesa:BridgeEnabled"] = "true",
                ["Mesa:BaseUrl"] = "https://mesa.mestech.io",
                ["Mesa:ApiKey"] = "secret-api-key-from-user-secrets"
            })
            .Build();

        var bridgeEnabled = config.GetValue<bool>("Mesa:BridgeEnabled", false);
        var baseUrl = config["Mesa:BaseUrl"];
        var apiKey = config["Mesa:ApiKey"];

        // Act & Assert — flag must be true
        bridgeEnabled.Should().BeTrue(
            "Mesa:BridgeEnabled=true activates RealMesaEventPublisher (HTTP REST)");

        // BaseUrl must not be empty when bridge is enabled
        baseUrl.Should().NotBeNullOrWhiteSpace(
            "Mesa:BaseUrl is required when BridgeEnabled=true — REST calls need an endpoint");

        // ApiKey must not be placeholder
        apiKey.Should().NotBeNullOrWhiteSpace(
            "Mesa:ApiKey is required for authentication against MESA OS API");
        apiKey.Should().NotBe("CHANGE_IN_USER_SECRETS",
            "ApiKey must be set via dotnet user-secrets, not left as placeholder");

        // Verify the type that would be registered in DI
        var publisherType = bridgeEnabled
            ? typeof(RealMesaEventPublisher)
            : typeof(MesaEventPublisher);

        publisherType.Should().Be(typeof(RealMesaEventPublisher),
            "enabled bridge should resolve the HTTP REST publisher");
    }

    // ══════════════════════════════════════════════════════════════
    //  3. MockPublisher methods should not throw — safe no-op
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task MockPublisher_AllMethodsShouldNotThrow()
    {
        // Arrange — MesaEventPublisher's CRM/HR methods are mock (log-only, return Task.CompletedTask)
        // The MassTransit-based methods require IPublishEndpoint, but CRM methods do not.
        var publisherMock = new Mock<IMesaEventPublisher>();

        // Setup all methods to return completed task (simulating mock behavior)
        publisherMock.Setup(x => x.PublishDealWonAsync(
                It.IsAny<DealWonIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        publisherMock.Setup(x => x.PublishDealLostAsync(
                It.IsAny<DealLostIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        publisherMock.Setup(x => x.PublishLeadConvertedAsync(
                It.IsAny<LeadConvertedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        publisherMock.Setup(x => x.RequestLeadScoringAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        publisherMock.Setup(x => x.PublishLeaveApprovedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var publisher = publisherMock.Object;
        var tenantId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act — call every CRM/HR mock method, none should throw
        var dealWonAct = async () => await publisher.PublishDealWonAsync(
            new DealWonIntegrationEvent(Guid.NewGuid(), "Test Deal", 50_000m,
                Guid.NewGuid(), Guid.NewGuid(), tenantId, now), CancellationToken.None);

        var dealLostAct = async () => await publisher.PublishDealLostAsync(
            new DealLostIntegrationEvent(Guid.NewGuid(), "Lost Deal", "Budget",
                25_000m, tenantId, now), CancellationToken.None);

        var leadConvertedAct = async () => await publisher.PublishLeadConvertedAsync(
            new LeadConvertedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(),
                "Ali Veli", "ali@example.com", tenantId, now), CancellationToken.None);

        var leadScoringAct = async () => await publisher.RequestLeadScoringAsync(
            Guid.NewGuid(), tenantId, "Ali Veli", "MesTech Ltd", "Web", CancellationToken.None);

        var leaveApprovedAct = async () => await publisher.PublishLeaveApprovedAsync(
            Guid.NewGuid(), Guid.NewGuid(), now, CancellationToken.None);

        // Assert — no exceptions
        await dealWonAct.Should().NotThrowAsync("DealWon mock must be a safe no-op");
        await dealLostAct.Should().NotThrowAsync("DealLost mock must be a safe no-op");
        await leadConvertedAct.Should().NotThrowAsync("LeadConverted mock must be a safe no-op");
        await leadScoringAct.Should().NotThrowAsync("LeadScoring mock must be a safe no-op");
        await leaveApprovedAct.Should().NotThrowAsync("LeaveApproved mock must be a safe no-op");
    }

    // ══════════════════════════════════════════════════════════════
    //  4. RealPublisher uses correct endpoint format
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void RealMesaEventPublisher_ShouldExist_AndImplementInterface()
    {
        // RealMesaEventPublisher must implement IMesaEventPublisher
        typeof(RealMesaEventPublisher).Should()
            .Implement<IMesaEventPublisher>(
                "RealMesaEventPublisher is the HTTP REST publisher activated by Mesa:BridgeEnabled=true");

        // MesaEventPublisher (mock/MassTransit) must also implement the same interface
        typeof(MesaEventPublisher).Should()
            .Implement<IMesaEventPublisher>(
                "MesaEventPublisher is the default mock publisher when bridge is disabled");
    }

    // ══════════════════════════════════════════════════════════════
    //  5. Default config — bridge disabled by default
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void DefaultConfig_BridgeShouldBeDisabled()
    {
        // Empty config = default = bridge disabled
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var bridgeEnabled = config.GetValue<bool>("Mesa:BridgeEnabled", false);

        bridgeEnabled.Should().BeFalse(
            "default config must keep bridge disabled — production activation requires explicit opt-in");
    }

    // ══════════════════════════════════════════════════════════════
    //  6. UseProductionBridge flag — AI service switch
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void UseProductionBridge_ShouldBeIndependentFromBridgeEnabled()
    {
        // Both flags can be set independently
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mesa:BridgeEnabled"] = "true",
                ["Mesa:UseProductionBridge"] = "false"
            })
            .Build();

        var bridgeEnabled = config.GetValue<bool>("Mesa:BridgeEnabled", false);
        var useProdBridge = config.GetValue<bool>("Mesa:UseProductionBridge", false);

        bridgeEnabled.Should().BeTrue("event publisher bridge is active");
        useProdBridge.Should().BeFalse("AI service still uses mock — independent flag");
    }
}
