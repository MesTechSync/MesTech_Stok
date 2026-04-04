using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MesTech.Avalonia.Converters;

/// <summary>
/// WPF014: Converts a stock level to a background brush based on stock status.
/// Accepts int (CurrentStock with optional string MinStock parameter) or
/// a pre-computed color string (e.g. "#FFEBEE", "Transparent").
/// - Stock == 0                   → Red   (#FFEBEE)
/// - Stock &lt; MinStock (parameter) → Yellow (#FFF8E1)
/// - else / "Transparent"         → Transparent
/// </summary>
public class StockLevelToBrushConverter : IValueConverter
{
    private static Color Token(string key) =>
        global::Avalonia.Application.Current?.FindResource(key) is Color c ? c : Colors.Gray;

    private static IBrush OutOfStockBrush => new SolidColorBrush(Token("MesStockOutBg"));
    private static IBrush LowStockBrush   => new SolidColorBrush(Token("MesStockLowBg"));
    private static IBrush NormalBrush     => Brushes.Transparent;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Pre-computed color string path (from RowBackground property)
        if (value is string colorStr)
        {
            return colorStr.ToUpperInvariant() switch
            {
                "#FFEBEE" or "OUTOFSTOCK" => OutOfStockBrush,
                "#FFF8E1" or "LOWSTOCK"   => LowStockBrush,
                _                         => NormalBrush
            };
        }

        // Int stock level path
        if (value is not int stock)
            return NormalBrush;

        if (stock == 0)
            return OutOfStockBrush;

        if (parameter is string strParam && int.TryParse(strParam, out var minStock) && stock < minStock)
            return LowStockBrush;

        return NormalBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
