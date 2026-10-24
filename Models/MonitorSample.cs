namespace RamTracking.Models;

/// <summary>
/// Una muestra de monitoreo de memoria
/// </summary>
public record MonitorSample(
    DateTime Timestamp,
    double UsagePercent
);