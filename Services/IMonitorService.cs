namespace RamTracking.Services;

using RamTracking.Models;

public interface IMonitorService
{
    event Action<MonitorSample>? SampleReady;
    event Action<double, double, double>? StatsUpdated; // min, max, avg
    event Action<MemoryInfo>? MemoryInfoReady;          // memoria actual (para tarjetas y barra en tiempo real)
    event Action<IReadOnlyList<ProcessMemoryUsage>>? ProcessesReady; // consumo de RAM por programa (agregado)

    bool IsMonitoring { get; }

    void Start();
    void Stop();
    void ClearSamples();
}
