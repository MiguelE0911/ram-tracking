namespace RamTracking.Models;

/// <summary>
/// Consumo de RAM de un programa (proceso), con todas sus instancias agrupadas por nombre.
/// </summary>
public record ProcessMemoryUsage(
    string Name,            // Nombre del proceso (agrupa todas las instancias)
    double UsedMiB,         // Memoria de trabajo en uso en este momento (MiB)
    double PeakMiB,         // Memoria máxima observada durante el monitoreo (MiB)
    int Rank,               // Posición en el ranking (1 = mayor consumo)
    double RelativePercent  // % relativo al programa más consumidor (para la barra, 0-100)
)
{
    public static ProcessMemoryUsage Raw(string name, double usedMiB) => new(name, usedMiB, 0, 0, 0);
}
