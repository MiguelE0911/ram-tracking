namespace RamTracking.Converters;

using System.Globalization;
using System.Windows.Data;

/// <summary>
/// Convierte porcentaje (0-100) y ancho disponible a ancho de barra de progreso
/// </summary>
public class PercentToWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 
            && values[0] is double percent 
            && values[1] is double availableWidth
            && availableWidth > 0)
        {
            double clampedPercent = Math.Clamp(percent, 0, 100);
            return availableWidth * clampedPercent / 100.0;
        }
        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Convierte count (int) a Visibility: 0 = Visible, >0 = Collapsed
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
        return System.Windows.Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}