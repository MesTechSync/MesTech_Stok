using System.Text.Json;

namespace MesTech.Tests.Integration._Shared;

/// <summary>
/// WireMock response builder for Amazon SP-API adapter contract tests.
/// LWA token, catalog, orders, feeds, RDT, and notification responses.
/// </summary>
public static class AmazonWireMockHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    // ══════════════════════════════════════
    // LWA (Login with Amazon) Auth Responses
    // ══════════════════════════════════════

    /// <summary>
    /// Builds a successful LWA token response.
    /// </summary>
    public static string BuildLwaTokenResponse(
        string accessToken = "Atza|test-access-token",
        int expiresIn = 3600,
        string tokenType = "bearer")
    {
        return JsonSerializer.Serialize(new
        {
            access_token = accessToken,
            expires_in = expiresIn,
            token_type = tokenType
        }, JsonOptions);
    }

    /// <summary>
    /// Builds an LWA error response.
    /// </summary>
    public static string BuildLwaErrorResponse(
        string error = "invalid_grant",
        string description = "The refresh token is invalid.")
    {
        return JsonSerializer.Serialize(new
        {
            error,
            error_description = description
        }, JsonOptions);
    }

    // ══════════════════════════════════════
    // RDT (Restricted Data Token) Responses
    // ══════════════════════════════════════

    /// <summary>
    /// Builds a Restricted Data Token response.
    /// </summary>
    public static string BuildRdtResponse(
        string restrictedDataToken = "Atz.sprdt|test-rdt-token",
        int expiresIn = 3600)
    {
        return JsonSerializer.Serialize(new
        {
            restrictedDataToken,
            expiresIn
        }, JsonOptions);
    }

    // ══════════════════════════════════════
    // Catalog Items Responses
    // ══════════════════════════════════════

    /// <summary>
    /// Builds a Catalog Items search response.
    /// Items should have: asin, title, sku.
    /// </summary>
    public static string BuildCatalogItemsResponse(
        (string asin, string title, string sku)[]? items = null)
    {
        var itemList = items ?? Array.Empty<(string, string, string)>();

        var catalogItems = itemList.Select(i => new
        {
            asin = i.asin,
            summaries = new[]
            {
                new
                {
                    marketplaceId = "A33AVAJ2PDY3EV",
                    itemName = i.title,
                    manufacturer = "MesTech"
                }
            },
            identifiers = new[]
            {
                new
                {
                    marketplaceId = "A33AVAJ2PDY3EV",
                    identifiers = new[]
                    {
                        new { identifier = i.sku, identifierType = "SKU" }
                    }
                }
            }
        }).ToArray();

        return JsonSerializer.Serialize(new
        {
            items = catalogItems,
            numberOfResults = catalogItems.Length
        }, JsonOptions);
    }

    // ══════════════════════════════════════
    // Orders Responses
    // ══════════════════════════════════════

    /// <summary>
    /// Builds a GetOrders response.
    /// Orders should have: orderId, status, date.
    /// </summary>
    public static string BuildOrdersResponse(
        (string orderId, string status, string date)[]? orders = null)
    {
        var orderList = orders ?? Array.Empty<(string, string, string)>();

        var orderItems = orderList.Select(o => new
        {
            AmazonOrderId = o.orderId,
            OrderStatus = o.status,
            PurchaseDate = o.date,
            OrderTotal = new { CurrencyCode = "TRY", Amount = "150.00" },
            BuyerInfo = new { BuyerEmail = "buyer@example.com", BuyerName = "Test Customer" }
        }).ToArray();

        return JsonSerializer.Serialize(new
        {
            payload = new
            {
                Orders = orderItems,
                NextToken = (string?)null
            }
        }, JsonOptions);
    }

    /// <summary>
    /// Builds a GetOrderItems response for a specific order.
    /// Items should have: sku, qty, price.
    /// </summary>
    public static string BuildOrderItemsResponse(
        string orderId,
        (string sku, int qty, decimal price)[]? items = null)
    {
        var itemList = items ?? Array.Empty<(string, int, decimal)>();

        var orderItems = itemList.Select(i => new
        {
            ASIN = $"B0{(i.sku.GetHashCode() & 0x7FFFFFFF):X8}".PadRight(12, '0')[..12],
            SellerSKU = i.sku,
            OrderItemId = $"OI-{i.sku}",
            Title = $"Product {i.sku}",
            QuantityOrdered = i.qty,
            ItemPrice = new
            {
                CurrencyCode = "TRY",
                Amount = i.price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
            },
            ItemTax = new
            {
                CurrencyCode = "TRY",
                Amount = (i.price * 0.18m).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
            }
        }).ToArray();

        return JsonSerializer.Serialize(new
        {
            payload = new
            {
                AmazonOrderId = orderId,
                OrderItems = orderItems
            }
        }, JsonOptions);
    }

    // ══════════════════════════════════════
    // Feeds Responses
    // ══════════════════════════════════════

    /// <summary>
    /// Builds a CreateFeed response.
    /// </summary>
    public static string BuildCreateFeedResponse(string feedId = "FD-12345")
    {
        return JsonSerializer.Serialize(new
        {
            feedId
        }, JsonOptions);
    }

    /// <summary>
    /// Builds a CreateFeedDocument response with upload URL.
    /// </summary>
    public static string BuildCreateFeedDocumentResponse(
        string feedDocumentId = "amzn1.tortuga.3.test-doc",
        string url = "https://tortuga-prod-na.s3.amazonaws.com/test-upload")
    {
        return JsonSerializer.Serialize(new
        {
            feedDocumentId,
            url
        }, JsonOptions);
    }

    /// <summary>
    /// Builds a GetFeed status response.
    /// </summary>
    public static string BuildFeedStatusResponse(
        string feedId = "FD-12345",
        string status = "DONE",
        int errorsCount = 0)
    {
        return JsonSerializer.Serialize(new
        {
            feedId,
            processingStatus = status,
            resultFeedDocumentId = $"amzn1.tortuga.3.result-{feedId}",
            errors = errorsCount > 0
                ? Enumerable.Range(1, errorsCount).Select(i => new { code = $"ERR-{i}", message = $"Error {i}" }).ToArray()
                : Array.Empty<object>()
        }, JsonOptions);
    }

    // ══════════════════════════════════════
    // Notifications Responses
    // ══════════════════════════════════════

    /// <summary>
    /// Builds a CreateSubscription response.
    /// </summary>
    public static string BuildSubscriptionResponse(
        string subscriptionId = "SUB-12345")
    {
        return JsonSerializer.Serialize(new
        {
            payload = new
            {
                subscriptionId,
                payloadVersion = "1.0",
                destinationId = "DEST-12345"
            }
        }, JsonOptions);
    }

    // ══════════════════════════════════════
    // Listings / PutItem Response
    // ══════════════════════════════════════

    /// <summary>
    /// Builds a successful PutListingsItem response.
    /// </summary>
    public static string BuildPutListingsItemResponse(
        string sku = "TST-001",
        string status = "ACCEPTED")
    {
        return JsonSerializer.Serialize(new
        {
            sku,
            status,
            submissionId = $"sub-{sku}",
            issues = Array.Empty<object>()
        }, JsonOptions);
    }

    /// <summary>
    /// Builds a generic SP-API error response.
    /// </summary>
    public static string BuildErrorResponse(string code, string message)
    {
        return JsonSerializer.Serialize(new
        {
            errors = new[]
            {
                new { code, message, details = "" }
            }
        }, JsonOptions);
    }
}
