namespace RamTracking.ViewModels;

using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Win32;
using RamTracking.Models;
using RamTracking.Services;
using SkiaSharp;

/// <summary>
/// ViewModel principal de la aplicación
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IMemoryService _memoryService;
    private readonly IModuleService _moduleService;
    private readonly IMonitorService _monitorService;
    private readonly IReportService _reportService;

    [ObservableProperty]
    private SystemInfo _systemInfo = new("Cargando...", "", "", "");

    [ObservableProperty]
    private MemoryInfo _currentMemory = MemoryInfo.Empty;

    [ObservableProperty]
    private ObservableCollection<MemoryModule> _modules = new();

    [ObservableProperty]
    private ObservableCollection<MonitorSample> _samples = new();

    // Top 10 de programas que más RAM están consumiendo (tiempo real)
    [ObservableProperty]
    private ObservableCollection<ProcessMemoryUsage> _topProcesses = new();

    [ObservableProperty]
    private double _minUsage;

    [ObservableProperty]
    private double _maxUsage;

    [ObservableProperty]
    private double _avgUsage;

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private string _diagnosticText = "";

    [ObservableProperty]
    private string _manualObservation = "";

    [ObservableProperty]
    private string _memoryTestStatus = "No realizada";

    // Para LiveCharts - Serie que se bindea a la gráfica
    [ObservableProperty]
    private ISeries[] _chartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _chartXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _chartYAxes = Array.Empty<Axis>();

    // Tarjetas de memoria para el header
    [ObservableProperty]
    private MemoryCard[] _memoryCards = Array.Empty<MemoryCard>();

    // Opciones para ComboBox de estado de prueba
    public string[] MemoryTestStatusOptions { get; } = new[]
    {
        "No realizada",
        "Pendiente",
        "Realizada - Pasa",
        "Realizada - Error",
        "Realizada - Sin resultado"
    };

    // Referencia interna a la serie de línea para actualizar valores
    private LineSeries<ObservablePoint>? _lineSeries;

    // Registro por programa: pico de RAM observado y último valor visto (para el reporte final)
    private readonly Dictionary<string, (double PeakMiB, double LastMiB)> _processMemory = new(StringComparer.OrdinalIgnoreCase);

    public IAsyncRelayCommand LoadInitialDataCommand { get; }
    public IRelayCommand StartMonitoringCommand { get; }
    public IRelayCommand StopMonitoringCommand { get; }
    public IRelayCommand ToggleMonitoringCommand { get; }
    public IAsyncRelayCommand GenerateReportCommand { get; }
    public IRelayCommand CopyDiagnosticCommand { get; }
    public IRelayCommand CheckMemoryTestResultCommand { get; }

    public MainViewModel(
        IMemoryService memoryService,
        IModuleService moduleService,
        IMonitorService monitorService,
        IReportService reportService)
    {
        _memoryService = memoryService;
        _moduleService = moduleService;
        _monitorService = monitorService;
        _reportService = reportService;

        // Comandos
        LoadInitialDataCommand = new AsyncRelayCommand(LoadInitialDataAsync);
        StartMonitoringCommand = new RelayCommand(StartMonitoring);
        StopMonitoringCommand = new RelayCommand(StopMonitoring);
        ToggleMonitoringCommand = new RelayCommand(ToggleMonitoring);
        GenerateReportCommand = new AsyncRelayCommand<object>(GenerateReportAsync);
        CopyDiagnosticCommand = new RelayCommand(CopyDiagnosticToClipboard);
        CheckMemoryTestResultCommand = new RelayCommand(CheckMemoryTestResult);

        // Suscribirse a eventos del monitor
        _monitorService.SampleReady += OnSampleReady;
        _monitorService.StatsUpdated += OnStatsUpdated;
        _monitorService.MemoryInfoReady += OnMemoryInfoReady;
        _monitorService.ProcessesReady += OnProcessesReady;

        // Inicializar gráfica vacía
        InitializeChart();

        // Texto de diagnóstico fijo
        DiagnosticText = @"El monitoreo informa sobre la utilización de la memoria. Para investigar posibles errores físicos se necesita una prueba específica de RAM.

Para ejecutar el Diagnóstico de memoria de Windows:
1. Presione Win+R, escriba 'mdsched.exe' y presione Enter
2. Seleccione 'Reiniciar ahora y comprobar si hay problemas (recomendado)'
3. El equipo se reiniciará y ejecutará la prueba automáticamente
4. Al volver a iniciar sesión, verá el resultado en el Visor de eventos (Event Viewer) > Registros de Windows > Sistema > Fuente: MemoryDiagnostics-Results

⚠ La prueba requiere reiniciar el equipo. Guarde su trabajo antes de ejecutarla.
⚠ No se programará un reinicio automático desde esta aplicación.";

        // Cargar datos iniciales
        _ = LoadInitialDataAsync();
    }

    private void InitializeChart()
    {
        // Serie de línea para uso de memoria
        _lineSeries = new LineSeries<ObservablePoint>
        {
            Values = new ObservableCollection<ObservablePoint>(),
            Fill = null,
            Stroke = new SolidColorPaint(SKColor.Parse("#22C55E")) { StrokeThickness = 2 },
            GeometrySize = 0,
            LineSmoothness = 0.3,
            Name = "Utilización %"
        };

        ChartSeries = new ISeries[] { _lineSeries };

        ChartXAxes = new Axis[]
        {
            new Axis
            {
                Labeler = value => DateTime.FromOADate(value).ToString("HH:mm:ss"),
                UnitWidth = TimeSpan.FromSeconds(2).TotalDays,
                MinStep = TimeSpan.FromSeconds(2).TotalDays,
                LabelsRotation = 0,
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#B0B0B0")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#4A4A4A")) { StrokeThickness = 0.5f },
                SeparatorsAtCenter = false
            }
        };

        ChartYAxes = new Axis[]
        {
            new Axis
            {
                MinLimit = 0,
                MaxLimit = 100,
                Labeler = value => $"{value:F0}%",
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#B0B0B0")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#4A4A4A")) { StrokeThickness = 0.5f }
            }
        };
    }

    private async Task LoadInitialDataAsync()
    {
        // Cargar info del sistema y memoria en paralelo
        var sysInfoTask = _memoryService.GetSystemInfoAsync();
        var memInfoTask = _memoryService.GetMemoryInfoAsync();
        var modulesTask = _moduleService.GetModulesAsync();

        SystemInfo = await sysInfoTask;
        CurrentMemory = await memInfoTask;
        var modules = await modulesTask;
        
        Modules.Clear();
        foreach (var m in modules)
            Modules.Add(m);

        // Actualizar tarjetas de memoria
        UpdateMemoryCards();
    }

    private void UpdateMemoryCards()
    {
        MemoryCards = new[]
        {
            new MemoryCard("Instalada", CurrentMemory.TotalGiB, "GiB"),
            new MemoryCard("Utilizable", CurrentMemory.UsableGiB, "GiB"),
            new MemoryCard("En uso", CurrentMemory.UsedGiB, "GiB"),
            new MemoryCard("Disponible", CurrentMemory.AvailableGiB, "GiB")
        };
    }

    partial void OnCurrentMemoryChanged(MemoryInfo value)
    {
        UpdateMemoryCards();
    }

    private void StartMonitoring()
    {
        _monitorService.ClearSamples();
        Samples.Clear();
        if (_lineSeries != null)
        {
            // Limpiar la colección existente en lugar de reemplazarla
            (_lineSeries.Values as ObservableCollection<ObservablePoint>)?.Clear();
        }
        MinUsage = MaxUsage = AvgUsage = 0;
        TopProcesses.Clear();
        _processMemory.Clear();
        
        _monitorService.Start();
        IsMonitoring = true;
    }

    private void StopMonitoring()
    {
        _monitorService.Stop();
        IsMonitoring = false;
    }

    private void ToggleMonitoring()
    {
        if (IsMonitoring)
            StopMonitoring();
        else
            StartMonitoring();
    }

    private void OnSampleReady(MonitorSample sample)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            Samples.Add(sample);
            while (Samples.Count > 60)
                Samples.RemoveAt(0);

            // Actualizar gráfica LiveCharts - mutar la colección existente
            if (_lineSeries != null)
            {
                var oaDate = sample.Timestamp.ToOADate();
                var currentValues = _lineSeries.Values as ObservableCollection<ObservablePoint>;
                
                if (currentValues == null)
                {
                    currentValues = new ObservableCollection<ObservablePoint>();
                    _lineSeries.Values = currentValues;
                }
                
                currentValues.Add(new ObservablePoint(oaDate, sample.UsagePercent));
                
                // Mantener solo últimos 60 puntos
                while (currentValues.Count > 60)
                    currentValues.RemoveAt(0);
            }
        });
    }

    private void OnStatsUpdated(double min, double max, double avg)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            MinUsage = min;
            MaxUsage = max;
            AvgUsage = avg;
        });
    }

    private void OnMemoryInfoReady(MemoryInfo memoryInfo)
    {
        // Actualiza tarjetas superiores y barra de utilización en tiempo real
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentMemory = memoryInfo;
        });
    }

    private void OnProcessesReady(IReadOnlyList<ProcessMemoryUsage> processUsage)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            // Registrar picos y último valor visto por programa solo durante el monitoreo activo:
            // así el reporte refleja únicamente la sesión monitoreada
            if (IsMonitoring)
            {
                foreach (var process in processUsage)
                {
                    var (peak, _) = _processMemory.GetValueOrDefault(process.Name);
                    _processMemory[process.Name] = (Math.Max(peak, process.UsedMiB), process.UsedMiB);
                }
            }

            // Refrescar el Top 10 en pantalla (siempre, en tiempo real)
            var top10 = processUsage
                .Where(p => p.UsedMiB > 0)
                .OrderByDescending(p => p.UsedMiB)
                .Take(10)
                .ToList();

            double maxMiB = top10.Count > 0 ? top10[0].UsedMiB : 0;
            var ranked = top10
                .Select((p, index) => p with
                {
                    Rank = index + 1,
                    PeakMiB = _processMemory.GetValueOrDefault(p.Name).PeakMiB,
                    RelativePercent = maxMiB > 0 ? Math.Round(p.UsedMiB / maxMiB * 100.0, 1) : 0
                })
                .ToList();

            TopProcesses.Clear();
            foreach (var p in ranked)
                TopProcesses.Add(p);
        });
    }

    /// <summary>
    /// Top 10 de programas que más RAM consumieron durante el monitoreo (por pico)
    /// </summary>
    private List<ProcessMemoryUsage> BuildTopProcessesForReport()
    {
        return _processMemory
            .Where(kv => kv.Value.PeakMiB > 0)
            .Select(kv => new ProcessMemoryUsage(
                Name: kv.Key,
                UsedMiB: Math.Round(kv.Value.LastMiB, 1),
                PeakMiB: Math.Round(kv.Value.PeakMiB, 1),
                Rank: 0,
                RelativePercent: 0))
            .OrderByDescending(p => p.PeakMiB)
            .Take(10)
            .Select((p, index) => p with { Rank = index + 1 })
            .ToList();
    }

    private async Task GenerateReportAsync(object? parameter)
    {
        var format = parameter?.ToString();
        var isCsv = format?.Equals("csv", StringComparison.OrdinalIgnoreCase) == true;
        
        var dialog = new SaveFileDialog
        {
            Title = "Guardar reporte de diagnóstico",
            Filter = isCsv 
                ? "Archivo CSV (*.csv)|*.csv"
                : "Archivo de texto (*.txt)|*.txt|Archivo CSV (*.csv)|*.csv",
            DefaultExt = isCsv ? "csv" : "txt",
            FileName = $"RamTracking_Report_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var reportData = new ReportData(
                    ComputerName: SystemInfo.ComputerName,
                    GeneratedAt: DateTime.Now,
                    Memory: CurrentMemory,
                    Modules: Modules.ToList(),
                    MonitorStats: (MinUsage, MaxUsage, AvgUsage),
                    MemoryTestStatus: MemoryTestStatus,
                    Observations: ManualObservation,
                    TopProcesses: BuildTopProcessesForReport()
                );

                if (isCsv || dialog.FilterIndex == 2) // CSV
                    await _reportService.ExportCsvAsync(reportData, dialog.FileName);
                else // TXT
                    await _reportService.ExportTxtAsync(reportData, dialog.FileName);

                System.Windows.MessageBox.Show(
                    $"Reporte guardado exitosamente en:\n{dialog.FileName}",
                    "Reporte generado",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error al guardar el reporte:\n{ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private void CopyDiagnosticToClipboard()
    {
        try
        {
            System.Windows.Clipboard.SetText(DiagnosticText);
            System.Windows.MessageBox.Show(
                "Instrucciones de diagnóstico copiadas al portapapeles",
                "Copiado",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch
        {
            // Ignorar errores de portapapeles
        }
    }

    private void CheckMemoryTestResult()
    {
        try
        {
            // Buscar el último evento de MemoryDiagnostics-Results (EventID 1101)
            string query = "*[System[(EventID=1101) and (Provider[@Name='Microsoft-Windows-MemoryDiagnostics-Results'] or Provider[@Name='MemoryDiagnostics-Results'])]]";
            var eventLogQuery = new EventLogQuery("System", PathType.LogName, query)
            {
                ReverseDirection = true
            };

            using var reader = new EventLogReader(eventLogQuery);
            EventRecord? record = reader.ReadEvent();

            if (record == null)
            {
                System.Windows.MessageBox.Show(
                    "No se encontró ningún resultado del diagnóstico de memoria en el Visor de Eventos.\n\n" +
                    "Asegúrate de haber ejecutado mdsched.exe, completado la prueba y reiniciado el equipo.",
                    "Sin resultado",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }

            string message = record.FormatDescription() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(message))
            {
                try
                {
                    message = record.ToXml();
                }
                catch
                {
                    // Ignorar
                }
            }

            string lower = message.ToLowerInvariant();

            if (lower.Contains("no errors") || lower.Contains("no error") || lower.Contains("sin errores"))
            {
                MemoryTestStatus = "Realizada - Pasa";
            }
            else if (lower.Contains("error") || lower.Contains("errors") || lower.Contains("error(s)"))
            {
                MemoryTestStatus = "Realizada - Error";
            }
            else
            {
                MemoryTestStatus = "Realizada - Sin resultado";
            }

            // Añadir resultado a las observaciones si no está ya presente
            string timestamp = record.TimeCreated?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string entry = $"[Diagnóstico automático {timestamp}]\n{message}\n\n";

            if (string.IsNullOrWhiteSpace(ManualObservation))
            {
                ManualObservation = entry.TrimEnd();
            }
            else if (!ManualObservation.Contains("Diagnóstico automático"))
            {
                ManualObservation = entry + ManualObservation;
            }

            System.Windows.MessageBox.Show(
                $"Resultado detectado automáticamente.\n\nEstado: {MemoryTestStatus}\nFecha: {timestamp}",
                "Resultado del diagnóstico",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Error al leer el resultado del diagnóstico:\n{ex.Message}\n\nEs posible que se requieran permisos o que el registro no esté disponible.",
                "Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    public void SetMemoryTestStatus(string status) => MemoryTestStatus = status;
}

/// <summary>
/// Modelo simple para las tarjetas de memoria en el header
/// </summary>
public record MemoryCard(string Label, double Value, string Unit);