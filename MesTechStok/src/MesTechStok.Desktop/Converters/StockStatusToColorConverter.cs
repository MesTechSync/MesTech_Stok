using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MesTechStok.Desktop.Converters
{
    /// <summary>
    /// Stock Status to Color Converter - BRAVO TİMİ
    /// Stok durumunu renge dönüştürür
    /// </summary>
    public class StockStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stockStatus)
            {
                return stockStatus.ToLower() switch
                {
                    "normal" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),     // Green
                    "düşük" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),      // Orange
                    "kritik" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),     // Red
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))           // Gray
                };
            }

            return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Default Gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}