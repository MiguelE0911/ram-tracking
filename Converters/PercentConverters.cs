namespace RamTracking.Converters;

using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

/// <summary>
/// Convierte porcentaje de uso a color del semáforo
/// Verde < 70%, Amarillo 70-85%, Rojo >= 85%
/// </summary>
public class PercentToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percent)
        {
            return percent switch
            {
                < 70 => Brushes.LimeGreen,
                < 85 => Brushes.Goldenrod,
                _ => Brushes.IndianRed
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Convierte porcentaje a texto de estado del semáforo
/// </summary>
public class PercentToStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percent)
        {
            return percent switch
            {
                < 70 => "Utilización dentro del rango definido para la práctica.",
                < 85 => "Utilización elevada; revisar las aplicaciones abiertas.",
                _ => "Utilización muy elevada; revisar la carga del equipo."
            };
        }
        return "Estado desconocido";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Convierte bool (IsMonitoring) a texto de botón
/// </summary>
public class BoolToMonitorButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "Detener monitoreo" : "Iniciar monitoreo";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}