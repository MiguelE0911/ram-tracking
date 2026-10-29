# Ram Tracking

Aplicación de escritorio para **consultar, monitorear y documentar** el estado de la memoria RAM en Windows. Forma parte del laboratorio de *Organización y Arquitectura de Computadoras* y está pensada para uso académico y diagnóstico básico.

> El monitoreo del uso de RAM **no demuestra** que la memoria esté libre de fallos físicos. Para eso se requiere una prueba específica de memoria, que la aplicación explica cómo ejecutar.

---

## Características

- **Consulta** de memoria instalada, utilizable por Windows, en uso, disponible y porcentaje de utilización.
- **Módulos de memoria** detectados vía SMBIOS/WMI: banco, capacidad, fabricante, número de parte, tipo (DDR3/DDR4/DDR5), velocidad en MT/s, número de serie y factor de forma.
- **Detalle por slot**: al seleccionar un módulo se muestra su posición física, la capacidad máxima del banco (SMBIOS), la capacidad estimada por slot, el tipo soportado, los voltajes y la corrección de errores del banco.
- **Top 10 de programas** con mayor consumo de RAM en tiempo real, con barras relativas al proceso más consumidor.
- **Monitoreo en tiempo real** cada 2 segundos, con gráfica de las últimas 60 muestras y estadísticas de mínimo, máximo y promedio.
- **Semáforo de utilización** según los rangos de la práctica:
  - `< 70 %` → verde
  - `70 % – 84.9 %` → amarillo
  - `≥ 85 %` → rojo
- **Diagnóstico** con explicación de la diferencia entre consumo elevado y fallos físicos, instrucciones para `mdsched.exe` y detección automática del resultado en el Visor de eventos.
- **Reportes** exportables en **TXT** o **CSV** con identificación del equipo, fecha/hora, mediciones, módulos, resumen del monitoreo, estado de la prueba y observaciones.
- **Tema oscuro** con **menú hamburguesa lateral comprimible** para navegar entre Monitor, Módulos, Diagnóstico, Reporte y Acerca de.
- **Animaciones fluidas**: expansión/contracción del menú, transición entre secciones y realimentación visual en botones, semáforo y gráfica.
- **Datos ausentes explícitos**: cualquier dato no expuesto por WMI/SMBIOS se muestra como **«No disponible»** en lugar de quedar vacío o convertirse en `0`.

---

## Requisitos

