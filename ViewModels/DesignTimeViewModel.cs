namespace RamTracking.ViewModels;

using System.Collections.ObjectModel;
using RamTracking.Models;
using RamTracking.Services;

/// <summary>
/// ViewModel de datos de diseño para preview en Visual Studio / Blend
/// </summary>
public class DesignTimeViewModel : MainViewModel
{
    public DesignTimeViewModel() : base(
        new DesignMemoryService(),
        new DesignModuleService(),
        new DesignMonitorService(),
        new DesignReportService())
    {
        // Datos de ejemplo para diseño
        SystemInfo = new SystemInfo(
            "DESKTOP-ABC123",
            "Microsoft Windows 11 Pro 10.0.22631",
            "Intel(R) Core(TM) i7-12700K",
            "x64"
        );

        CurrentMemory = new MemoryInfo(
            TotalGiB: 32.00,
            UsableGiB: 31.75,
            UsedGiB: 18.42,
            AvailableGiB: 13.33,
            UsagePercent: 58.0
        );

        Modules.Clear();
        Modules.Add(new MemoryModule("Bank 0", 16.00, "Samsung", "M378A2G43AB2-CWE", "DDR4", 3200, "12345678", "DIMM"));
        Modules.Add(new MemoryModule("Bank 1", 16.00, "Samsung", "M378A2G43AB2-CWE", "DDR4", 3200, "12345679", "DIMM"));

        Samples.Clear();
        var rand = new Random(42);
        double baseVal = 58.0;
        for (int i = 0; i < 60; i++)
        {
            baseVal += rand.NextDouble() * 4 - 2; // variación ±2%
            baseVal = Math.Clamp(baseVal, 40, 80);
            Samples.Add(new MonitorSample(DateTime.Now.AddSeconds(-120 + i * 2), Math.Round(baseVal, 1)));
        }

        MinUsage = 42.3;
        MaxUsage = 78.9;
        AvgUsage = 58.1;
        IsMonitoring = true;
        ManualObservation = "Equipo funcionando correctamente. Uso de RAM dentro de parámetros normales para carga de trabajo típica (navegador + IDE).";
        MemoryTestStatus = "Pendiente";
    }

    // Servicios dummy para diseño
    private class DesignMemoryService : IMemoryService
    {
        public Task<MemoryInfo> GetMemoryInfoAsync() => Task.FromResult(MemoryInfo.Empty);
        public Task<SystemInfo> GetSystemInfoAsync() => Task.FromResult(new SystemInfo("", "", "", ""));
    }

    private class DesignModuleService : IModuleService
    {
        public Task<IReadOnlyList<MemoryModule>> GetModulesAsync() => Task.FromResult<IReadOnlyList<MemoryModule>>(Array.Empty<MemoryModule>());
    }

    private class DesignMonitorService : IMonitorService
    {
        public event Action<MonitorSample>? SampleReady;
        public event Action<double, double, double>? StatsUpdated;
        public bool IsMonitoring => false;
        public void Start() { }
        public void Stop() { }
        public void ClearSamples() { }
    }

    private class DesignReportService : IReportService
    {
        public Task ExportTxtAsync(ReportData data, string filePath) => Task.CompletedTask;
        public Task ExportCsvAsync(ReportData data, string filePath) => Task.CompletedTask;
    }
}