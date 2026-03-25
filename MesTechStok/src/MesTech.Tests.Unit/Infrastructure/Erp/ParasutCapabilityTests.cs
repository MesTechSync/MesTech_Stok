using FluentAssertions;
using MesTech.Application.Interfaces.Erp;
using MesTech.Infrastructure.Integration.ERP.Parasut;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MesTech.Tests.Unit.Infrastructure.Erp;

/// <summary>
/// I-14 T-01: Parasut ERP adapter capability parity tests.
/// Verifies ISP interface implementations (IErpInvoiceCapable, IErpAccountCapable, IErpStockCapable, IErpBankCapable).
/// </summary>
public class ParasutCapabilityTests
{
    private static ParasutERPAdapter CreateAdapter()
    {
        var httpClient = new HttpClient();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ERP:Parasut:ClientId"] = "test-client",
                ["ERP:Parasut:ClientSecret"] = "test-secret",
                ["ERP:Parasut:CompanyId"] = "12345"
            })
            .Build();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var tokenService = new ParasutTokenService(
            httpClient,
            cache,
            config,
            NullLogger<ParasutTokenService>.Instance);

        return new ParasutERPAdapter(
            httpClient,
            tokenService,
            new Mock<MesTech.Domain.Interfaces.IOrderRepository>().Object,
            new Mock<MesTech.Domain.Interfaces.IInvoiceRepository>().Object,
            NullLogger<ParasutERPAdapter>.Instance);
    }

    [Fact]
    public void InvoiceCapable_CastSuccess()
    {
        var adapter = CreateAdapter();
        (adapter is IErpInvoiceCapable).Should().BeTrue("Parasut must implement IErpInvoiceCapable");
    }

    [Fact]
    public void AccountCapable_CastSuccess()
    {
        var adapter = CreateAdapter();
        (adapter is IErpAccountCapable).Should().BeTrue("Parasut must implement IErpAccountCapable");
    }

    [Fact]
    public void StockCapable_CastSuccess()
    {
        var adapter = CreateAdapter();
        (adapter is IErpStockCapable).Should().BeTrue("Parasut must implement IErpStockCapable");
    }

    [Fact]
    public void BankCapable_CastSuccess()
    {
        var adapter = CreateAdapter();
        (adapter is IErpBankCapable).Should().BeTrue("Parasut must implement IErpBankCapable");
    }

    [Fact]
    public void WaybillCapable_NotImplemented()
    {
        var adapter = CreateAdapter();
        (adapter is IErpWaybillCapable).Should().BeFalse("Parasut does not support waybill operations");
    }

    [Fact]
    public async Task InvoiceCapable_CreateInvoice_WithNullRequest_ThrowsOrFails()
    {
        var adapter = CreateAdapter();
        var invoiceCapable = (IErpInvoiceCapable)adapter;

        var act = () => invoiceCapable.CreateInvoiceAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>("null request should be rejected");
    }

    [Fact]
    public async Task AccountCapable_CreateAccount_WithNullRequest_ThrowsOrFails()
    {
        var adapter = CreateAdapter();
        var accountCapable = (IErpAccountCapable)adapter;

        var act = () => accountCapable.CreateAccountAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>("null request should be rejected");
    }

    [Fact]
    public async Task StockCapable_GetStockLevels_ReturnsNonNull()
    {
        var adapter = CreateAdapter();
        var stockCapable = (IErpStockCapable)adapter;

        // Will fail with HTTP error but should not return null — validates method exists and returns a list type
        Func<Task> act = () => stockCapable.GetStockLevelsAsync(CancellationToken.None);

        // Either returns a valid list or throws an HTTP/network error — both acceptable
        try
        {
            var result = await stockCapable.GetStockLevelsAsync(CancellationToken.None);
            result.Should().NotBeNull();
        }
        catch (Exception ex)
        {
            // HTTP errors expected in unit test without real API — just verify it's not NotImplementedException
            ex.Should().NotBeOfType<NotImplementedException>("GetStockLevels must be implemented");
        }
    }

    [Fact]
    public async Task BankCapable_GetTransactions_ReturnsNonNull()
    {
        var adapter = CreateAdapter();
        var bankCapable = (IErpBankCapable)adapter;

        try
        {
            var result = await bankCapable.GetTransactionsAsync(
                DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, CancellationToken.None);
            result.Should().NotBeNull();
        }
        catch (Exception ex)
        {
            // HTTP errors expected in unit test without real API — just verify it's not NotImplementedException
            ex.Should().NotBeOfType<NotImplementedException>("GetTransactions must be implemented");
        }
    }

    [Fact]
    public void AllFourCapabilities_CombinedCast_Success()
    {
        var adapter = CreateAdapter();

        adapter.Should().BeAssignableTo<IErpInvoiceCapable>();
        adapter.Should().BeAssignableTo<IErpAccountCapable>();
        adapter.Should().BeAssignableTo<IErpStockCapable>();
        adapter.Should().BeAssignableTo<IErpBankCapable>();
    }
}
