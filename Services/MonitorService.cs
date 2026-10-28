namespace RamTracking.Services;

using System.Collections;
using System.Diagnostics;
using System.Windows.Threading;
using RamTracking.Models;

/// <summary>
/// Servicio de actualización en tiempo real cada 2 segundos.
/// Tarjetas y Top de programas se actualizan siempre; muestras y estadísticas
/// de la gráfica solo durante el monitoreo activo. Mantienen buffer circular de 60 muestras.
/// </summary>
public class MonitorService : IMonitorService
{
    private readonly IMemoryService _memoryService;
    private DispatcherTimer? _timer;
    private readonly CircularBuffer<MonitorSample> _samples;
    private bool _isMonitoring;

    // Excluir procesos del sistema que no aportan al diagnóstico
    private static readonly HashSet<string> ExcludedProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Idle",
        "Memory Compression"
    };

    public event Action<MonitorSample>? SampleReady;
    public event Action<double, double, double>? StatsUpdated;
    public event Action<MemoryInfo>? MemoryInfoReady;
    public event Action<IReadOnlyList<ProcessMemoryUsage>>? ProcessesReady;

    public bool IsMonitoring => _isMonitoring;

    public MonitorService(IMemoryService memoryService)
    {
        _memoryService = memoryService;
        _samples = new CircularBuffer<MonitorSample>(60);

        // Timer siempre activo: tarjetas y Top de programas se actualizan
        // independientemente de que el monitoreo esté activo o no
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _timer.Tick += async (_, _) => await CollectSampleAsync();
        _timer.Start();

        // Primera carga inmediata
        _ = CollectSampleAsync();
    }

    public void Start()
    {
        if (_isMonitoring) return;

        _isMonitoring = true;

        // Primera muestra inmediata del monitoreo
        _ = CollectSampleAsync();
    }

    public void Stop()
    {
        if (!_isMonitoring) return;

        _isMonitoring = false;
    }

    public void ClearSamples()
    {
        _samples.Clear();
        StatsUpdated?.Invoke(0, 0, 0);
    }

    private async Task CollectSampleAsync()
    {
        try
        {
            var memoryInfo = await _memoryService.GetMemoryInfoAsync();
            bool monitoring = _isMonitoring;

            // Muestras y estadísticas solo durante el monitoreo activo
            if (monitoring)
            {
                var sample = new MonitorSample(DateTime.Now, memoryInfo.UsagePercent);
                _samples.Add(sample);
                SampleReady?.Invoke(sample);

                if (_samples.Count > 0)
                {
                    var values = _samples.Select(s => s.UsagePercent).ToArray();
                    double min = values.Min();
                    double max = values.Max();
                    double avg = values.Average();
                    StatsUpdated?.Invoke(Math.Round(min, 1), Math.Round(max, 1), Math.Round(avg, 1));
                }
            }

            // Memoria actual: siempre (tarjetas y barra en tiempo real)
            MemoryInfoReady?.Invoke(memoryInfo);

            // Consumo de RAM por programa (en segundo plano para no bloquear el timer)
            var processUsage = await Task.Run(CollectProcessUsage);
            ProcessesReady?.Invoke(processUsage);
        }
        catch
        {
            // Silenciar errores para no bloquear el timer
        }
    }

    /// <summary>
    /// Recolecta la memoria de trabajo (WorkingSet) de todos los procesos,
    /// agrupando todas las instancias por nombre de proceso
    /// </summary>
    private static List<ProcessMemoryUsage> CollectProcessUsage()
    {
        var byName = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (ExcludedProcesses.Contains(process.ProcessName))
                    continue;

                long workingSet = process.WorkingSet64;
                if (workingSet > 0)
                {
                    double mib = workingSet / (1024.0 * 1024.0);
                    byName[process.ProcessName] = byName.GetValueOrDefault(process.ProcessName) + mib;
                }
            }
            catch
            {
                // Algunos procesos del sistema deniegan el acceso; se ignoran
            }
            finally
            {
                process.Dispose();
            }
        }

        return byName
            .Where(kv => kv.Value > 1) // Descartar procesos con consumo despreciable (< 1 MiB)
            .Select(kv => ProcessMemoryUsage.Raw(kv.Key, Math.Round(kv.Value, 1)))
            .OrderByDescending(p => p.UsedMiB)
            .ToList();
    }
}

/// <summary>
/// Buffer circular genérico de capacidad fija
/// </summary>
internal class CircularBuffer<T> : IEnumerable<T>
{
    private readonly T[] _buffer;
    private int _head = 0;
    private int _count = 0;

    public int Capacity { get; }
    public int Count => _count;

    public CircularBuffer(int capacity)
    {
        Capacity = capacity;
        _buffer = new T[capacity];
    }

    public void Add(T item)
    {
        _buffer[_head] = item;
        _head = (_head + 1) % Capacity;
        if (_count < Capacity) _count++;
    }

    public void Clear()
    {
        _head = 0;
        _count = 0;
        Array.Clear(_buffer, 0, _buffer.Length);
    }

    public IEnumerable<T> ToEnumerable()
    {
        if (_count == 0) return Enumerable.Empty<T>();
        
        if (_count < Capacity)
        {
            return _buffer.Take(_count);
        }
        
        // Buffer lleno: devolver desde head (más antiguo) hasta head-1
        return _buffer.Skip(_head).Concat(_buffer.Take(_head));
    }

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= _count) throw new ArgumentOutOfRangeException(nameof(index));
            int actualIndex = (_head - _count + index + Capacity) % Capacity;
            return _buffer[actualIndex];
        }
    }

    public IEnumerator<T> GetEnumerator() => ToEnumerable().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}