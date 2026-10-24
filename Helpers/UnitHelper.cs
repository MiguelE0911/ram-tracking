namespace RamTracking.Helpers;

/// <summary>
/// Helpers para formateo de unidades consistente en toda la app
/// </summary>
public static class UnitHelper
{
    // Constantes de conversión
    public const double BytesPerKiB = 1024.0;
    public const double BytesPerMiB = 1024.0 * 1024.0;
    public const double BytesPerGiB = 1024.0 * 1024.0 * 1024.0;
    public const double KiBPerGiB = 1024.0 * 1024.0;

    /// <summary>
    /// Formatea GiB a string "X.XX GiB" o "No disponible"
    /// </summary>
    public static string FormatGiB(double gib) => gib > 0 ? $"{gib:F2} GiB" : "No disponible";

    /// <summary>
    /// Formatea velocidad MT/s a string "XXXX MT/s" o "No disponible"
    /// </summary>
    public static string FormatMTs(int mts) => mts > 0 ? $"{mts} MT/s" : "No disponible";

    /// <summary>
    /// Formatea porcentaje a string "XX.X%"
    /// </summary>
    public static string FormatPercent(double percent) => $"{percent:F1}%";

    /// <summary>
    /// Convierte bytes a GiB (double)
    /// </summary>
    public static double BytesToGiB(long bytes) => bytes / BytesPerGiB;

    /// <summary>
    /// Convierte KB a GiB (double)
    /// </summary>
    public static double KiBToGiB(double kib) => kib / KiBPerGiB;

    /// <summary>
    /// Redondea a 2 decimales para presentación
    /// </summary>
    public static double Round2(double value) => Math.Round(value, 2);

    /// <summary>
    /// Redondea a 1 decimal para porcentajes
    /// </summary>
    public static double Round1(double value) => Math.Round(value, 1);
}