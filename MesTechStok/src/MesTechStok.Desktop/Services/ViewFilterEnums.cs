namespace MesTechStok.Desktop.Services;

public enum CustomerTypeFilter
{
    All,
    VIP,
    Individual,
    Corporate,
    Active,
    Inactive
}

public enum CustomerSortOrder
{
    FullName,
    RegistrationDate,
    TotalPurchases
}

public enum StockStatusFilter
{
    All,
    Normal,
    Low,
    Critical,
    OutOfStock
}

public enum InventorySortOrder
{
    ProductName,
    StockQuantity,
    LastUpdated
}

public enum OrderStatusFilter
{
    All,
    Pending,
    Processing,
    Completed,
    Cancelled
}

public enum OrderSortOrder
{
    OrderDateDesc,
    OrderDateAsc,
    TotalAmount
}

public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Cancelled
}

public enum ReportTypeFilter
{
    All,
    Sales,
    Inventory,
    Financial
}

public enum ReportSortOrder
{
    CreatedDateDesc,
    CreatedDateAsc,
    Title
}

public enum ProductSortOrder
{
    Name,
    Price,
    Stock,
    CreatedDate
}
