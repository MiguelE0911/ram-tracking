namespace RamTracking.Models;

/// <summary>
/// Información de un módulo de memoria RAM individual
/// Velocidad en MT/s (no MHz) para DDR
/// </summary>
public record MemoryModule(
    string BankLabel,           // Ej: "Bank 0", "Bank 1", "DIMM_A0"
    double CapacityGiB,         // Capacidad del módulo en GiB
    string Manufacturer,        // Fabricante: "Samsung", "Kingston", "Corsair", etc.
    string PartNumber,          // Número de parte: "KHX3200C16D4/16GX"
    string MemoryType,          // Tipo: "DDR3", "DDR4", "DDR5", "Desconocido"
    int ConfiguredSpeedMTs,     // Velocidad configurada en MT/s (ej: 3200, 4800, 5600)
    string SerialNumber = "",   // Número de serie (opcional)
    string FormFactor = "",     // Factor de forma: "DIMM", "SODIMM", etc.
    string WmiTag = ""          // Tag de WMI: "Physical Memory 0"
)
{
    public static MemoryModule Unknown(string bankLabel) => new(
        bankLabel, 0, "No disponible", "No disponible", "No disponible", 0);

    public bool HasValidData => CapacityGiB > 0;

    // Propiedades de presentación: garantizan que la UI nunca muestre celdas vacías
    public string BankDisplay => Fallback(BankLabel);
    public string CapacityDisplay => CapacityGiB > 0 ? $"{CapacityGiB:F2} GiB" : "No disponible";
    public string ManufacturerDisplay => Fallback(Manufacturer);
    public string PartNumberDisplay => Fallback(PartNumber);
    public string MemoryTypeDisplay => Fallback(MemoryType);
    public string SpeedDisplay => ConfiguredSpeedMTs > 0 ? $"{ConfiguredSpeedMTs} MT/s" : "No disponible";
    public string SerialDisplay => Fallback(SerialNumber);
    public string FormFactorDisplay => Fallback(FormFactor);

    private static string Fallback(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "No disponible" : value.Trim();
}