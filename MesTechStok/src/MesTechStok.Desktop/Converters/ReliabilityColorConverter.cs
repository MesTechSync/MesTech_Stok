using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MesTechStok.Desktop.Converters;

/// <summary>
/// DEMİR KARAR güvenilirlik eşiklerini WPF rengine dönüştürür:
/// ≥90 Yeşil / 70-89 Sarı / 50-69 Turuncu / &lt;50 Kırmızı
/// </summary>
[ValueConversion(typeof(decimal), typeof(SolidColorBrush))]
public class ReliabilityColorConverter : IValueConverter
{
    // DEMİR KARAR renkleri (#10b981 / #f59e0b / #f97316 / #ef4444)
    private static readonly SolidColorBrush Green  = new(Color.FromRgb(0x10, 0xb9, 0x81));
    private static readonly SolidColorBrush Yellow = new(Color.FromRgb(0xf5, 0x9e, 0x0b));
    private static readonly SolidColorBrush Orange = new(Color.FromRgb(0xf9, 0x73, 0x16));
    private static readonly SolidColorBrush Red    = new(Color.FromRgb(0xef, 0x44, 0x44));

    // Arka plan renkleri (daha açık ton)
    private static readonly SolidColorBrush GreenBg  = new(Color.FromRgb(0xec, 0xfd, 0xf5));
    private static readonly SolidColorBrush YellowBg = new(Color.FromRgb(0xff, 0xfb, 0xeb));
    private static readonly SolidColorBrush OrangeBg = new(Color.FromRgb(0xff, 0xf7, 0xed));
    private static readonly SolidColorBrush RedBg    = new(Color.FromRgb(0xfe, 0xf2, 0xf2));

    public enum Mode { Foreground, Background }
    public Mode ConvertMode { get; set; } = Mode.Foreground;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var score = value switch
        {
            decimal d => d,
            double d  => (decimal)d,
            int i     => (decimal)i,
            string s when decimal.TryParse(s, out var p) => p,
            _ => 0m
        };

        return ConvertMode == Mode.Background
            ? score >= 90 ? GreenBg : score >= 70 ? YellowBg : score >= 50 ? OrangeBg : RedBg
            : score >= 90 ? Green   : score >= 70 ? Yellow   : score >= 50 ? Orange   : Red;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException("ConvertBack is not supported for one-way bindings.");
}

/// <summary>
/// Güvenilirlik skoru → metin (Yeşil/Sarı/Turuncu/Kırmızı + puan)
/// </summary>
public class ReliabilityLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not decimal score) return "—";
        var label = score >= 90 ? "Yeşil" : score >= 70 ? "Sarı" : score >= 50 ? "Turuncu" : "Kırmızı";
        return $"{label} {score:F0}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException("ConvertBack is not supported for one-way bindings.");
}
