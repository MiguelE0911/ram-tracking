namespace RamTracking.Converters;

using System.Globalization;
using System.Windows.Data;

/// <summary>
/// Convierte bytes a GiB con formato "X.XX GiB" o "No disponible"
/// </summary>
public class BytesToGiBConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double gib)
        {
            return gib > 0 ? $"{gib:F2} GiB" : "No disponible";
        }
        if (value is long bytes)
        {
            double gibValue = bytes / (1024.0 * 1024.0 * 1024.0);
            return gibValue > 0 ? $"{gibValue:F2} GiB" : "No disponible";
        }
        return "No disponible";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}