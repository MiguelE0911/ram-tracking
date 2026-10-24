# Ram Tracking — Contexto de Implementación (Ruta B: .NET 8 + WPF + LibreHardwareMonitor)

## Decisiones técnicas

| Área | Decisión | Justificación |
|------|----------|---------------|
| **Framework** | .NET 8 WPF (C#) | Nativo Windows, ejecutable ligero, acceso directo a WMI/SMBIOS |
| **Hardware lib** | `LibreHardwareMonitor` (NuGet) | Expone módulos RAM: fabricante, parte, capacidad, tipo DDR, velocidad MT/s |
| **RAM del SO** | `System.Management` (WMI `Win32_OperatingSystem`, `Win32_ComputerSystem`) | Total físico, disponible, en uso sin dependencias extra |
| **Módulos SMBIOS** | `LibreHardwareMonitor.Hardware.Ram` + fallback WMI `Win32_PhysicalMemory` | Detección robusta; si falla → "No disponible" |
| **Unidades** | **GiB** (1 GiB = 1024³ bytes) en toda la app; etiquetas muestran "GiB" | Consistencia, evita ambigüedad GB vs GiB |
| **Velocidad DDR** | **MT/s** (no MHz) | Especificación correcta para DDR (double data rate) |
| **Monitoreo** | `DispatcherTimer` (UI thread) cada 2 s, buffer circular 60 muestras | No bloquea UI, simple en WPF |
| **Gráfica** | `LiveChartsCore.SkiaSharpView.WPF` | Performante, MVVM-friendly, MIT |
| **Semáforo** | <70% Verde, 70-85% Amarillo, ≥85% Rojo | Según spec |
| **Reporte** | TXT y CSV (usuario elige) via `Microsoft.Win32.SaveFileDialog` | Requisitos: equipo, fecha, mediciones, módulos, stats, diagnóstico, observaciones |
| **Diagnóstico** | Texto estático + instrucciones `mdsched.exe` + campo observación manual | No reinicio automático |
| **Patrón** | MVVM (CommunityToolkit.Mvvm) | Separación limpia, testable |
| **Empaquetado** | `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true` | Un solo .exe ~15-20 MB |

---

## Estructura del proyecto

```
RamTracking/
├── RamTracking.csproj
├── App.xaml / App.xaml.cs
├── MainWindow.xaml / MainWindow.xaml.cs
├── ViewModels/
│   ├── MainViewModel.cs
│   └── DesignTimeViewModel.cs (para preview en designer)
├── Models/
│   ├── SystemInfo.cs
│   ├── MemoryInfo.cs
│   ├── MemoryModule.cs
│   ├── MonitorSample.cs
│   └── ReportData.cs
├── Services/
│   ├── IMemoryService.cs / MemoryService.cs
│   ├── IModuleService.cs / ModuleService.cs
│   ├── IMonitorService.cs / MonitorService.cs
│   └── IReportService.cs / ReportService.cs
├── Converters/
│   ├── BytesToGiBConverter.cs
│   ├── PercentToColorConverter.cs
│   └── BoolToStatusTextConverter.cs
├── Helpers/
│   └── UnitHelper.cs (GiB, MT/s formatting)
├── Assets/
│   └── logo.png
└── Tests/
    └── RamTracking.Tests/
```

---

## Modelos clave

```csharp
// Models/MemoryInfo.cs
public record MemoryInfo(
    double TotalGiB,        // física instalada
    double UsableGiB,       // reconocida por Windows
    double UsedGiB,
    double AvailableGiB,
    double UsagePercent
);

// Models/MemoryModule.cs
public record MemoryModule(
    string BankLabel,       // "Bank 0", "Bank 1"...
    double CapacityGiB,
    string Manufacturer,    // "Samsung", "Kingston"...
    string PartNumber,      // "KHX3200C16..."
    string MemoryType,      // "DDR4", "DDR5"...
    int ConfiguredSpeedMTs  // 3200, 4800, etc.
);

// Models/MonitorSample.cs
public record MonitorSample(DateTime Timestamp, double UsagePercent);

// Models/ReportData.cs (lo que se exporta)
public record ReportData(
    string ComputerName,
    DateTime GeneratedAt,
    MemoryInfo Memory,
    IReadOnlyList<MemoryModule> Modules,
    (double Min, double Max, double Avg) MonitorStats,
    string MemoryTestStatus,      // "Realizada", "No realizada", "Pendiente"
    string Observations
);
```

---

## Servicios (interfaces + implementación)

### `IMemoryService` → `MemoryService`
- `Task<MemoryInfo> GetMemoryInfoAsync()` — usa `Win32_OperatingSystem` (TotalVisibleMemorySize, FreePhysicalMemory) + `Win32_ComputerSystem` (TotalPhysicalMemory)
- Conversión: KB → GiB (`/ 1024 / 1024`)

### `IModuleService` → `ModuleService`
- `Task<IReadOnlyList<MemoryModule>> GetModulesAsync()`
- Primero intenta `LibreHardwareMonitor` (más detalle: MT/s real, timings)
- Fallback: `Win32_PhysicalMemory` (Capacity, Manufacturer, PartNumber, Speed, MemoryType → mapear a DDRx)
- Si no hay datos → lista vacía (UI muestra "No disponible")

### `IMonitorService` → `MonitorService`
- `event Action<MonitorSample> SampleReady;`
- `Start()` / `Stop()`
- Interno: `DispatcherTimer` 2s → llama `MemoryService.GetMemoryInfoAsync()` → emite sample
- Buffer circular `MonitorSample[60]` + stats min/max/avg

### `IReportService` → `ReportService`
- `Task ExportTxtAsync(ReportData, string path)`
- `Task ExportCsvAsync(ReportData, string path)`
- Formato exacto según tabla del enunciado

---

## ViewModel principal (`MainViewModel`)

Props (INOTIFY):
- `SystemInfo SystemInfo { get; }`
- `MemoryInfo CurrentMemory { get; }`
- `IReadOnlyList<MemoryModule> Modules { get; }`
- `ObservableCollection<MonitorSample> Samples { get; }` (para gráfica)
- `double MinUsage, MaxUsage, AvgUsage`
- `bool IsMonitoring`
- `string StatusText` / `Brush StatusColor` (semáforo)
- `string DiagnosticText` (explicación + instrucciones mdsched)
- `string ManualObservation` (campo editable)

Comandos:
- `StartMonitoringCommand`
- `StopMonitoringCommand`
- `GenerateReportCommand` (abre SaveFileDialog .txt/.csv)
- `CopyDiagnosticCommand` (opcional)

Lifecycle:
- `OnLoaded()` → carga inicial (SystemInfo, Memory, Modules)
- `OnUnloaded()` → `StopMonitoring()`

---

## UI (MainWindow.xaml) — Layout sugerido

```
<Grid>
  <Grid.RowDefinitions>
    Auto   // Header: Logo + Nombre + Equipo
    Auto   // Tarjetas: Instalada | Usable | En uso | Disponible
    Auto   // Barra utilización + % + semáforo color
    *      // Gráfica (LiveCharts) + Tabla módulos (GridView)
    Auto   // Área diagnóstico (TextBlock + TextBox observaciones)
    Auto   // Botones: Iniciar/Detener | Generar Reporte | Diagnóstico
  </Grid.RowDefinitions>
</Grid>
```

- DataContext = `MainViewModel`
- Converters para bytes→GiB, %→Color, bool→Texto
- Tabla módulos: columnas Bank | Capacidad | Fabricante | Parte | Tipo | MT/s
- Gráfica: eje X = tiempo (últimos 60 samples), eje Y = 0-100%, línea + área, colores semáforo como umbrales

---

## Pasos de implementación (orden)

1. **Crear solution + proyecto WPF** (`dotnet new wpf -n RamTracking -f net8.0`)
2. **Agregar paquetes NuGet**:
   - `CommunityToolkit.Mvvm`
   - `LibreHardwareMonitor`
   - `LiveChartsCore.SkiaSharpView.WPF`
3. **Models** (records inmutables)
4. **Services** (interfaces + impl, inyectados en VM)
5. **Converters + Helpers**
6. **ViewModel** (commands, timer, properties)
7. **MainWindow.xaml** (binding, styles, LiveCharts)
8. **App.xaml** (DI simple: `Services.AddSingleton<...>()`)
9. **Tests unitarios** (MemoryService, ModuleService, ReportService con mocks)
10. **Build & publish** single-file exe
11. **Validación**: comparar con Administrador de tareas, capturas, reporte de prueba

---

## Criterios de "No disponible"

- Si WMI devuelve null/0 → propiedad = null en record → Converter muestra "No disponible"
- Nunca sustituir por 0
- Tabla de módulos: si lista vacía → fila única "No se detectaron módulos (No disponible)"

---

## Validaciones finales (entregables)

- [ ] Ejecutable `.exe` funcional en Win10/11
- [ ] Captura ventana principal
- [ ] Captura comparativa vs Task Manager (valores ±5%)
- [ ] Captura gráfica en vivo (≥60s)
- [ ] Reporte .txt y .csv generados
- [ ] Evidencia prueba `mdsched.exe` (o justificación)
- [ ] Conclusión escrita (limitaciones: monitoreo ≠ diagnóstico físico)

---

## Notas de desarrollo

- **Velocidad MT/s**: `LibreHardwareMonitor` reporta `Memory.Speed` en MHz real (p.ej. 1600 MHz DDR4-3200 → 3200 MT/s). Multiplicar ×2 si la lib da frecuencia base.
- **MemoryType mapping**: `Win32_PhysicalMemory.MemoryType` (24=DDR3, 26=DDR4, 34=DDR5) → string legible.
- **Thread safety**: `MemoryService` llamadas en background (`Task.Run`), VM actualiza en UI thread via `Dispatcher`.
- **Accesibilidad**: colores semáforo + texto (no solo color), contraste AA.
- **Localización**: español fijo (requisito del lab), cultura `es-ES` para formato de fecha/números.

---

## Próximos comandos a ejecutar

```bash
cd C:\Users\bunni\ram-tracking
dotnet new sln -n RamTracking
dotnet new wpf -n RamTracking -f net8.0
dotnet sln add RamTracking/RamTracking.csproj
cd RamTracking
dotnet add package CommunityToolkit.Mvvm
dotnet add package LibreHardwareMonitor
dotnet add package LiveChartsCore.SkiaSharpView.WPF
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Hosting
# (opcional) dotnet new xunit -n RamTracking.Tests -o Tests/RamTracking.Tests
# dotnet sln add Tests/RamTracking.Tests/RamTracking.Tests.csproj
# cd Tests/RamTracking.Tests && dotnet add reference ../../RamTracking/RamTracking.csproj
```

---

*Este documento sirve como única fuente de verdad para la implementación. Cualquier duda de diseño se resuelve aquí antes de codear.*