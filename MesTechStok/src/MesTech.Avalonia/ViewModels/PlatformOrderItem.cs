namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Lightweight DTO used by all platform Avalonia views to display recent orders in a DataGrid.
/// </summary>
public sealed record PlatformOrderItem(
    string OrderNumber,
    string OrderDate,
    string CustomerName,
    string TotalAmount,
    string Status);
