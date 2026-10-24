namespace RamTracking.Models;

/// <summary>
/// Datos completos para generar el reporte (TXT/CSV)
/// </summary>
public record ReportData(
    string ComputerName,
    DateTime GeneratedAt,
    MemoryInfo Memory,
    IReadOnlyList<MemoryModule> Modules,
    (double Min, double Max, double Avg) MonitorStats,
    string MemoryTestStatus,      // "Realizada", "No realizada", "Pendiente", "Resultado: Pasa/Error"
    string Observations
);