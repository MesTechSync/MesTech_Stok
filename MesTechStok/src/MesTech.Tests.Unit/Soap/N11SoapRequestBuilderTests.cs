using System.Xml.Linq;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Soap;
using Xunit;

namespace MesTech.Tests.Unit.Soap;

[Trait("Category", "Unit")]
public class N11SoapRequestBuilderTests
{
    private const string TestKey = "test-app-key";
    private const string TestSecret = "test-app-secret";

    [Fact]
    public void BuildAuth_WithValidCredentials_ShouldContainAppKeyAndSecret()
    {
        var auth = N11SoapRequestBuilder.BuildAuth(TestKey, TestSecret);
        auth.Element("appKey")!.Value.Should().Be(TestKey);
        auth.Element("appSecret")!.Value.Should().Be(TestSecret);
    }

    [Fact]
    public void BuildAuth_WithXmlSpecialCharsInCredentials_ShouldEscapeXmlSpecialChars()
    {
        // XElement auto-escapes &, <, >, " in text content
        var auth = N11SoapRequestBuilder.BuildAuth("key<>&\"test", "secret<>&\"test");
        auth.Element("appKey")!.Value.Should().Be("key<>&\"test");
        auth.Element("appSecret")!.Value.Should().Be("secret<>&\"test");
    }

    [Fact]
    public void BuildGetProducts_WithPaginationAndAuth_ShouldContainPaginationAndAuth()
    {
        var body = N11SoapRequestBuilder.BuildGetProducts(TestKey, TestSecret, 2, 50);
        var xml = body.ToString();

        xml.Should().Contain("currentPage");
        xml.Should().Contain("pageSize");
        xml.Should().Contain(">2<");   // currentPage value
        xml.Should().Contain(">50<");  // pageSize value
        body.Element("auth").Should().NotBeNull();
    }

    [Fact]
    public void BuildSaveProduct_WithAllProductFields_ShouldContainAllProductFields()
    {
        var body = N11SoapRequestBuilder.BuildSaveProduct(
            TestKey, TestSecret, "SKU-001", "Test Product", 1001, 99.99m, 10, "Test desc");
        var xml = body.ToString();

        xml.Should().Contain("SKU-001");
        xml.Should().Contain("Test Product");
        xml.Should().Contain("1001");
        xml.Should().Contain("99.99");  // InvariantCulture
        xml.Should().Contain("10");
        xml.Should().Contain("Test desc");
    }

    [Fact]
    public void BuildSaveProduct_WithoutDescription_ShouldOmitDescriptionElement()
    {
        var body = N11SoapRequestBuilder.BuildSaveProduct(
            TestKey, TestSecret, "SKU-002", "No Desc", 1002, 50.00m, 5);
        body.ToString().Should().NotContain("description");
    }

    [Fact]
    public void BuildUpdateStock_WithProductIdAndQuantity_ShouldContainProductIdAndQuantity()
    {
        var body = N11SoapRequestBuilder.BuildUpdateStock(TestKey, TestSecret, 12345, 50);
        var xml = body.ToString();

        xml.Should().Contain(">12345<");
        xml.Should().Contain(">50<");
    }

    [Fact]
    public void BuildUpdatePrice_WithListPrice_ShouldContainOptionAndListPrice()
    {
        var body = N11SoapRequestBuilder.BuildUpdatePrice(TestKey, TestSecret, 12345, 199.99m, 249.99m);
        var xml = body.ToString();

        xml.Should().Contain("199.99");
        xml.Should().Contain("249.99");
        xml.Should().Contain("optionPrice");
        xml.Should().Contain("listPrice");
    }

    [Fact]
    public void BuildUpdatePrice_WithoutListPrice_ShouldNotContainOptionPrice()
    {
        var body = N11SoapRequestBuilder.BuildUpdatePrice(TestKey, TestSecret, 12345, 199.99m);
        body.ToString().Should().NotContain("optionPrice");
    }

    [Fact]
    public void BuildGetOrders_WithStatus_ShouldContainStatusElement()
    {
        var body = N11SoapRequestBuilder.BuildGetOrders(TestKey, TestSecret, "New", 0, 50);
        var xml = body.ToString();

        xml.Should().Contain(">New<");
        xml.Should().Contain("currentPage");
        xml.Should().Contain("pageSize");
    }

    [Fact]
    public void BuildGetOrders_WithoutStatus_ShouldNotContainStatusElement()
    {
        var body = N11SoapRequestBuilder.BuildGetOrders(TestKey, TestSecret, null, 0, 100);
        var searchData = body.Element("searchData");
        searchData.Should().NotBeNull();
        searchData!.Element("status").Should().BeNull();
    }

    [Fact]
    public void BuildUpdateOrderStatus_WithOrderItemIdAndStatus_ShouldContainOrderItemIdAndStatus()
    {
        var body = N11SoapRequestBuilder.BuildUpdateOrderStatus(TestKey, TestSecret, 999, "Approved");
        var xml = body.ToString();

        xml.Should().Contain(">999<");
        xml.Should().Contain(">Approved<");
    }

    [Fact]
    public void BuildGetCategories_ForTopLevel_ShouldHaveCorrectRequestNameAndAuth()
    {
        var body = N11SoapRequestBuilder.BuildGetCategories(TestKey, TestSecret);
        body.Name.LocalName.Should().Be("GetTopLevelCategoriesRequest");
        body.Element("auth").Should().NotBeNull();
    }

    [Fact]
    public void BuildGetSubCategories_WithParentId_ShouldContainParentId()
    {
        var body = N11SoapRequestBuilder.BuildGetSubCategories(TestKey, TestSecret, 1001);
        body.Name.LocalName.Should().Be("GetSubCategoriesRequest");
        body.ToString().Should().Contain(">1001<");
    }

    [Fact]
    public void BuildUpdateShipment_WithShipmentFields_ShouldContainShipmentFields()
    {
        var body = N11SoapRequestBuilder.BuildUpdateShipment(TestKey, TestSecret, 999, "Yurtici", "TR12345");
        var xml = body.ToString();

        xml.Should().Contain(">999<");
        xml.Should().Contain(">Yurtici<");
        xml.Should().Contain(">TR12345<");
    }

    [Fact]
    public void BuildGetCities_WhenCalled_ShouldHaveCorrectRequestNameAndAuth()
    {
        var body = N11SoapRequestBuilder.BuildGetCities(TestKey, TestSecret);
        body.Name.LocalName.Should().Be("GetCitiesRequest");
        body.Element("auth").Should().NotBeNull();
    }

    [Fact]
    public void AllBuilders_WhenCalled_ShouldUseN11Namespace()
    {
        XNamespace ns = "http://www.n11.com/ws/schemas";

        N11SoapRequestBuilder.BuildGetProducts(TestKey, TestSecret, 0, 10).Name.Namespace.Should().Be(ns);
        N11SoapRequestBuilder.BuildGetOrders(TestKey, TestSecret, null, 0, 10).Name.Namespace.Should().Be(ns);
        N11SoapRequestBuilder.BuildGetCategories(TestKey, TestSecret).Name.Namespace.Should().Be(ns);
        N11SoapRequestBuilder.BuildGetCities(TestKey, TestSecret).Name.Namespace.Should().Be(ns);
        N11SoapRequestBuilder.BuildUpdateShipment(TestKey, TestSecret, 1, "x", "y").Name.Namespace.Should().Be(ns);
    }
}
