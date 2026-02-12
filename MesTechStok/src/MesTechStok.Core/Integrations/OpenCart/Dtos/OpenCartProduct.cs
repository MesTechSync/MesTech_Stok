using Newtonsoft.Json;

namespace MesTechStok.Core.Integrations.OpenCart.Dtos;

/// <summary>
/// OpenCart ürün veri transfer modeli
/// OpenCart API ile veri alışverişinde kullanılır
/// </summary>
public class OpenCartProduct
{
    [JsonProperty("product_id")]
    public int ProductId { get; set; }

    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty; // SKU equivalent

    [JsonProperty("sku")]
    public string? Sku { get; set; }

    [JsonProperty("upc")]
    public string? Upc { get; set; } // Barcode

    [JsonProperty("ean")]
    public string? Ean { get; set; } // Alternative Barcode

    [JsonProperty("jan")]
    public string? Jan { get; set; } // Japanese Article Number

    [JsonProperty("isbn")]
    public string? Isbn { get; set; } // International Standard Book Number

    [JsonProperty("mpn")]
    public string? Mpn { get; set; } // Manufacturer Part Number

    [JsonProperty("location")]
    public string? Location { get; set; }

    [JsonProperty("quantity")]
    public int Quantity { get; set; }

    [JsonProperty("minimum")]
    public int Minimum { get; set; } // Minimum stock level

    [JsonProperty("subtract")]
    public bool Subtract { get; set; } = true; // Subtract from stock on sale

    [JsonProperty("stock_status_id")]
    public int StockStatusId { get; set; }

    [JsonProperty("date_available")]
    public string? DateAvailable { get; set; }

    [JsonProperty("manufacturer_id")]
    public int ManufacturerId { get; set; }

    [JsonProperty("shipping")]
    public bool Shipping { get; set; } = true;

    [JsonProperty("price")]
    public decimal Price { get; set; }

    [JsonProperty("points")]
    public int Points { get; set; }

    [JsonProperty("tax_class_id")]
    public int TaxClassId { get; set; }

    [JsonProperty("date_added")]
    public string? DateAdded { get; set; }

    [JsonProperty("date_modified")]
    public string? DateModified { get; set; }

    [JsonProperty("weight")]
    public decimal Weight { get; set; }

    [JsonProperty("weight_class_id")]
    public int WeightClassId { get; set; }

    [JsonProperty("length")]
    public decimal Length { get; set; }

    [JsonProperty("width")]
    public decimal Width { get; set; }

    [JsonProperty("height")]
    public decimal Height { get; set; }

    [JsonProperty("length_class_id")]
    public int LengthClassId { get; set; }

    [JsonProperty("status")]
    public bool Status { get; set; } = true; // Product enabled/disabled

    [JsonProperty("sort_order")]
    public int SortOrder { get; set; }

    [JsonProperty("image")]
    public string? Image { get; set; }

    // Product Description (multi-language support)
    [JsonProperty("product_description")]
    public List<OpenCartProductDescription> ProductDescriptions { get; set; } = new();

    // Product Categories
    [JsonProperty("product_category")]
    public List<int> CategoryIds { get; set; } = new();

    // Product Images
    [JsonProperty("product_image")]
    public List<OpenCartProductImage> Images { get; set; } = new();

    // Product Options
    [JsonProperty("product_option")]
    public List<OpenCartProductOption> Options { get; set; } = new();

    // Custom Fields
    [JsonProperty("product_custom_field")]
    public List<OpenCartCustomField> CustomFields { get; set; } = new();

    // SEO URL
    [JsonProperty("keyword")]
    public string? SeoKeyword { get; set; }

    // Additional properties for internal use
    [JsonIgnore]
    public DateTime? LastSyncDate { get; set; }

    [JsonIgnore]
    public bool IsModified { get; set; }

    [JsonIgnore]
    public List<string> SyncErrors { get; set; } = new();
}

/// <summary>
/// OpenCart ürün açıklama modeli (çoklu dil desteği)
/// </summary>
public class OpenCartProductDescription
{
    [JsonProperty("language_id")]
    public int LanguageId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("tag")]
    public string? Tag { get; set; }

    [JsonProperty("meta_title")]
    public string? MetaTitle { get; set; }

    [JsonProperty("meta_description")]
    public string? MetaDescription { get; set; }

    [JsonProperty("meta_keyword")]
    public string? MetaKeyword { get; set; }
}

/// <summary>
/// OpenCart ürün resim modeli
/// </summary>
public class OpenCartProductImage
{
    [JsonProperty("product_image_id")]
    public int ProductImageId { get; set; }

    [JsonProperty("image")]
    public string Image { get; set; } = string.Empty;

    [JsonProperty("sort_order")]
    public int SortOrder { get; set; }
}

/// <summary>
/// OpenCart ürün seçenek modeli
/// </summary>
public class OpenCartProductOption
{
    [JsonProperty("product_option_id")]
    public int ProductOptionId { get; set; }

    [JsonProperty("option_id")]
    public int OptionId { get; set; }

    [JsonProperty("option_value")]
    public List<OpenCartProductOptionValue> OptionValues { get; set; } = new();

    [JsonProperty("required")]
    public bool Required { get; set; }
}

/// <summary>
/// OpenCart ürün seçenek değer modeli
/// </summary>
public class OpenCartProductOptionValue
{
    [JsonProperty("product_option_value_id")]
    public int ProductOptionValueId { get; set; }

    [JsonProperty("option_value_id")]
    public int OptionValueId { get; set; }

    [JsonProperty("quantity")]
    public int Quantity { get; set; }

    [JsonProperty("subtract")]
    public bool Subtract { get; set; }

    [JsonProperty("price")]
    public decimal Price { get; set; }

    [JsonProperty("price_prefix")]
    public string PricePrefix { get; set; } = "+";

    [JsonProperty("points")]
    public int Points { get; set; }

    [JsonProperty("points_prefix")]
    public string PointsPrefix { get; set; } = "+";

    [JsonProperty("weight")]
    public decimal Weight { get; set; }

    [JsonProperty("weight_prefix")]
    public string WeightPrefix { get; set; } = "+";
}

/// <summary>
/// OpenCart özel alan modeli
/// </summary>
public class OpenCartCustomField
{
    [JsonProperty("custom_field_id")]
    public int CustomFieldId { get; set; }

    [JsonProperty("custom_field_value_id")]
    public int CustomFieldValueId { get; set; }

    [JsonProperty("value")]
    public string? Value { get; set; }
}
