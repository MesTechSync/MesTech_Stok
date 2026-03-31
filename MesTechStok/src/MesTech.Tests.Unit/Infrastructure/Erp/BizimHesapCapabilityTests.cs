using FluentAssertions;
using MesTech.Application.Interfaces.Erp;
using MesTech.Infrastructure.Integration.ERP.BizimHesap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MesTech.Tests.Unit.Infrastructure.Erp;

/// <summary>
/// I-14 T-01: BizimHesap ERP adapter capability parity tests.
/// Verifies ISP interface implementations (IErpInvoiceCapable, IErpAccountCapable, IErpStockCapable).
/// BizimHesap does NOT support IErpWaybillCapable or IErpBankCapable.
/// </summary>
public class BizimHesapCapabilityTests
{
    private static BizimHesapERPAdapter CreateAdapter()
    {
        var httpClient = new HttpClient();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ERP:BizimHesap:BaseUrl"] = "https://api.bizimhesap.com/v1/",
                ["ERP:BizimHesap:ApiKey"] = "test-api-key"
            })
            .Build();

        var apiClient = new BizimHesapApiClient(
            httpClient,
            config,
            NullLogger<BizimHesapApiClient>.Instance);

        return new BizimHesapERPAdapter(
            apiClient,
            new Mock<MesTech.Domain.Interfaces.IOrderRepository>().Object,
            new Mock<MesTech.Domain.Interfaces.IInvoiceRepository>().Object,
            NullLogger<BizimHesapERPAdapter>.Instance);
    }

    [Fact]
    public void InvoiceCapable_CastSuccess()
    {
        var adapter = CreateAdapter();
        (adapter is IErpInvoiceCapable).Should().BeTrue("BizimHesap must implement IErpInvoiceCapable");
    }

    [Fact]
    public void AccountCapable_CastSuccess()
    {
        var adapter = CreateAdapter();
        (adapter is IErpAccountCapable).Should().BeTrue("BizimHesap must implement IErpAccountCapable");
    }

    [Fact]
    public void StockCapable_CastSuccess()
    {
        var adapter = CreateAdapter();
        (adapter is IErpStockCapable).Should().BeTrue("BizimHesap must implement IErpStockCapable");
    }

    [Fact]
    public void WaybillCapable_Implemented()
    {
        var adapter = CreateAdapter();
        (adapter is IErpWaybillCapable).Should().BeTrue("BizimHesap now implements IErpWaybillCapable");
    }

    [Fact]
    public void BankCapable_Implemented()
    {
        var adapter = CreateAdapter();
        (adapter is IErpBankCapable).Should().BeTrue("BizimHesap now implements IErpBankCapable");
    }

    [Fact]
    public void AllThreeCapabilities_CombinedCast_Success()
    {
        var adapter = CreateAdapter();

        adapter.Should().BeAssignableTo<IErpInvoiceCapable>();
        adapter.Should().BeAssignableTo<IErpAccountCapable>();
        adapter.Should().BeAssignableTo<IErpStockCapable>();
    }
}
