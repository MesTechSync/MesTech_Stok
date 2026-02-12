using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MesTechStok.Desktop.Views
{
    public class MarginToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is decimal d)
                {
                    return d >= 0 ? new SolidColorBrush(Color.FromRgb(41, 180, 111)) : new SolidColorBrush(Color.FromRgb(244, 67, 54));
                }
            }
            catch { }
            return new SolidColorBrush(Color.FromRgb(33, 33, 33));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}


