namespace MesTech.Domain.Enums;

/// <summary>Stok alarm seviyesi.</summary>
public enum StockAlertLevel
{
    /// <summary>Stok &lt;= MinimumStock × 2</summary>
    Low,
    /// <summary>Stok &lt;= MinimumStock</summary>
    Critical,
    /// <summary>Stok = 0</summary>
    OutOfStock
}
