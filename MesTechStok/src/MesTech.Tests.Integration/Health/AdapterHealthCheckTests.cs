using System.Net.Http;
using System.Reflection;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesTech.Tests.Integration.Health;

/// <summary>
/// Health checks for all 8 platform adapters + 3 cargo adapters.
/// Validates Polly ResiliencePipeline and SemaphoreSlim rate limiting
/// via reflection — ensures defensive infrastructure is wired correctly.
/// DEV 3 Dalga 7 Batch 3.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Group", "Health")]
public class AdapterHealthCheckTests
{
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

    // ════════════════════════════════════════════════════════════════
    //  Platform Adapter Health: Polly Pipeline Checks
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(typeof(TrendyolAdapter), "_retryPipeline")]
    [InlineData(typeof(OpenCartAdapter), "_retryPipeline")]
    [InlineData(typeof(CiceksepetiAdapter), "_retryPipeline")]
    [InlineData(typeof(HepsiburadaAdapter), "_retryPipeline")]
    [InlineData(typeof(PazaramaAdapter), "_retryPipeline")]
    [InlineData(typeof(AmazonTrAdapter), "_retryPipeline")]
    [InlineData(typeof(Bitrix24Adapter), "_retryPipeline")]
    public void PlatformAdapter_HasRetryPipeline(Type adapterType, string fieldName)
    {
        var field = adapterType.GetField(fieldName, PrivateInstance);
        field.Should().NotBeNull(
            $"{adapterType.Name} should have '{fieldName}' field for Polly retry/circuit-breaker");

        var adapter = CreateAdapterInstance(adapterType);
        var pipeline = field!.GetValue(adapter);
        pipeline.Should().NotBeNull(
            $"{adapterType.Name}.{fieldName} should be initialized in constructor");
    }

    // ════════════════════════════════════════════════════════════════
    //  Platform Adapter Health: Rate Limit Semaphore Checks
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(typeof(TrendyolAdapter), "_rateLimitSemaphore", 100)]
    [InlineData(typeof(CiceksepetiAdapter), "_rateLimitSemaphore", 10)]
    [InlineData(typeof(HepsiburadaAdapter), "_rateLimitSemaphore", 20)]
    [InlineData(typeof(PazaramaAdapter), "_rateLimitSemaphore", 10)]
    // Bitrix24Adapter excluded: ConfigureRateLimit() replaces the static semaphore object,
    // causing a race when parallel unit tests (ConfigureRateLimit_SetsConcurrency) run
    // concurrently with integration tests. Configurable limit is verified in unit tests.
    public void PlatformAdapter_HasRateLimitSemaphore(Type adapterType, string fieldName, int expectedConcurrency)
    {
        var field = adapterType.GetField(fieldName, PrivateStatic);
        field.Should().NotBeNull(
            $"{adapterType.Name} should have static '{fieldName}' for rate limiting");

        var semaphore = field!.GetValue(null) as SemaphoreSlim;
        semaphore.Should().NotBeNull();
        // Use m_maxCount (design-time capacity) instead of CurrentCount (runtime available slots)
        // to avoid flakiness when integration tests run in parallel and consume semaphore slots.
        GetSemaphoreMaxCount(semaphore!).Should().Be(expectedConcurrency,
            $"{adapterType.Name} rate limit capacity should be {expectedConcurrency} concurrent requests");
    }

    [Fact]
    public void Bitrix24Adapter_HasRateLimitSemaphore_FieldExists()
    {
        // Bitrix24 has a configurable static semaphore (ConfigureRateLimit replaces the object).
        // We only verify the field exists and is initialized; exact capacity is tested in unit tests.
        var field = typeof(Bitrix24Adapter).GetField("_rateLimitSemaphore", PrivateStatic);
        field.Should().NotBeNull("Bitrix24Adapter should have static '_rateLimitSemaphore' for rate limiting");

        var semaphore = field!.GetValue(null) as SemaphoreSlim;
        semaphore.Should().NotBeNull("Bitrix24Adapter._rateLimitSemaphore should be initialized");
    }

    // ════════════════════════════════════════════════════════════════
    //  Platform Adapter Health: PlatformCode Checks
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(typeof(TrendyolAdapter), "Trendyol")]
    [InlineData(typeof(OpenCartAdapter), "OpenCart")]
    [InlineData(typeof(CiceksepetiAdapter), "Ciceksepeti")]
    [InlineData(typeof(HepsiburadaAdapter), "Hepsiburada")]
    [InlineData(typeof(PazaramaAdapter), "Pazarama")]
    [InlineData(typeof(AmazonTrAdapter), "Amazon")]
    [InlineData(typeof(Bitrix24Adapter), "Bitrix24")]
    public void PlatformAdapter_HasCorrectPlatformCode(Type adapterType, string expectedCode)
    {
        var adapter = CreateAdapterInstance(adapterType);
        var platformCodeProp = adapterType.GetProperty("PlatformCode");
        platformCodeProp.Should().NotBeNull();

        var code = platformCodeProp!.GetValue(adapter) as string;
        code.Should().Be(expectedCode);
    }

    // ════════════════════════════════════════════════════════════════
    //  Cargo Adapter Health: Polly + Rate Limit Checks
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(typeof(ArasKargoAdapter), "_retryPipeline")]
    [InlineData(typeof(SuratKargoAdapter), "_retryPipeline")]
    public void CargoAdapter_HasRetryPipeline(Type adapterType, string fieldName)
    {
        var field = adapterType.GetField(fieldName, PrivateInstance);
        field.Should().NotBeNull(
            $"{adapterType.Name} should have '{fieldName}' for Polly retry/circuit-breaker");

        var adapter = CreateCargoAdapterInstance(adapterType);
        var pipeline = field!.GetValue(adapter);
        pipeline.Should().NotBeNull(
            $"{adapterType.Name}.{fieldName} should be initialized in constructor");
    }

    [Theory]
    [InlineData(typeof(ArasKargoAdapter), "_rateLimitSemaphore", 15)]
    [InlineData(typeof(SuratKargoAdapter), "_rateLimitSemaphore", 10)]
    public void CargoAdapter_HasRateLimitSemaphore(Type adapterType, string fieldName, int expectedConcurrency)
    {
        var field = adapterType.GetField(fieldName, PrivateStatic);
        field.Should().NotBeNull(
            $"{adapterType.Name} should have static '{fieldName}' for rate limiting");

        var semaphore = field!.GetValue(null) as SemaphoreSlim;
        semaphore.Should().NotBeNull();
        // Use m_maxCount (design-time capacity) instead of CurrentCount (runtime available slots)
        // to avoid flakiness when integration tests run in parallel and consume semaphore slots.
        GetSemaphoreMaxCount(semaphore!).Should().Be(expectedConcurrency,
            $"{adapterType.Name} rate limit capacity should be {expectedConcurrency} concurrent requests");
    }

    // ════════════════════════════════════════════════════════════════
    //  Adapter Error Handling: SupportsXxx Capability Flags
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(typeof(TrendyolAdapter), true, true, true)]
    [InlineData(typeof(OpenCartAdapter), true, true, false)]
    [InlineData(typeof(CiceksepetiAdapter), true, true, true)]
    [InlineData(typeof(HepsiburadaAdapter), true, true, true)]
    [InlineData(typeof(PazaramaAdapter), true, true, true)]
    [InlineData(typeof(AmazonTrAdapter), true, true, true)]
    [InlineData(typeof(Bitrix24Adapter), false, true, false)]
    public void PlatformAdapter_HasCorrectCapabilities(
        Type adapterType, bool supportsStock, bool supportsPrice, bool supportsShipment)
    {
        var adapter = CreateAdapterInstance(adapterType);

        var stockProp = adapterType.GetProperty("SupportsStockUpdate");
        var priceProp = adapterType.GetProperty("SupportsPriceUpdate");
        var shipProp = adapterType.GetProperty("SupportsShipment");

        stockProp.Should().NotBeNull();
        priceProp.Should().NotBeNull();
        shipProp.Should().NotBeNull();

        stockProp!.GetValue(adapter).Should().Be(supportsStock,
            $"{adapterType.Name}.SupportsStockUpdate");
        priceProp!.GetValue(adapter).Should().Be(supportsPrice,
            $"{adapterType.Name}.SupportsPriceUpdate");
        shipProp!.GetValue(adapter).Should().Be(supportsShipment,
            $"{adapterType.Name}.SupportsShipment");
    }

    // ════════════════════════════════════════════════════════════════
    //  DI Compatibility: Constructor Parameter Validation
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(typeof(TrendyolAdapter))]
    [InlineData(typeof(OpenCartAdapter))]
    [InlineData(typeof(CiceksepetiAdapter))]
    [InlineData(typeof(HepsiburadaAdapter))]
    [InlineData(typeof(PazaramaAdapter))]
    [InlineData(typeof(AmazonTrAdapter))]
    [InlineData(typeof(Bitrix24Adapter))]
    public void PlatformAdapter_ThrowsOnNullHttpClient(Type adapterType)
    {
        var ctor = adapterType.GetConstructors().FirstOrDefault(c =>
            c.GetParameters().Any(p => p.ParameterType == typeof(HttpClient)));

        ctor.Should().NotBeNull($"{adapterType.Name} should accept HttpClient in constructor");

        var act = () =>
        {
            var loggerType = ctor!.GetParameters()
                .First(p => p.ParameterType.Name.StartsWith("ILogger")).ParameterType;
            var nullLoggerType = typeof(NullLogger<>).MakeGenericType(adapterType);
            var logger = Activator.CreateInstance(nullLoggerType);
            ctor.Invoke(new object?[] { null, logger });
        };

        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<ArgumentNullException>();
    }

    // ════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════

    private static object CreateAdapterInstance(Type adapterType)
    {
        var loggerType = typeof(NullLogger<>).MakeGenericType(adapterType);
        var logger = Activator.CreateInstance(loggerType)!;
        return Activator.CreateInstance(adapterType, new HttpClient(), logger)!;
    }

    private static object CreateCargoAdapterInstance(Type adapterType)
    {
        var loggerType = typeof(NullLogger<>).MakeGenericType(adapterType);
        var logger = Activator.CreateInstance(loggerType)!;
        return Activator.CreateInstance(adapterType, new HttpClient(), logger)!;
    }

    /// <summary>
    /// Returns the design-time max capacity of a SemaphoreSlim via reflection on the internal
    /// m_maxCount field. Unlike CurrentCount, this value is immutable after construction and
    /// is not affected by concurrent WaitAsync/Release calls during parallel test execution.
    /// </summary>
    private static int GetSemaphoreMaxCount(SemaphoreSlim semaphore)
    {
        var field = typeof(SemaphoreSlim).GetField("m_maxCount", BindingFlags.NonPublic | BindingFlags.Instance);
        return field is not null ? (int)field.GetValue(semaphore)! : -1;
    }
}
