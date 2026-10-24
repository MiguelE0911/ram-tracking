namespace RamTracking.Services;

using System.Management;
using RamTracking.Models;

/// <summary>
/// Servicio para obtener información de módulos de memoria RAM
/// Usa WMI (Win32_PhysicalMemory) como fuente principal
/// </summary>
public class ModuleService : IModuleService
{
    public async Task<IReadOnlyList<MemoryModule>> GetModulesAsync()
    {
        return await Task.Run(() =>
        {
            var modules = new List<MemoryModule>();

            try
            {
                var wmiModules = GetModulesFromWmi();
                if (wmiModules.Count > 0)
                {
                    return wmiModules;
                }
            }
            catch
            {
                // Ignorar errores de WMI
            }

            return modules; // Lista vacía -> UI mostrará "No disponible"
        });
    }

    private List<MemoryModule> GetModulesFromWmi()
    {
        var modules = new List<MemoryModule>();

        using var searcher = new ManagementObjectSearcher(
            "SELECT BankLabel, Capacity, Manufacturer, PartNumber, Speed, MemoryType, SerialNumber, FormFactor FROM Win32_PhysicalMemory");
        
        int index = 0;
        foreach (ManagementObject obj in searcher.Get())
        {
            string bankLabel = obj["BankLabel"]?.ToString() ?? $"Slot {index}";
            double capacityBytes = Convert.ToDouble(obj["Capacity"] ?? 0);
            double capacityGib = capacityBytes / (1024.0 * 1024.0 * 1024.0);
            
            string manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "No disponible";
            string partNumber = obj["PartNumber"]?.ToString()?.Trim() ?? "No disponible";
            int speedMHz = Convert.ToInt32(obj["Speed"] ?? 0);
            int memoryTypeCode = Convert.ToInt32(obj["MemoryType"] ?? 0);
            string serial = obj["SerialNumber"]?.ToString()?.Trim() ?? "";
            int formFactorCode = Convert.ToInt32(obj["FormFactor"] ?? 0);

            string memoryType = MapMemoryType(memoryTypeCode);
            string formFactor = MapFormFactor(formFactorCode);
            
            // Speed en WMI está en MHz (frecuencia base), DDR = x2 para MT/s
            int speedMTs = speedMHz > 0 ? speedMHz * 2 : 0;

            if (capacityGib > 0)
            {
                modules.Add(new MemoryModule(
                    bankLabel, 
                    Math.Round(capacityGib, 2), 
                    manufacturer, 
                    partNumber, 
                    memoryType, 
                    speedMTs,
                    serial,
                    formFactor
                ));
            }
            
            index++;
        }

        return modules;
    }

    private static string MapMemoryType(int typeCode)
    {
        return typeCode switch
        {
            20 => "DDR",
            21 => "DDR2",
            22 => "DDR2 FB-DIMM",
            24 => "DDR3",
            26 => "DDR4",
            34 => "DDR5",
            _ => "Desconocido"
        };
    }

    private static string MapFormFactor(int formFactorCode)
    {
        return formFactorCode switch
        {
            8 => "DIMM",
            12 => "SODIMM",
            13 => "SODIMM",
            _ => "Desconocido"
        };
    }
}