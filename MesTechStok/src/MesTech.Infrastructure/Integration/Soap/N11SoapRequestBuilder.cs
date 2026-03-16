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

    // ── ProductSellingService ──

    public static XElement BuildActivateProductSelling(string appKey, string appSecret, long productId)
    {
        return new XElement(Ns + "ActivateProductSellingRequest",
            BuildAuth(appKey, appSecret),
            new XElement("productId", productId));
    }

    public static XElement BuildDeactivateProductSelling(string appKey, string appSecret, long productId)
    {
        return new XElement(Ns + "DeactivateProductSellingRequest",
            BuildAuth(appKey, appSecret),
            new XElement("productId", productId));
    }

    // ── InvoiceService ──

    public static XElement BuildSendInvoice(string appKey, string appSecret,
        long orderId, string invoiceNo, DateTime invoiceDate)
    {
        return new XElement(Ns + "SendInvoiceRequest",
            BuildAuth(appKey, appSecret),
            new XElement("orderId", orderId),
            new XElement("invoiceNumber", invoiceNo),
            new XElement("invoiceDate", invoiceDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));
    }

    // ── ClaimService ──

    public static XElement BuildGetClaims(string appKey, string appSecret, int currentPage, int pageSize)
    {
        return new XElement(Ns + "GetClaimsRequest",
            BuildAuth(appKey, appSecret),
            new XElement("pagingData",
                new XElement("currentPage", currentPage),
                new XElement("pageSize", pageSize)));
    }

    public static XElement BuildApproveClaim(string appKey, string appSecret, long claimId)
    {
        return new XElement(Ns + "ApproveClaimRequest",
            BuildAuth(appKey, appSecret),
            new XElement("claimId", claimId));
    }

    // ── SettlementService ──

    public static XElement BuildGetSettlements(string appKey, string appSecret,
        DateTime startDate, DateTime endDate)
    {
        return new XElement(Ns + "GetSettlementsRequest",
            BuildAuth(appKey, appSecret),
            new XElement("startDate", startDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)),
            new XElement("endDate", endDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));
    }

    // ── CategoryService (attributes) ──

    public static XElement BuildGetCategoryAttributes(string appKey, string appSecret, long categoryId)
    {
        return new XElement(Ns + "GetCategoryAttributesRequest",
            BuildAuth(appKey, appSecret),
            new XElement("categoryId", categoryId));
    }

    // ── BrandService ──

    public static XElement BuildGetBrands(string appKey, string appSecret, int currentPage, int pageSize)
    {
        return new XElement(Ns + "GetBrandsRequest",
            BuildAuth(appKey, appSecret),
            new XElement("pagingData",
                new XElement("currentPage", currentPage),
                new XElement("pageSize", pageSize)));
    }

    // ── CityService ──

    public static XElement BuildGetCities(string appKey, string appSecret)
    {
        return new XElement(Ns + "GetCitiesRequest",
            BuildAuth(appKey, appSecret));
    }
}
