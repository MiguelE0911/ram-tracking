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

/// <summary>
/// Devuelve "No disponible" cuando el texto es null, vacío o no resuelto
/// </summary>
public class NotAvailableTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || value == System.Windows.DependencyProperty.UnsetValue)
            return "No disponible";

        string text = value.ToString()?.Trim() ?? "";
        return string.IsNullOrWhiteSpace(text) || text.Equals("Desconocido", StringComparison.OrdinalIgnoreCase)
            ? "No disponible"
            : text;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Convierte un conteo (int) a texto; 0 o negativo = "No disponible"
/// </summary>
public class OptionalCountConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count && count > 0)
            return parameter is string suffix ? $"{count} {suffix}" : count.ToString();

        return "No disponible";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Convierte una capacidad en GiB a texto; 0 = "No disponible"
/// </summary>
public class OptionalGiBConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double gib && gib > 0 ? $"{gib:F2} GiB" : "No disponible";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Convierte un voltaje (V, double) a texto; 0 = "No disponible"
/// </summary>
public class OptionalVoltageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double volts && volts > 0 ? $"{volts:F2} V" : "No disponible";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Muestra el contenido solo cuando el índice de sección coincide con el parámetro.
/// Sustituye por completo al TabControl: cada sección vive en su propio panel.
/// </summary>
public class SectionVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index && int.TryParse(parameter?.ToString(), out int section))
            return index == section ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        return System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Ancho del menú lateral: expandido = 210 px, contraído = 62 px.
/// </summary>
public class MenuWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? 210.0 : 62.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Cantidad de slots ocupados del banco; 0 = "No disponible".
/// </summary>
public class OptionalSlotsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count && count > 0)
            return count == 1
                ? "1 slot del banco con módulo instalado"
                : $"{count} slots del banco con módulo instalado";

        return "No disponible";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}