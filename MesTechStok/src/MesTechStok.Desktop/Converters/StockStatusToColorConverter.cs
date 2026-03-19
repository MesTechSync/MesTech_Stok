using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MesTechStok.Desktop.Converters
{
    /// <summary>
    /// Stock Status to Color Converter - EMR-06 4 seviye renk tablosu
    /// Stok durumunu renge dönüştürür (enum + string desteği)
    ///
    /// EMR-06 Renk Tablosu:
    ///   Stok = 0                      -> TÜKENDİ   (#D32F2F kırmızı)
    ///   Stok &lt;= MinimumStock       -> KRİTİK    (#D32F2F kırmızı)
    ///   Stok &lt;= MinimumStock x 1.5 -> DÜŞÜK     (#F57C00 turuncu)
    ///   Stok &gt; MinimumStock x 1.5  -> YETERLİ   (#388E3C yeşil)
    /// </summary>
    public class StockStatusToColorConverter : IValueConverter
    {
        // EMR-06 palette (all panels MUST use the same colours)
        private static readonly SolidColorBrush RedBrush = new(Color.FromRgb(0xD3, 0x2F, 0x2F));
        private static readonly SolidColorBrush OrangeBrush = new(Color.FromRgb(0xF5, 0x7C, 0x00));
        private static readonly SolidColorBrush GreenBrush = new(Color.FromRgb(0x38, 0x8E, 0x3C));
        private static readonly SolidColorBrush GrayBrush = new(Color.FromRgb(0x75, 0x75, 0x75));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Handle Models.StockStatus enum
            if (value is Models.StockStatus enumStatus)
            {
                return enumStatus switch
                {
                    Models.StockStatus.OutOfStock => RedBrush,   // TÜKENDİ
                    Models.StockStatus.Critical => RedBrush,     // KRİTİK
                    Models.StockStatus.Low => OrangeBrush,       // DÜŞÜK
                    Models.StockStatus.Normal => GreenBrush,     // YETERLİ
                    _ => GrayBrush
                };
            }

            // Handle string values (backward compatibility)
            if (value is string stockStatus)
            {
                return stockStatus.ToLower() switch
                {
                    "tükendi" or "outofstock" => RedBrush,
                    "kritik" or "critical" => RedBrush,
                    "düşük" or "low" => OrangeBrush,
                    "normal" or "yeterli" or "sufficient" => GreenBrush,
                    _ => GrayBrush
                };
            }

            return GrayBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException("ConvertBack is not supported for one-way bindings.");
    }

    /// <summary>
    /// EMR-06: Stok durumu enum/string → okunabilir Türkçe etiket
    /// </summary>
    public class StockStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.StockStatus enumStatus)
            {
                return enumStatus switch
                {
                    Models.StockStatus.OutOfStock => "TÜKENDİ",
                    Models.StockStatus.Critical => "KRİTİK",
                    Models.StockStatus.Low => "DÜŞÜK",
                    Models.StockStatus.Normal => "YETERLİ",
                    _ => "?"
                };
            }

            return value?.ToString() ?? "?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// EMR-06: Depo doluluk oranı → renk (ProgressBar Foreground için)
    /// %0-50 yeşil (#388E3C), %50-80 turuncu (#F57C00), %80+ kırmızı (#D32F2F)
    /// </summary>
    public class OccupancyRateToColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush RedBrush = new(Color.FromRgb(0xD3, 0x2F, 0x2F));
        private static readonly SolidColorBrush OrangeBrush = new(Color.FromRgb(0xF5, 0x7C, 0x00));
        private static readonly SolidColorBrush GreenBrush = new(Color.FromRgb(0x38, 0x8E, 0x3C));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var rate = System.Convert.ToDouble(value ?? 0);
            if (rate >= 80) return RedBrush;
            if (rate >= 50) return OrangeBrush;
            return GreenBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}