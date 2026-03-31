using System.Globalization;
using Avalonia.Data.Converters;

namespace MesTech.Avalonia.Converters;

/// <summary>G041: Value converters for OnboardingWizard step visibility.</summary>
public class EqualConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return value.ToString() == parameter.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class GreaterThanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intVal && parameter is string strParam && int.TryParse(strParam, out var threshold))
            return intVal > threshold;
        if (value is double dVal && parameter is string strP2 && double.TryParse(strP2, out var dThreshold))
            return dVal > dThreshold;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BetweenConverter : IMultiValueConverter, IValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 3) return false;
        if (values[0] is int val && values[1] is int min && values[2] is int max)
            return val >= min && val <= max;
        return false;
    }

    // Single-value binding: Converter={StaticResource BetweenConverter}, ConverterParameter=2-6
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int val || parameter is not string range) return false;
        var parts = range.Split('-');
        if (parts.Length == 2 && int.TryParse(parts[0], out var min) && int.TryParse(parts[1], out var max))
            return val >= min && val <= max;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
