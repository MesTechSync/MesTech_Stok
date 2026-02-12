using System;
using System.Globalization;
using System.Windows.Data;

namespace MesTechStok.Desktop.Converters
{
    /// <summary>
    /// UTC DateTime'ı yerel saat dilimi formatına çevirir (kullanıcı dostu görüntüm)
    /// </summary>
    public class UtcToLocalTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime utcTime && utcTime.Kind == DateTimeKind.Utc)
            {
                var localTime = utcTime.ToLocalTime();
                var format = parameter?.ToString() ?? "G";
                return localTime.ToString(format, culture);
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("UTC to Local conversion is one-way only");
        }
    }

    /// <summary>
    /// Boolean success değerini renkli metin formatına çevirir
    /// </summary>
    public class SuccessToDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool success)
            {
                return success ? "✓ Başarılı" : "✗ Hatalı";
            }
            return "—";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// HTTP durum kodunu anlamlı metin formatına çevirir
    /// </summary>
    public class StatusCodeToDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int statusCode)
            {
                return statusCode switch
                {
                    200 => "200 OK",
                    201 => "201 Created",
                    400 => "400 Bad Request",
                    401 => "401 Unauthorized",
                    403 => "403 Forbidden",
                    404 => "404 Not Found",
                    429 => "429 Too Many Requests",
                    500 => "500 Internal Error",
                    502 => "502 Bad Gateway",
                    503 => "503 Service Unavailable",
                    _ => statusCode.ToString()
                };
            }
            return value?.ToString() ?? "—";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
