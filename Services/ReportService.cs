namespace RamTracking.Services;

using System.IO;
using System.Globalization;
using System.Text;
using RamTracking.Models;

/// <summary>
/// Servicio para exportar reportes en TXT y CSV
/// </summary>
public class ReportService : IReportService
{
    private static readonly CultureInfo EsEs = new("es-ES");

    public async Task ExportTxtAsync(ReportData data, string filePath)
    {
        var lines = new List<string>
        {
            "========================================",
            "       REPORTE DE DIAGNÓSTICO RAM       ",
            "========================================",
            "",
            $"Equipo:              {data.ComputerName}",
            $"Fecha y hora:        {data.GeneratedAt:G}",
            "",
            "--- MEMORIA ---",
            $"Memoria instalada:   {FormatGiB(data.Memory.TotalGiB)}",
            $"Memoria utilizable:  {FormatGiB(data.Memory.UsableGiB)}",
            $"Memoria en uso:      {FormatGiB(data.Memory.UsedGiB)}",
            $"Memoria disponible:  {FormatGiB(data.Memory.AvailableGiB)}",
            $"Utilización:         {data.Memory.UsagePercent:F1}%",
            "",
            "--- MÓDULOS DE MEMORIA ---"
        };

        if (data.Modules.Count == 0)
        {
            lines.Add("No disponible");
        }
        else
        {
            foreach (var module in data.Modules)
            {
                lines.Add($"  [{module.BankLabel}]");
                lines.Add($"    Capacidad:       {FormatGiB(module.CapacityGiB)}");
                lines.Add($"    Fabricante:      {module.Manufacturer}");
                lines.Add($"    Número de parte: {module.PartNumber}");
                lines.Add($"    Tipo:            {module.MemoryType}");
                lines.Add($"    Velocidad:       {FormatSpeed(module.ConfiguredSpeedMTs)}");
                if (!string.IsNullOrEmpty(module.SerialNumber))
                    lines.Add($"    Número de serie: {module.SerialNumber}");
                if (!string.IsNullOrEmpty(module.FormFactor))
                    lines.Add($"    Factor de forma: {module.FormFactor}");
                lines.Add("");
            }
        }

        lines.AddRange(new[]
        {
            "--- RESUMEN DEL MONITOREO ---",
            $"Mínimo:              {data.MonitorStats.Min:F1}%",
            $"Máximo:              {data.MonitorStats.Max:F1}%",
            $"Promedio:            {data.MonitorStats.Avg:F1}%",
            ""
        });

        // Top 10 programas con mayor consumo de RAM durante el monitoreo
        lines.Add("--- PROGRAMAS CON MAYOR CONSUMO DE RAM (TOP 10) ---");
        if (data.TopProcesses.Count == 0)
        {
            lines.Add("(Sin datos de monitoreo)");
        }
        else
        {
            foreach (var p in data.TopProcesses)
            {
                string name = p.Name.Length > 26 ? p.Name[..26] : p.Name;
                lines.Add($"  {p.Rank,2}. {name,-26}  Pico:      {FormatMiB(p.PeakMiB)}");
                lines.Add($"      {"",-26}  Al final:  {FormatMiB(p.UsedMiB)}");
            }
        }

        lines.AddRange(new[]
        {
            "",
            "--- PRUEBA DE MEMORIA ---",
            $"Estado:              {data.MemoryTestStatus}",
            "",
            "--- OBSERVACIONES ---",
            string.IsNullOrWhiteSpace(data.Observations) ? "(Sin observaciones)" : data.Observations,
            "",
            "========================================",
            "Fin del reporte",
            "========================================"
        });

        await File.WriteAllLinesAsync(filePath, lines, Encoding.UTF8);
    }

    public async Task ExportCsvAsync(ReportData data, string filePath)
    {
        var lines = new List<string>
        {
            "Campo,Valor"
        };

        // Función helper para escapar CSV
        string Escape(string value) => value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

        void Add(string field, string value) => lines.Add($"{Escape(field)},{Escape(value)}");

        Add("Equipo", data.ComputerName);
        Add("Fecha y hora", data.GeneratedAt.ToString("G", EsEs));
        Add("Memoria instalada (GiB)", data.Memory.TotalGiB.ToString("F2", EsEs));
        Add("Memoria utilizable (GiB)", data.Memory.UsableGiB.ToString("F2", EsEs));
        Add("Memoria en uso (GiB)", data.Memory.UsedGiB.ToString("F2", EsEs));
        Add("Memoria disponible (GiB)", data.Memory.AvailableGiB.ToString("F2", EsEs));
        Add("Utilización (%)", data.Memory.UsagePercent.ToString("F1", EsEs));

        // Módulos
        if (data.Modules.Count == 0)
        {
            Add("Módulos", "No disponible");
        }
        else
        {
            for (int i = 0; i < data.Modules.Count; i++)
            {
                var m = data.Modules[i];
                Add($"Módulo {i + 1} - Bank", m.BankLabel);
                Add($"Módulo {i + 1} - Capacidad (GiB)", m.CapacityGiB.ToString("F2", EsEs));
                Add($"Módulo {i + 1} - Fabricante", m.Manufacturer);
                Add($"Módulo {i + 1} - Número de parte", m.PartNumber);
                Add($"Módulo {i + 1} - Tipo", m.MemoryType);
                Add($"Módulo {i + 1} - Velocidad (MT/s)", m.ConfiguredSpeedMTs > 0 ? m.ConfiguredSpeedMTs.ToString() : "No disponible");
                if (!string.IsNullOrEmpty(m.SerialNumber))
                    Add($"Módulo {i + 1} - Número de serie", m.SerialNumber);
                if (!string.IsNullOrEmpty(m.FormFactor))
                    Add($"Módulo {i + 1} - Factor de forma", m.FormFactor);
            }
        }

        Add("Monitoreo - Mínimo (%)", data.MonitorStats.Min.ToString("F1", EsEs));
        Add("Monitoreo - Máximo (%)", data.MonitorStats.Max.ToString("F1", EsEs));
        Add("Monitoreo - Promedio (%)", data.MonitorStats.Avg.ToString("F1", EsEs));

        // Top 10 programas con mayor consumo de RAM durante el monitoreo
        if (data.TopProcesses.Count == 0)
        {
            Add("Programas con mayor consumo de RAM", "(Sin datos de monitoreo)");
        }
        else
        {
            foreach (var p in data.TopProcesses)
            {
                Add($"Programa {p.Rank} - Nombre", p.Name);
                Add($"Programa {p.Rank} - Pico de RAM (MiB)", p.PeakMiB.ToString("F1", EsEs));
                Add($"Programa {p.Rank} - RAM al final del monitoreo (MiB)", p.UsedMiB.ToString("F1", EsEs));
            }
        }

        Add("Prueba de memoria", data.MemoryTestStatus);
        Add("Observaciones", data.Observations);

        await File.WriteAllLinesAsync(filePath, lines, Encoding.UTF8);
    }

    private static string FormatGiB(double gib) => gib > 0 ? $"{gib:F2} GiB" : "No disponible";
    private static string FormatSpeed(int mts) => mts > 0 ? $"{mts} MT/s" : "No disponible";
    private static string FormatMiB(double mib) => mib >= 1024 ? $"{mib / 1024.0:F2} GiB" : $"{mib:F1} MiB";
}