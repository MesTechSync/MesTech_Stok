using MesTechStok.Core.Integrations.OpenCart.Dtos;
using MesTechStok.Core.Data.Models;

namespace MesTechStok.Core.Integrations.OpenCart;

/// <summary>
/// Extension methods for OpenCart DTOs to provide backward compatibility
/// </summary>
public static class OpenCartExtensions
{
    /// <summary>
    /// Gets the SKU from OpenCart Product (Model field)
    /// </summary>
    public static string SKU(this OpenCartProduct product) => product.Model;

    /// <summary>
    /// Gets the Name from OpenCart Product Description
    /// </summary>
    public static string Name(this OpenCartProduct product)
    {
        var description = product.ProductDescriptions?.FirstOrDefault();
        return description?.Name ?? string.Empty;
    }

    /// <summary>
    /// Gets the Description from OpenCart Product Description
    /// </summary>
    public static string Description(this OpenCartProduct product)
    {
        var description = product.ProductDescriptions?.FirstOrDefault();
        return description?.Description ?? string.Empty;
    }

    /// <summary>
    /// Gets the Sale Price from OpenCart Product
    /// </summary>
    public static decimal SalePrice(this OpenCartProduct product) => product.Price;

    /// <summary>
    /// Gets the ID from OpenCart Product
    /// </summary>
    public static int Id(this OpenCartProduct product) => product.ProductId;

    /// <summary>
    /// Gets the ID from OpenCart Category
    /// </summary>
    public static int Id(this OpenCartCategory category) => category.CategoryId;

    /// <summary>
    /// Gets the Customer Name from OpenCart Order
    /// </summary>
    public static string Customer(this OpenCartOrder order) => $"{order.FirstName} {order.LastName}".Trim();

    /// <summary>
    /// Gets the Customer Name from OpenCart Order
    /// </summary>
    public static string CustomerName(this OpenCartOrder order) => $"{order.FirstName} {order.LastName}".Trim();

    /// <summary>
    /// Gets the Customer Email from OpenCart Order
    /// </summary>
    public static string CustomerEmail(this OpenCartOrder order) => order.Email ?? string.Empty;

    /// <summary>
    /// Gets the Sale Price from OpenCart Order Product
    /// </summary>
    public static decimal SalePrice(this OpenCartOrderProduct product) => product.Price;

    /// <summary>
    /// Maps local OrderStatus to OpenCart order status ID
    /// </summary>
    public static int MapOrderStatusToOpenCart(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Pending => 1, // Pending
            OrderStatus.Confirmed => 2, // Processing
            OrderStatus.Shipped => 3, // Shipped
            OrderStatus.Delivered => 5, // Complete
            OrderStatus.Cancelled => 7, // Canceled
            _ => 1 // Default to Pending
        };
    }

    /// <summary>
    /// Maps OpenCart order status ID to local OrderStatus
    /// </summary>
    public static OrderStatus MapOpenCartToOrderStatus(int openCartStatusId)
    {
        return openCartStatusId switch
        {
            1 => OrderStatus.Pending, // Pending
            2 => OrderStatus.Confirmed, // Processing
            3 => OrderStatus.Shipped, // Shipped
            5 => OrderStatus.Delivered, // Complete
            7 => OrderStatus.Cancelled, // Canceled
            _ => OrderStatus.Pending // Default to Pending
        };
    }
}