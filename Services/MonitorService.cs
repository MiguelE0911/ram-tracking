namespace RamTracking.Services;

using System.Collections;
using System.Windows.Threading;
using RamTracking.Models;

/// <summary>
/// Servicio de monitoreo en tiempo real cada 2 segundos
/// Mantiene buffer circular de 60 muestras
/// </summary>
public class MonitorService : IMonitorService
{
    private readonly IMemoryService _memoryService;
    private DispatcherTimer? _timer;
    private readonly CircularBuffer<MonitorSample> _samples;
    private bool _isMonitoring;

    public event Action<MonitorSample>? SampleReady;
    public event Action<double, double, double>? StatsUpdated;

    public bool IsMonitoring => _isMonitoring;

    public MonitorService(IMemoryService memoryService)
    {
        _memoryService = memoryService;
        _samples = new CircularBuffer<MonitorSample>(60);
    }

    public void Start()
    {
        if (_isMonitoring) return;
        
        // Crear timer en el hilo UI actual (lazy initialization)
        if (_timer == null)
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _timer.Tick += async (_, _) => await CollectSampleAsync();
        }

        _isMonitoring = true;
        _timer.Start();
        
        // Primera muestra inmediata
        _ = CollectSampleAsync();
    }

    public void Stop()
    {
        if (!_isMonitoring) return;
        
        _timer?.Stop();
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
            var sample = new MonitorSample(DateTime.Now, memoryInfo.UsagePercent);
            
            _samples.Add(sample);
            SampleReady?.Invoke(sample);
            
            // Calcular estadísticas
            if (_samples.Count > 0)
            {
                var values = _samples.Select(s => s.UsagePercent).ToArray();
                double min = values.Min();
                double max = values.Max();
                double avg = values.Average();
                StatsUpdated?.Invoke(Math.Round(min, 1), Math.Round(max, 1), Math.Round(avg, 1));
            }
        }
        catch
        {
            // Silenciar errores para no bloquear el timer
        }
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