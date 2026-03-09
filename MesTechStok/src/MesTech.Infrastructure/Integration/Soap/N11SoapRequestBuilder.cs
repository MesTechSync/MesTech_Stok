using System.Globalization;
using System.Xml.Linq;

namespace MesTech.Infrastructure.Integration.Soap;

/// <summary>
/// N11 SOAP request body builder — XElement tree constructor per service method.
/// Namespace: http://www.n11.com/ws/schemas
/// Auth: Authentication element with appKey + appSecret in every request body.
/// </summary>
public static class N11SoapRequestBuilder
{
    private static readonly XNamespace Ns = "http://www.n11.com/ws/schemas";

    public static XElement BuildAuth(string appKey, string appSecret)
    {
        return new XElement("auth",
            new XElement("appKey", appKey),
            new XElement("appSecret", appSecret));
    }

    // ── ProductService ──

    public static XElement BuildGetProducts(string appKey, string appSecret, int currentPage, int pageSize)
    {
        return new XElement(Ns + "GetProductListRequest",
            BuildAuth(appKey, appSecret),
            new XElement("pagingData",
                new XElement("currentPage", currentPage),
                new XElement("pageSize", pageSize)));
    }

    public static XElement BuildSaveProduct(string appKey, string appSecret,
        string productSellerCode, string title, long categoryId,
        decimal price, int stockQuantity, string? description = null)
    {
        return new XElement(Ns + "SaveProductRequest",
            BuildAuth(appKey, appSecret),
            new XElement("product",
                new XElement("productSellerCode", productSellerCode),
                new XElement("title", title),
                new XElement("category",
                    new XElement("id", categoryId)),
                new XElement("price", price.ToString(CultureInfo.InvariantCulture)),
                new XElement("stockItems",
                    new XElement("stockItem",
                        new XElement("quantity", stockQuantity))),
                description is not null ? new XElement("description", description) : null!));
    }

    public static XElement BuildUpdateStock(string appKey, string appSecret, long productId, int quantity)
    {
        return new XElement(Ns + "UpdateStockByStockIdRequest",
            BuildAuth(appKey, appSecret),
            new XElement("stockItems",
                new XElement("stockItem",
                    new XElement("productId", productId),
                    new XElement("quantity", quantity))));
    }

    public static XElement BuildUpdatePrice(string appKey, string appSecret,
        long productId, decimal price, decimal? listPrice = null)
    {
        var request = new XElement(Ns + "UpdateProductPriceByIdRequest",
            BuildAuth(appKey, appSecret),
            new XElement("productId", productId),
            new XElement("price", price.ToString(CultureInfo.InvariantCulture)),
            new XElement("currencyType", "1")); // TRY

        if (listPrice.HasValue)
        {
            request.Add(new XElement("optionPrice",
                new XElement("listPrice", listPrice.Value.ToString(CultureInfo.InvariantCulture))));
        }

        return request;
    }

    // ── OrderService ──

    public static XElement BuildGetOrders(string appKey, string appSecret,
        string? status, int currentPage, int pageSize)
    {
        var searchData = new XElement("searchData",
            new XElement("productSellerCode"),
            new XElement("buyerName"));

        if (!string.IsNullOrEmpty(status))
            searchData.Add(new XElement("status", status));

        return new XElement(Ns + "DetailedOrderListRequest",
            BuildAuth(appKey, appSecret),
            searchData,
            new XElement("pagingData",
                new XElement("currentPage", currentPage),
                new XElement("pageSize", pageSize)));
    }

    public static XElement BuildUpdateOrderStatus(string appKey, string appSecret,
        long orderItemId, string status)
    {
        return new XElement(Ns + "OrderItemAcceptRequest",
            BuildAuth(appKey, appSecret),
            new XElement("orderItemList",
                new XElement("orderItem",
                    new XElement("id", orderItemId),
                    new XElement("status", status))));
    }

    // ── CategoryService ──

    public static XElement BuildGetCategories(string appKey, string appSecret)
    {
        return new XElement(Ns + "GetTopLevelCategoriesRequest",
            BuildAuth(appKey, appSecret));
    }

    public static XElement BuildGetSubCategories(string appKey, string appSecret, long parentCategoryId)
    {
        return new XElement(Ns + "GetSubCategoriesRequest",
            BuildAuth(appKey, appSecret),
            new XElement("categoryId", parentCategoryId));
    }

    // ── ShipmentService ──

    public static XElement BuildUpdateShipment(string appKey, string appSecret,
        long orderItemId, string shipmentCompany, string trackingNumber)
    {
        return new XElement(Ns + "MakeOrderItemShipmentRequest",
            BuildAuth(appKey, appSecret),
            new XElement("orderItemList",
                new XElement("orderItem",
                    new XElement("id", orderItemId),
                    new XElement("shipmentCompany", shipmentCompany),
                    new XElement("trackingNumber", trackingNumber))));
    }

    // ── CityService ──

    public static XElement BuildGetCities(string appKey, string appSecret)
    {
        return new XElement(Ns + "GetCitiesRequest",
            BuildAuth(appKey, appSecret));
    }
}
