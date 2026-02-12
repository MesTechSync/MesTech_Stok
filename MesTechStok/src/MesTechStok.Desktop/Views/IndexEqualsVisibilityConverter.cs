using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MesTechStok.Desktop.Views
{
    public class IndexEqualsVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values.Length < 2) return Visibility.Collapsed;
                if (values[0] is int index && values[1] is int cover)
                {
                    return index == cover ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch { }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


