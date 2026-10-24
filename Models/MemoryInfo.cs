namespace RamTracking.Models;

/// <summary>
/// Información de memoria RAM en GiB (1 GiB = 1024^3 bytes)
/// </summary>
public record MemoryInfo(
    double TotalGiB,        // Memoria física instalada total
    double UsableGiB,       // Memoria utilizable por Windows
    double UsedGiB,         // Memoria en uso actual
    double AvailableGiB,    // Memoria disponible
    double UsagePercent     // Porcentaje de utilización (0-100)
)
{
    public static MemoryInfo Empty => new(0, 0, 0, 0, 0);
    
    public bool HasValidData => TotalGiB > 0;
}