| Requisito | Detalle |
|-----------|---------|
| Sistema operativo | Windows 10 u 11 (WPF no funciona en Linux/macOS) |
| SDK | [.NET 10 SDK](https://dotnet.microsoft.com/download) |
| Arquitectura | x64 recomendado |
| Permisos | Un usuario estándar puede consultar WMI; para leer el resultado del diagnóstico desde el Visor de eventos puede requerirse ejecutar como administrador |
| IDE opcional | Visual Studio con soporte para .NET 10, JetBrains Rider o VS Code |

Verifica la instalación del SDK:

```powershell
dotnet --version
```

---

## Inicio rápido

### 1. Clonar el repositorio

```powershell
git clone https://github.com/MiguelE0911/ram-tracking.git
cd ram-tracking
```

### 2. Restaurar dependencias y ejecutar

```powershell
dotnet restore
dotnet run --project RamTracking.csproj
```

También puedes abrir `RamTracking.slnx` en un IDE compatible y ejecutar el proyecto con `F5`.

### 3. Compilar en modo Release

```powershell
dotnet build RamTracking.csproj -c Release
```

El ejecutable queda en:

```text
bin\Release\net10.0-windows\RamTracking.exe
```

### 4. Publicar un ejecutable autónomo (opcional)

Genera un único `.exe` que no requiere tener .NET instalado en el equipo destino:

```powershell
dotnet publish RamTracking.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Salida aproximada:

```text
bin\Release\net10.0-windows\win-x64\publish\RamTracking.exe
```

---

## Cómo usar la aplicación

### Navegación con el menú hamburguesa

El botón **☰** de la esquina superior izquierda expande o contrae el menú lateral:

- **Expandido (210 px):** muestra iconos y etiquetas de cada sección.
- **Contraído (62 px):** muestra solo los iconos; cada uno conserva su tooltip.
- La sección activa se resalta con un indicador verde y el contenido entra con una animación de desvanecimiento y desplazamiento.

Ya no existen pestañas: toda la navegación se realiza desde este menú.

### Monitor

1. Pulsa **Iniciar monitoreo** en la barra inferior.
2. La gráfica se actualiza cada 2 segundos y conserva las últimas 60 muestras.
3. Observa el porcentaje, el color del semáforo y las tarjetas de mínimo, promedio y máximo.
4. Revisa el **Top 10 de programas** con mayor consumo de RAM.
5. Pulsa **Detener monitoreo** cuando termines.

El monitoreo no bloquea la ventana: puedes navegar entre secciones o generar un reporte mientras está activo.

### Módulos

Muestra una fila por cada módulo de memoria detectado. El tipo de memoria se obtiene de `SMBIOSMemoryType` (campo fiable en equipos modernos) y, si no está expuesto, de `MemoryType`; por eso la tabla y el panel de detalle muestran la misma generación (por ejemplo, **DDR4**). Si el fabricante, el número de parte, la velocidad u otro dato no están expuestos por el firmware, se muestran como **«No disponible»**; nunca se inventan valores ni se sustituyen por cero.

Al hacer clic en una fila se abre el **panel de detalles del slot** con:

- Posición física (`DeviceLocator`).
- Capacidad máxima total del banco según SMBIOS y capacidad estimada por slot.
- Cantidad de slots ocupados del banco.
- Tipo de RAM soportado y detalle del tipo.
- Voltajes mínimo, máximo y configurado.
- Corrección de errores y uso del banco.

### Diagnóstico

Incluye las instrucciones para ejecutar el **Diagnóstico de memoria de Windows**:

1. Presiona `Win + R`, escribe `mdsched.exe` y presiona Enter.
2. Elige **«Reiniciar ahora y comprobar si hay problemas»**.
3. El equipo se reiniciará y ejecutará la prueba.
4. Al volver a iniciar sesión, el resultado aparece en el **Visor de eventos** → **Registros de Windows** → **Sistema** → origen `MemoryDiagnostics-Results`.

La aplicación **no reinicia el equipo automáticamente**. Puedes:

- Registrar el estado de la prueba manualmente en el selector (`No realizada`, `Pendiente`, `Realizada - Pasa`, `Realizada - Error`, `Realizada - Sin resultado`).
- Usar **Detectar resultado automáticamente** para leer el evento más reciente (Event ID `1101`).
- Escribir observaciones libres, que se incluirán en el reporte.
- Copiar las instrucciones al portapapeles.

### Reporte

Puedes generar el reporte desde la sección **Reporte** o con el botón **Generar reporte** de la barra inferior. Se abre un diálogo para elegir ubicación y formato (`.txt` o `.csv`).

El reporte incluye:

- Nombre del equipo y fecha/hora de generación.
- Memoria instalada, utilizable, en uso, disponible y porcentaje de utilización.
- Detalle de cada módulo detectado.
- Mínimo, máximo y promedio de las muestras del monitoreo.
- Estado de la prueba de memoria.
- Observaciones del usuario.

---

## Arquitectura

El proyecto sigue el patrón **MVVM** con inyección de dependencias y servicios desacoplados.

```text
ram-tracking/
├── App.xaml / App.xaml.cs          # Arranque, tema y contenedor DI
├── MainWindow.xaml / .cs           # Ventana principal, menú lateral y animaciones
├── RamTracking.csproj
├── RamTracking.slnx
├── Controls/
│   └── RamEyeLogo.xaml / .cs       # Logo vectorial de la aplicación
├── ViewModels/
│   ├── MainViewModel.cs            # Estado, sección activa, comandos y gráfica
│   └── DesignTimeViewModel.cs      # Datos de diseño para el diseñador XAML
├── Models/
│   ├── SystemInfo.cs
│   ├── MemoryInfo.cs
│   ├── MemoryModule.cs
│   ├── MemorySlotDetail.cs
│   ├── MonitorSample.cs
│   ├── ProcessMemoryUsage.cs
│   └── ReportData.cs
├── Services/
│   ├── IMemoryService.cs / MemoryService.cs
│   ├── IModuleService.cs / ModuleService.cs
│   ├── IMonitorService.cs / MonitorService.cs
│   └── IReportService.cs / ReportService.cs
├── Converters/
│   ├── BytesToGiBConverter.cs
│   ├── PercentConverters.cs
│   └── CustomConverters.cs         # Fallbacks «No disponible», secciones y menú
├── Helpers/
│   └── UnitHelper.cs
├── Assets/
│   └── logo.svg
└── IMPLEMENTACION.md               # Decisiones técnicas originales
```

### Flujo de datos

1. `App.xaml.cs` construye un `IHost` y registra servicios y `MainViewModel` como singleton/transient.
2. `MainViewModel` carga la información inicial en paralelo: sistema, memoria y módulos.
3. La navegación no usa `TabControl`: `SelectedSectionIndex` determina qué panel de sección es visible e `IsMenuExpanded` controla el ancho del menú (62/210 px). `MainWindow.xaml.cs` anima la apertura del menú y la transición entre secciones.
4. `MonitorService` usa un `DispatcherTimer` de 2 segundos, consulta `MemoryService` y emite `SampleReady`, `StatsUpdated`, `MemoryInfoReady` y `ProcessesReady`.
5. El ViewModel actualiza las tarjetas, la gráfica (LiveCharts), el Top 10 y las estadísticas en el hilo de UI.
6. `ReportService` serializa un `ReportData` a TXT o CSV.

### Servicios principales

| Servicio | Responsabilidad | Fuente de datos |
|----------|-----------------|-----------------|
| `MemoryService` | Memoria total, visible, libre, en uso y porcentaje; datos del equipo y CPU | WMI `Win32_OperatingSystem`, `Win32_ComputerSystem`, `Win32_Processor` |
| `ModuleService` | Módulos físicos instalados | WMI `Win32_PhysicalMemory` (SMBIOS) |
| `MonitorService` | Muestreo periódico, buffer circular de 60 muestras y estadísticas | `MemoryService` |
| `ReportService` | Exportación a TXT/CSV con cultura `es-ES` | `ReportData` |

### Tecnologías

- **.NET 10 + WPF** (C#)
- **CommunityToolkit.Mvvm** para `ObservableObject`, propiedades y comandos
- **LiveChartsCore.SkiaSharpView.WPF** para la gráfica
- **Microsoft.Extensions.Hosting / DependencyInjection** para el arranque y la composición
- **System.Management (WMI)** para la consulta de hardware
- **LibreHardwareMonitorLib** incluido como dependencia para futuras ampliaciones del sensado

---

## Decisiones y criterios de datos

- **Unidades:** toda la aplicación usa **GiB** (`1 GiB = 1024³ bytes`). No se mezcla con GB decimales.
- **Velocidad DDR:** se expresa en **MT/s**, no en MHz. WMI reporta la frecuencia base y el código la multiplica por 2 para obtener la velocidad efectiva de DDR; si el fabricante ya reporta MT/s, este cálculo podría duplicar el valor.
- **Tipo de memoria:** se prioriza `SMBIOSMemoryType` porque `MemoryType` suele devolver `0` en equipos modernos. La tabla y el panel de detalle usan la misma fuente, de modo que ambos muestran la misma generación (DDR4, DDR5, etc.).
- **Datos ausentes:** si WMI no expone un dato, la UI muestra **«No disponible»**. No se sustituye por `0`. Esto aplica a tarjetas de memoria, estadísticas, gráfica vacía, Top 10, tabla de módulos, detalle por slot y reportes.
- **Navegación:** se eliminó el `TabControl`; las cinco secciones son paneles independientes y solo se muestra el activo. Toda la navegación se realiza desde el menú hamburguesa.
- **Errores tolerados:** las consultas WMI y la lectura de eventos capturan excepciones para que la interfaz siga operativa.
- **Monitoreo vs. diagnóstico:** el porcentaje de uso describe carga de trabajo, no salud física de la RAM. Esta distinción se muestra explícitamente en la sección de diagnóstico y en el reporte.

---

## Limitaciones conocidas

- Solo funciona en **Windows** por dependencia de WPF y WMI.
- La disponibilidad de datos de módulos depende del firmware/SMBIOS del equipo; en algunas máquinas virtuales o equipos OEM la información puede faltar. Si `SMBIOSMemoryType` y `MemoryType` no están expuestos, el tipo se muestra como «No disponible» en la tabla y en el detalle.
- El panel de detalle usa `Win32_PhysicalMemoryArray`; cuando no es posible asociar un módulo con su banco, se toma el primer banco como respaldo.
- La detección automática del resultado de `mdsched.exe` depende de que exista el evento `MemoryDiagnostics-Results` en el registro `System` y de los permisos del usuario.
- La aplicación no incluye todavía una suite de pruebas automatizadas.
- El cálculo de MT/s a partir de la frecuencia base de WMI puede no ser exacto en todos los equipos.
- La lectura de sensores de terceros (LibreHardwareMonitor) no está integrada en la lógica actual.

---

## Solución de problemas

| Problema | Qué revisar |
|----------|-------------|
| `dotnet` no se reconoce | Instala el SDK de .NET 10 y reinicia la terminal; comprueba con `dotnet --version` |
| Error al compilar la solución | Ejecuta `dotnet build RamTracking.csproj`; el proyecto debe compilarse desde la raíz del repositorio |
| La app no abre | Confirma que estás en Windows 10/11 y que el runtime/SDK es compatible con `net10.0-windows` |
| Módulos muestran «No disponible» | Es una limitación de WMI/SMBIOS; prueba en otro equipo o actualiza el firmware |
| No se detecta el resultado de la prueba de memoria | Ejecuta la app como administrador y verifica el evento `1101` en el Visor de eventos |
| La velocidad en MT/s parece duplicada | WMI puede reportar la velocidad efectiva en algunos equipos; verifica con la ficha del módulo |
| Los valores difieren del Administrador de tareas | Son fuentes y momentos de medición distintos; se espera una diferencia pequeña, no idéntica |

---

## Contexto académico

- **Asignatura:** Organización y Arquitectura de Computadoras
- **Actividad:** Laboratorio — desarrollo de una app para el diagnóstico de memoria RAM
- **Modalidad:** Individual
- **Autor:** Miguel Aparicio
- **Año:** 2026

La documentación de decisiones técnicas original está en [`IMPLEMENTACION.md`](IMPLEMENTACION.md).

---

## Uso

Proyecto con fines académicos y educativos. Si deseas reutilizarlo, conserva la atribución al autor y revisa las licencias de las dependencias de terceros.
