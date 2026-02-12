using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MesTechStok.Desktop.Converters
{
    /// <summary>
    /// String to Visibility Converter - BRAVO TİMİ
    /// String değerini Visibility'ye dönüştürür
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                return string.IsNullOrEmpty(strValue) ? Visibility.Collapsed : Visibility.Visible;
            }

            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}