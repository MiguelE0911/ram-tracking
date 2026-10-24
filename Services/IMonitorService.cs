namespace RamTracking.Services;

using RamTracking.Models;

public interface IMonitorService
{
    event Action<MonitorSample>? SampleReady;
    event Action<double, double, double>? StatsUpdated; // min, max, avg
    
    bool IsMonitoring { get; }
    
    void Start();
    void Stop();
    void ClearSamples();
}