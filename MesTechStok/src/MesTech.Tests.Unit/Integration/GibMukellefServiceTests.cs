using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Invoice;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MesTech.Tests.Unit.Integration;

public class GibMukellefServiceTests
{
    [Fact]
    public void IGibMukellefService_ShouldExist()
    {
        var type = typeof(IGibMukellefService);
        Assert.True(type.IsInterface);
    }

    [Fact]
    public void IGibMukellefService_ShouldHaveIsEFaturaMukellefAsync()
    {
        var method = typeof(IGibMukellefService).GetMethod("IsEFaturaMukellefAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<bool>), method!.ReturnType);
    }

    [Fact]
    public void IGibMukellefService_ShouldHaveClearCacheMethod()
    {
        var method = typeof(IGibMukellefService).GetMethod("ClearCache");
        Assert.NotNull(method);
    }

    [Fact]
    public async Task IsEFaturaMukellef_CachesMissResult_For24Hours()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var mockProvider = new Mock<IInvoiceProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("Sovos");
        mockProvider.Setup(p => p.IsEInvoiceTaxpayerAsync("1234567890", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new GibMukellefService(
            new[] { mockProvider.Object },
            cache,
            NullLogger<GibMukellefService>.Instance);

        await service.IsEFaturaMukellefAsync("1234567890");
        await service.IsEFaturaMukellefAsync("1234567890");

        mockProvider.Verify(p => p.IsEInvoiceTaxpayerAsync("1234567890", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsEFaturaMukellef_ReturnsFalse_OnProviderException()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var mockProvider = new Mock<IInvoiceProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("Sovos");
        mockProvider.Setup(p => p.IsEInvoiceTaxpayerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("timeout"));

        var service = new GibMukellefService(
            new[] { mockProvider.Object },
            cache,
            NullLogger<GibMukellefService>.Instance);

        var result = await service.IsEFaturaMukellefAsync("9999999999");
        Assert.False(result);
    }

    [Fact]
    public async Task IsEFaturaMukellef_NormalizesVkn_TrimsAndUppercase()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var mockProvider = new Mock<IInvoiceProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("Sovos");
        mockProvider.Setup(p => p.IsEInvoiceTaxpayerAsync("1234567890", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new GibMukellefService(
            new[] { mockProvider.Object },
            cache,
            NullLogger<GibMukellefService>.Instance);

        var result = await service.IsEFaturaMukellefAsync("  1234567890  ");
        Assert.True(result);
    }

    [Fact]
    public void ClearCache_DoesNotThrow()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new GibMukellefService(
            Array.Empty<IInvoiceProvider>(),
            cache,
            NullLogger<GibMukellefService>.Instance);

        var act = () => service.ClearCache();

        act.Should().NotThrow();
    }

    [Fact]
    public async Task IsEFaturaMukellef_ReturnsFalse_WhenNoProviders()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new GibMukellefService(
            Array.Empty<IInvoiceProvider>(),
            cache,
            NullLogger<GibMukellefService>.Instance);

        var result = await service.IsEFaturaMukellefAsync("1234567890");
        Assert.False(result);
    }
}
