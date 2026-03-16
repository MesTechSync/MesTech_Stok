using System.Xml.Linq;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Soap;
using Xunit;

namespace MesTech.Tests.Unit.Soap;

[Trait("Category", "Unit")]
public class N11SoapRequestBuilderNewEndpointTests
{
    private const string TestKey = "test-app-key";
    private const string TestSecret = "test-app-secret";
    private static readonly XNamespace Ns = "http://www.n11.com/ws/schemas";

    // ── ProductSellingService ──

    [Fact]
    public void BuildActivateProductSelling_ContainsProductId()
    {
        var body = N11SoapRequestBuilder.BuildActivateProductSelling(TestKey, TestSecret, 12345);
        var xml = body.ToString();

        body.Name.LocalName.Should().Be("ActivateProductSellingRequest");
        body.Name.Namespace.Should().Be(Ns);
        xml.Should().Contain(">12345<");
        body.Element("auth").Should().NotBeNull();
    }

    [Fact]
    public void BuildDeactivateProductSelling_ContainsProductId()
    {
        var body = N11SoapRequestBuilder.BuildDeactivateProductSelling(TestKey, TestSecret, 99999);
        var xml = body.ToString();

        body.Name.LocalName.Should().Be("DeactivateProductSellingRequest");
        body.Name.Namespace.Should().Be(Ns);
        xml.Should().Contain(">99999<");
        body.Element("auth").Should().NotBeNull();
    }

    // ── InvoiceService ──

    [Fact]
    public void BuildSendInvoice_ContainsOrderIdAndInvoiceNo()
    {
        var date = new DateTime(2026, 3, 16);
        var body = N11SoapRequestBuilder.BuildSendInvoice(TestKey, TestSecret, 777, "INV-001", date);
        var xml = body.ToString();

        body.Name.LocalName.Should().Be("SendInvoiceRequest");
        body.Name.Namespace.Should().Be(Ns);
        xml.Should().Contain(">777<");
        xml.Should().Contain("INV-001");
        xml.Should().Contain("16/03/2026");
        body.Element("auth").Should().NotBeNull();
    }

    // ── ClaimService ──

    [Fact]
    public void BuildGetClaims_ContainsPagination()
    {
        var body = N11SoapRequestBuilder.BuildGetClaims(TestKey, TestSecret, 0, 50);
        var xml = body.ToString();

        body.Name.LocalName.Should().Be("GetClaimsRequest");
        body.Name.Namespace.Should().Be(Ns);
        xml.Should().Contain("currentPage");
        xml.Should().Contain("pageSize");
        body.Element("auth").Should().NotBeNull();
    }

    [Fact]
    public void BuildApproveClaim_ContainsClaimId()
    {
        var body = N11SoapRequestBuilder.BuildApproveClaim(TestKey, TestSecret, 5555);
        var xml = body.ToString();

        body.Name.LocalName.Should().Be("ApproveClaimRequest");
        body.Name.Namespace.Should().Be(Ns);
        xml.Should().Contain(">5555<");
        body.Element("auth").Should().NotBeNull();
    }

    // ── SettlementService ──

    [Fact]
    public void BuildGetSettlements_ContainsDateRange()
    {
        var start = new DateTime(2026, 3, 1);
        var end = new DateTime(2026, 3, 31);
        var body = N11SoapRequestBuilder.BuildGetSettlements(TestKey, TestSecret, start, end);
        var xml = body.ToString();

        body.Name.LocalName.Should().Be("GetSettlementsRequest");
        body.Name.Namespace.Should().Be(Ns);
        xml.Should().Contain("01/03/2026");
        xml.Should().Contain("31/03/2026");
        body.Element("auth").Should().NotBeNull();
    }

    // ── CategoryService (attributes) ──

    [Fact]
    public void BuildGetCategoryAttributes_ContainsCategoryId()
    {
        var body = N11SoapRequestBuilder.BuildGetCategoryAttributes(TestKey, TestSecret, 1001);
        var xml = body.ToString();

        body.Name.LocalName.Should().Be("GetCategoryAttributesRequest");
        body.Name.Namespace.Should().Be(Ns);
        xml.Should().Contain(">1001<");
        body.Element("auth").Should().NotBeNull();
    }

    // ── BrandService ──

    [Fact]
    public void BuildGetBrands_ContainsPagination()
    {
        var body = N11SoapRequestBuilder.BuildGetBrands(TestKey, TestSecret, 0, 100);
        var xml = body.ToString();

        body.Name.LocalName.Should().Be("GetBrandsRequest");
        body.Name.Namespace.Should().Be(Ns);
        xml.Should().Contain("currentPage");
        xml.Should().Contain("pageSize");
        body.Element("auth").Should().NotBeNull();
    }

    [Fact]
    public void AllNewBuilders_UseN11Namespace()
    {
        N11SoapRequestBuilder.BuildActivateProductSelling(TestKey, TestSecret, 1).Name.Namespace.Should().Be(Ns);
        N11SoapRequestBuilder.BuildDeactivateProductSelling(TestKey, TestSecret, 1).Name.Namespace.Should().Be(Ns);
        N11SoapRequestBuilder.BuildSendInvoice(TestKey, TestSecret, 1, "x", DateTime.UtcNow).Name.Namespace.Should().Be(Ns);
        N11SoapRequestBuilder.BuildGetClaims(TestKey, TestSecret, 0, 10).Name.Namespace.Should().Be(Ns);
        N11SoapRequestBuilder.BuildApproveClaim(TestKey, TestSecret, 1).Name.Namespace.Should().Be(Ns);
        N11SoapRequestBuilder.BuildGetSettlements(TestKey, TestSecret, DateTime.UtcNow, DateTime.UtcNow).Name.Namespace.Should().Be(Ns);
        N11SoapRequestBuilder.BuildGetCategoryAttributes(TestKey, TestSecret, 1).Name.Namespace.Should().Be(Ns);
        N11SoapRequestBuilder.BuildGetBrands(TestKey, TestSecret, 0, 10).Name.Namespace.Should().Be(Ns);
    }
}
