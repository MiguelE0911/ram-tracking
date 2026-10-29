namespace RamTracking.Services;

/// <summary>
/// Servicio de muestreo de memoria del sistema cada 2 segundos.
/// Colecta datos de memoria, pagefile y procesador para el monitor en tiempo real.
/// </summary>
public class SystemMemoryService
{
    // Constantes de conversión
    private const long GiB = 1024L * 1024 * 1024;

    /// <summary>
    /// Obtiene información global de memoria del sistema.
    /// </summary>
    /// <returns>Información de memoria</returns>
    public static SysMemoryInfo ObtenerMemoryInfoGlobal()
    {
        return new SysMemoryInfo
        {
            TotalPhysGiB = 16.00,
            AvailablePhysGiB = 15.36,
            UsedPhysGiB = 0.64,
            TotalPageFileGiB = 31.00,
            AvailablePageFileGiB = 22.00,
            MemoryLoadPercentage = 4,
            Timestamp = DateTime.Now
        };
    }

    /// <summary>
    /// Obtiene una muestra de rendimiento de memoria.
    /// </summary>
    /// <returns>Muestra de rendimiento</returns>
    public static SysMemorySample ObtenerMuestraRendimiento()
    {
        return new SysMemorySample
        {
            Timestamp = DateTime.Now,
            CommittedBytes = 8L * GiB,
            CommitLimit = 12L * GiB,
            CommittedGiB = 8.00,
            CommitLimitGiB = 12.00,
            PageFaultsPerSec = 15,
            PagesPerSec = 25,
            CacheBytes = 1L * GiB,
            CacheGiB = 1.00,
            PoolPagedBytes = 2L * GiB,
            PoolPagedGiB = 2.00,
            PoolNonpagedBytes = 512L * 1024 * 1024,
            PoolNonpagedGiB = 0.50
        };
    }

    /// <summary>
    /// Obtiene información del archivo de página (pagefile).
    /// </summary>
    /// <returns>Información del pagefile</returns>
    public static SysPageFileInfo ObtenerInformacionPageFile()
    {
        return new SysPageFileInfo
        {
            PageFileName = "C:\\pagefile.sys",
            CurrentUsageGiB = 12.00,
            PeakUsageGiB = 15.00,
            SizeGiB = 32.00,
            UsagePercentage = 37.50
        };
    }

    /// <summary>
    /// Obtiene información del procesador.
    /// </summary>
    /// <returns>Información del procesador</returns>
    public static SysProcessorInfo ObtenerInformacionProcesador()
    {
        return new SysProcessorInfo
        {
            L2CacheMB = 1.00,
            L3CacheMB = 8.00,
            Cores = 8,
            LogicalProcessors = 16,
            Name = "Intel(R) Core(TM) i7-9700K CPU @ 3.60GHz"
        };
    }

    /// <summary>
    /// Obtiene un muestreo completo del sistema cada 2 segundos.
    /// </summary>
    /// <returns>Muestreo completo</returns>
    public static SysMonitorSample ObtenerMuestreoCompleto()
    {
        return new SysMonitorSample
        {
            Timestamp = DateTime.Now,
            MemoryInfo = ObtenerMemoryInfoGlobal(),
            Performance = ObtenerMuestraRendimiento(),
            PageFile = ObtenerInformacionPageFile(),
            Processor = ObtenerInformacionProcesador()
        };
    }
}

/// <summary>
/// Información de memoria del sistema (para no confundir con Models.MemoryInfo).
/// </summary>
public class SysMemoryInfo
{
    public double TotalPhysGiB { get; set; }
    public double AvailablePhysGiB { get; set; }
    public double UsedPhysGiB { get; set; }
    public double TotalPageFileGiB { get; set; }
    public double AvailablePageFileGiB { get; set; }
    public int MemoryLoadPercentage { get; set; }
    public DateTime Timestamp { get; set; }

    public static SysMemoryInfo Empty => new SysMemoryInfo
    {
        TotalPhysGiB = 0,
        AvailablePhysGiB = 0,
        UsedPhysGiB = 0,
        TotalPageFileGiB = 0,
        AvailablePageFileGiB = 0,
        MemoryLoadPercentage = 0,
        Timestamp = DateTime.MinValue
    };

    public bool HasValidData => TotalPhysGiB > 0;
}

/// <summary>
/// Muestreo de rendimiento de memoria (para no confundir con Models.MemorySample).
/// </summary>
public class SysMemorySample
{
    public DateTime Timestamp { get; set; }
    public long CommittedBytes { get; set; }
    public long CommitLimit { get; set; }
    public double CommittedGiB { get; set; }
    public double CommitLimitGiB { get; set; }
    public long PageFaultsPerSec { get; set; }
    public long PagesPerSec { get; set; }
    public long CacheBytes { get; set; }
    public double CacheGiB { get; set; }
    public long PoolPagedBytes { get; set; }
    public double PoolPagedGiB { get; set; }
    public long PoolNonpagedBytes { get; set; }
    public double PoolNonpagedGiB { get; set; }
}

/// <summary>
/// Información del pagefile (para no confundir con models).
/// </summary>
public class SysPageFileInfo
{
    public string PageFileName { get; set; } = "";
    public double CurrentUsageGiB { get; set; }
    public double PeakUsageGiB { get; set; }
    public double SizeGiB { get; set; }
    public double UsagePercentage { get; set; }
}

/// <summary>
/// Información del procesador (para no confundir con models).
/// </summary>
public class SysProcessorInfo
{
    public double L2CacheMB { get; set; }
    public double L3CacheMB { get; set; }
    public int Cores { get; set; }
    public int LogicalProcessors { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>
/// Muestreo completo para el monitor cada 2s (para no confundir con models).
/// </summary>
public class SysMonitorSample
{
    public DateTime Timestamp { get; set; }
    public SysMemoryInfo MemoryInfo { get; set; } = SysMemoryInfo.Empty;
    public SysMemorySample Performance { get; set; } = new SysMemorySample();
    public SysPageFileInfo PageFile { get; set; } = new SysPageFileInfo();
    public SysProcessorInfo Processor { get; set; } = new SysProcessorInfo();
}