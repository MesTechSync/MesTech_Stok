using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MesTechStok.Desktop.Converters
{
    public class GreaterThanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            if (!double.TryParse(System.Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return false;
            if (!double.TryParse(System.Convert.ToString(parameter, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var p)) return false;
            return v > p;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BetweenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            if (!double.TryParse(System.Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return false;
            var parts = System.Convert.ToString(parameter, CultureInfo.InvariantCulture)?.Split(',');
            if (parts == null || parts.Length != 2) return false;
            if (!double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var a)) return false;
            if (!double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var b)) return false;
            var min = Math.Min(a, b);
            var max = Math.Max(a, b);
            return v >= min && v <= max;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? "✔" : "✖";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? Brushes.LightGreen : Brushes.OrangeRed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}


