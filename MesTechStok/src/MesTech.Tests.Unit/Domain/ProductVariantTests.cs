using MesTech.Domain.Common;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// G9 C-07 — ProductVariant flexible attribute tests.
/// </summary>
public class ProductVariantTests
{
    private static readonly Guid ValidProductId = Guid.NewGuid();
    private const string ValidSku = "VAR-001";

    // ── Create ──

    [Fact]
    public void Create_ValidArgs_Returns_ProductVariant_With_Correct_Properties()
    {
        var productId = ValidProductId;

        var variant = ProductVariant.Create(productId, ValidSku, stock: 10, price: 99.99m);

        Assert.Equal(productId, variant.ProductId);
        Assert.Equal(ValidSku, variant.SKU);
        Assert.Equal(10, variant.Stock);
        Assert.Equal(99.99m, variant.Price);
        Assert.True(variant.IsActive);
        Assert.NotEqual(Guid.Empty, variant.Id);
    }

    [Fact]
    public void Create_EmptySku_Should_Throw_ArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => ProductVariant.Create(ValidProductId, string.Empty));

        Assert.NotNull(ex);
    }

    [Fact]
    public void Create_WhitespaceSku_Should_Throw_ArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => ProductVariant.Create(ValidProductId, "   "));
    }

    [Fact]
    public void Create_EmptyGuid_ProductId_Should_Throw_ArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => ProductVariant.Create(Guid.Empty, ValidSku));

        Assert.Equal("productId", ex.ParamName);
    }

    [Fact]
    public void Create_NullPrice_Defaults_To_Null()
    {
        var variant = ProductVariant.Create(ValidProductId, ValidSku);

        Assert.Null(variant.Price);
    }

    // ── SetAttribute ──

    [Fact]
    public void SetAttribute_Adds_KeyValue_To_Attributes()
    {
        var variant = ProductVariant.Create(ValidProductId, ValidSku);

        variant.SetAttribute("Color", "Red");

        Assert.True(variant.Attributes.ContainsKey("Color"));
        Assert.Equal("Red", variant.Attributes["Color"]);
    }

    [Fact]
    public void SetAttribute_Overwrites_Existing_Key()
    {
        var variant = ProductVariant.Create(ValidProductId, ValidSku);
        variant.SetAttribute("Size", "M");

        variant.SetAttribute("Size", "XL");

        Assert.Equal("XL", variant.Attributes["Size"]);
        Assert.Single(variant.Attributes, kv => kv.Key == "Size");
    }

    [Fact]
    public void SetAttribute_EmptyKey_Should_Throw_ArgumentException()
    {
        var variant = ProductVariant.Create(ValidProductId, ValidSku);

        Assert.Throws<ArgumentException>(() => variant.SetAttribute(string.Empty, "Value"));
    }

    [Fact]
    public void SetAttribute_MultipleAttributes_Are_All_Stored()
    {
        var variant = ProductVariant.Create(ValidProductId, ValidSku);

        variant.SetAttribute("Color", "Blue");
        variant.SetAttribute("Size", "L");
        variant.SetAttribute("Material", "Cotton");

        Assert.Equal(3, variant.Attributes.Count);
        Assert.Equal("Blue", variant.Attributes["Color"]);
        Assert.Equal("L", variant.Attributes["Size"]);
        Assert.Equal("Cotton", variant.Attributes["Material"]);
    }

    // ── RemoveAttribute ──

    [Fact]
    public void RemoveAttribute_Removes_Existing_Key()
    {
        var variant = ProductVariant.Create(ValidProductId, ValidSku);
        variant.SetAttribute("Color", "Red");
        variant.SetAttribute("Size", "M");

        variant.RemoveAttribute("Color");

        Assert.False(variant.Attributes.ContainsKey("Color"));
        Assert.True(variant.Attributes.ContainsKey("Size"));
    }

    [Fact]
    public void RemoveAttribute_NonExistent_Key_Does_Not_Throw()
    {
        var variant = ProductVariant.Create(ValidProductId, ValidSku);

        // Should not throw
        var ex = Record.Exception(() => variant.RemoveAttribute("NonExistentKey"));
        Assert.Null(ex);
    }

    // ── GetAttribute ──

    [Fact]
    public void GetAttribute_Returns_Correct_Value_For_Existing_Key()
    {
        var variant = ProductVariant.Create(ValidProductId, ValidSku);
        variant.SetAttribute("Color", "Green");

        var value = variant.GetAttribute("Color");

        Assert.Equal("Green", value);
    }

    [Fact]
    public void GetAttribute_Returns_Null_For_Missing_Key()
    {
        var variant = ProductVariant.Create(ValidProductId, ValidSku);

        var value = variant.GetAttribute("NonExistent");

        Assert.Null(value);
    }

    // ── Attributes immutability ──

    [Fact]
    public void Attributes_Is_IReadOnlyDictionary_Cannot_Cast_And_Add()
    {
        var variant = ProductVariant.Create(ValidProductId, ValidSku);
        variant.SetAttribute("Key", "Value");

        // Attributes should return IReadOnlyDictionary, not IDictionary
        var attrs = variant.Attributes;
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(attrs);

        // ReadOnlyDictionary implements IDictionary but throws on mutation
        if (attrs is IDictionary<string, string> mutableDict)
        {
            Assert.Throws<NotSupportedException>(() => mutableDict.Add("Hack", "Value"));
        }
        else
        {
            // Not castable to IDictionary — still verify external mutation is impossible
            // by confirming the original variant's attributes remain untouched
            Assert.Single(variant.Attributes);
            Assert.Equal("Value", variant.GetAttribute("Key"));
        }

        // Regardless of cast path, internal state must be intact after immutability check
        Assert.Single(variant.Attributes);
        Assert.Equal("Value", variant.GetAttribute("Key"));
    }

    // ── AttributesJson round-trip ──

    [Fact]
    public void AttributesJson_Serializes_And_Deserializes_Correctly()
    {
        var variant = ProductVariant.Create(ValidProductId, ValidSku);
        variant.SetAttribute("Color", "Red");
        variant.SetAttribute("Size", "M");

        // Simulate EF round-trip via AttributesJson
        var json = variant.AttributesJson;
        Assert.Contains("Color", json);
        Assert.Contains("Red", json);

        // Simulate EF loading from DB
        var loadedVariant = ProductVariant.Create(ValidProductId, "VAR-002");
        loadedVariant.AttributesJson = json;

        Assert.Equal("Red", loadedVariant.GetAttribute("Color"));
        Assert.Equal("M", loadedVariant.GetAttribute("Size"));
    }

    // ── BaseEntity ──

    [Fact]
    public void ProductVariant_Should_Inherit_BaseEntity()
    {
        var variant = ProductVariant.Create(ValidProductId, ValidSku);

        Assert.IsAssignableFrom<BaseEntity>(variant);
        Assert.NotEqual(Guid.Empty, variant.Id);
    }

    // ── Default state ──

    [Fact]
    public void Create_Variant_Has_Empty_Attributes_By_Default()
    {
        var variant = ProductVariant.Create(ValidProductId, ValidSku);

        Assert.Empty(variant.Attributes);
    }
}
