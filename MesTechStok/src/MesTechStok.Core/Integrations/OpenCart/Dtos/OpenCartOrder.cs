using Newtonsoft.Json;

namespace MesTechStok.Core.Integrations.OpenCart.Dtos;

/// <summary>
/// OpenCart sipariş veri transfer modeli
/// </summary>
public class OpenCartOrder
{
    [JsonProperty("order_id")]
    public int OrderId { get; set; }

    [JsonProperty("invoice_no")]
    public int InvoiceNo { get; set; }

    [JsonProperty("store_id")]
    public int StoreId { get; set; }

    [JsonProperty("customer_id")]
    public int CustomerId { get; set; }

    [JsonProperty("firstname")]
    public string? FirstName { get; set; }

    [JsonProperty("lastname")]
    public string? LastName { get; set; }

    [JsonProperty("email")]
    public string? Email { get; set; }

    [JsonProperty("telephone")]
    public string? Telephone { get; set; }

    [JsonProperty("payment_method")]
    public string? PaymentMethod { get; set; }

    [JsonProperty("shipping_method")]
    public string? ShippingMethod { get; set; }

    [JsonProperty("shipping_address_1")]
    public string? ShippingAddress1 { get; set; }

    [JsonProperty("shipping_city")]
    public string? ShippingCity { get; set; }

    [JsonProperty("comment")]
    public string? Comment { get; set; }

    [JsonProperty("total")]
    public decimal Total { get; set; }

    [JsonProperty("order_status_id")]
    public int OrderStatusId { get; set; }

    [JsonProperty("order_status")]
    public string? OrderStatus { get; set; }

    [JsonProperty("currency_code")]
    public string? CurrencyCode { get; set; }

    [JsonProperty("date_added")]
    public string? DateAdded { get; set; }

    [JsonProperty("date_modified")]
    public string? DateModified { get; set; }

    [JsonProperty("products")]
    public List<OpenCartOrderProduct> Products { get; set; } = new();

    [JsonIgnore]
    public DateTime? LastSyncDate { get; set; }

    [JsonIgnore]
    public bool IsProcessed { get; set; }
}

/// <summary>
/// OpenCart sipariş ürün modeli
/// </summary>
public class OpenCartOrderProduct
{
    [JsonProperty("order_product_id")]
    public int OrderProductId { get; set; }

    [JsonProperty("product_id")]
    public int ProductId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;

    [JsonProperty("quantity")]
    public int Quantity { get; set; }

    [JsonProperty("price")]
    public decimal Price { get; set; }

    [JsonProperty("total")]
    public decimal Total { get; set; }
}

/// <summary>
/// OpenCart kategori modeli
/// </summary>
public class OpenCartCategory
{
    [JsonProperty("category_id")]
    public int CategoryId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("parent_id")]
    public int ParentId { get; set; }

    [JsonProperty("status")]
    public bool Status { get; set; }

    [JsonProperty("sort_order")]
    public int SortOrder { get; set; }
}

/// <summary>
/// OpenCart müşteri modeli
/// </summary>
public class OpenCartCustomer
{
    [JsonProperty("customer_id")]
    public int CustomerId { get; set; }

    [JsonProperty("firstname")]
    public string FirstName { get; set; } = string.Empty;

    [JsonProperty("lastname")]
    public string LastName { get; set; } = string.Empty;

    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("telephone")]
    public string? Telephone { get; set; }

    [JsonProperty("status")]
    public bool Status { get; set; }

    [JsonProperty("date_added")]
    public string? DateAdded { get; set; }
}
