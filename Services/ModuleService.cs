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
            "SELECT Tag, BankLabel, Capacity, Manufacturer, PartNumber, Speed, MemoryType, SerialNumber, FormFactor FROM Win32_PhysicalMemory");
        
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
            // Nota: el identificador de Win32_PhysicalMemory es "Tag", no "DeviceID"
            string wmiTag = obj["Tag"]?.ToString()?.Trim() ?? "";

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
                    formFactor,
                    wmiTag
                ));
            }
            
            index++;
        }

        return modules;
    }

    public async Task<MemorySlotDetail?> GetSlotDetailAsync(MemoryModule module)
    {
        return await Task.Run(() =>
        {
            try
            {
                var (physical, allDeviceIds) = ScanPhysicalMemory(module);
                if (physical == null)
                    return null;

                using (physical)
                {
                    // --- Detalles del módulo físico ---
                    // Nota: el identificador de Win32_PhysicalMemory es "Tag", no "DeviceID"
                    string wmiTag = physical["Tag"]?.ToString() ?? "";
                    string deviceLocator = physical["DeviceLocator"]?.ToString()?.Trim() ?? module.BankLabel;
                    int typeDetailBits = Convert.ToInt32(physical["TypeDetail"] ?? 0);
                    int smbiosTypeCode = Convert.ToInt32(physical["SMBIOSMemoryType"] ?? 0);
                    double minV = MilliVoltsToVolts(physical["MinVoltage"]);
                    double maxV = MilliVoltsToVolts(physical["MaxVoltage"]);
                    double confV = MilliVoltsToVolts(physical["ConfiguredVoltage"]);
                    string supportedType = smbiosTypeCode > 0 ? MapMemoryType(smbiosTypeCode) : module.MemoryType;

                    // --- Bancos físicos de memoria (arreglos) ---
                    var arrays = new List<(string Tag, double MaxGiB, string Ecc, string Use)>();
                    using (var arraySearcher = new ManagementObjectSearcher(
                        "SELECT * FROM Win32_PhysicalMemoryArray"))
                    {
                        foreach (ManagementObject obj in arraySearcher.Get())
                        {
                            double maxKb = Convert.ToDouble(obj["MaxCapacityEx"] ?? 0);
                            if (maxKb <= 0)
                                maxKb = Convert.ToDouble(obj["MaxCapacity"] ?? 0);

                            arrays.Add(new(
                                obj["Tag"]?.ToString() ?? "",
                                Math.Round(maxKb / (1024.0 * 1024.0), 2),   // KB -> GiB
                                MapErrorCorrection(Convert.ToInt32(obj["MemoryErrorCorrection"] ?? 0)),
                                MapArrayUse(Convert.ToInt32(obj["Use"] ?? 0))));
                        }
                    }

                    // Mapear el módulo a su banco (1 a 1): asociación WMI o banco único
                    string arrayTag = "";
                    if (arrays.Count == 1)
                    {
                        arrayTag = arrays[0].Tag;
                    }
                    else if (arrays.Count > 1)
                    {
                        var mapping = GetMemoryToArrayMap();
                        mapping.TryGetValue(wmiTag, out arrayTag!);
                        if (string.IsNullOrEmpty(arrayTag))
                            arrayTag = arrays[0].Tag;
                    }

                    var array = arrays.FirstOrDefault(a => a.Tag == arrayTag);

                    // Slots ocupados del mismo banco (para estimar el máximo por slot)
                    int slotsInArray = allDeviceIds.Count;
                    if (arrays.Count > 1)
                    {
                        var mapping = GetMemoryToArrayMap();
                        slotsInArray = mapping.Values.Count(v => v.Equals(arrayTag, StringComparison.OrdinalIgnoreCase));
                        if (slotsInArray == 0) slotsInArray = allDeviceIds.Count;
                    }

                    double perSlotGiB = array.MaxGiB > 0 && slotsInArray > 0
                        ? Math.Round(array.MaxGiB / slotsInArray, 2)
                        : 0;

                    return new MemorySlotDetail(
                        Module: module,
                        DeviceLocator: deviceLocator,
                        ArrayDevice: array.Tag,
                        ArrayMaxCapacityGiB: array.MaxGiB,
                        SlotsInArray: slotsInArray,
                        PerSlotCapacityGiB: perSlotGiB,
                        ArrayErrorCorrection: array.Ecc,
                        ArrayUse: array.Use,
                        SupportedType: supportedType,
                        TypeDetail: TranslateTypeDetail(typeDetailBits),
                        MinVoltage: minV,
                        MaxVoltage: maxV,
                        ConfiguredVoltage: confV
                    );
                }
            }
            catch
            {
                return null;
            }
        });
    }

    /// <summary>
    /// Enumera Win32_PhysicalMemory y localiza el módulo seleccionado.
    /// Devuelve también la lista de DeviceIDs de todos los módulos instalados.
    /// </summary>
    private static (ManagementObject? Physical, List<string> AllDeviceIds) ScanPhysicalMemory(MemoryModule module)
    {
        ManagementObject? match = null;
        var deviceIds = new List<string>();

        using var searcher = new ManagementObjectSearcher(
            "SELECT Tag, DeviceLocator, BankLabel, Capacity, TypeDetail, SMBIOSMemoryType, MinVoltage, MaxVoltage, ConfiguredVoltage FROM Win32_PhysicalMemory");

        foreach (ManagementObject obj in searcher.Get())
        {
            string id = obj["Tag"]?.ToString() ?? "";
            deviceIds.Add(id);

            // Coincidencia preferente por Tag; alternativa por banco + capacidad
            if (match == null)
            {
                if (!string.IsNullOrEmpty(module.WmiTag) && id.Equals(module.WmiTag, StringComparison.OrdinalIgnoreCase))
                    match = obj;
                else if (string.IsNullOrEmpty(module.WmiTag))
                {
                    string bank = obj["BankLabel"]?.ToString() ?? "";
                    double gib = Convert.ToDouble(obj["Capacity"] ?? 0) / (1024.0 * 1024.0 * 1024.0);
                    if (bank.Equals(module.BankLabel, StringComparison.OrdinalIgnoreCase) && Math.Abs(gib - module.CapacityGiB) < 0.05)
                        match = obj;
                }
            }
        }

        return (match, deviceIds);
    }

    /// <summary>
    /// Asociación Win32_PhysicalMemoryLocation: mapa DeviceID del módulo -> Tag del banco (arreglo)
    /// </summary>
    private static Dictionary<string, string> GetMemoryToArrayMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT GroupComponent, PartComponent FROM Win32_PhysicalMemoryLocation");
            foreach (ManagementObject obj in searcher.Get())
            {
                try
                {
                    var g = obj["GroupComponent"] as ManagementObject;
                    var p = obj["PartComponent"] as ManagementObject;
                    if (g == null || p == null) continue;

                    string gClass = g.ClassPath?.ClassName ?? "";
                    string pClass = p.ClassPath?.ClassName ?? "";

                    ManagementObject? memory = null, array = null;
                    if (gClass.Equals("Win32_PhysicalMemoryArray", StringComparison.OrdinalIgnoreCase))
                    {
                        array = g;
                        memory = p;
                    }
                    else if (pClass.Equals("Win32_PhysicalMemoryArray", StringComparison.OrdinalIgnoreCase))
                    {
                        array = p;
                        memory = g;
                    }

                    if (memory == null || array == null) continue;

                    string memoryId = memory["Tag"]?.ToString() ?? memory["DeviceID"]?.ToString() ?? "";
                    string arrayTag = array["Tag"]?.ToString() ?? "";
                    if (memoryId.Length > 0 && arrayTag.Length > 0)
                        map[memoryId] = arrayTag;
                }
                catch
                {
                    // Ignorar asociaciones no legibles
                }
            }
        }
        catch
        {
            // Si la asociación no está disponible se usa el fallback del llamador
        }

        return map;
    }

    private static double MilliVoltsToVolts(object? mv) => Convert.ToDouble(mv ?? 0) / 1000.0;

    /// <summary>
    /// Traduce la máscara de bits TypeDetail de Win32_PhysicalMemory
    /// </summary>
    private static string TranslateTypeDetail(int bits)
    {
        if (bits == 0) return "No disponible";

        var flags = new List<string>();
        if ((bits & 1) != 0) flags.Add("Reservado");
        if ((bits & 2) != 0) flags.Add("Otro");
        if ((bits & 4) != 0) flags.Add("Desconocido");
        if ((bits & 8) != 0) flags.Add("Rápida paginada (Fast-paged)");
        if ((bits & 16) != 0) flags.Add("Columna estática");
        if ((bits & 32) != 0) flags.Add("Seudoestática");
        if ((bits & 256) != 0) flags.Add("SDRAM (síncrona)");
        if ((bits & 512) != 0) flags.Add("CMOS");
        if ((bits & 1024) != 0) flags.Add("EDO");
        if ((bits & 2048) != 0) flags.Add("Window DRAM");
        if ((bits & 4096) != 0) flags.Add("Cache DRAM");
        if ((bits & 32768) != 0) flags.Add("Con buffer (buffered)");
        if ((bits & 65536) != 0) flags.Add("Sin buffer (unbuffered)");

        return flags.Count > 0 ? string.Join(", ", flags) : $"Código WMI: {bits}";
    }

    private static string MapErrorCorrection(int code) => code switch
    {
        1 => "Otro",
        2 => "Desconocido",
        3 => "Ninguna",
        4 => "Paridad",
        5 => "ECC de 1 bit",
        6 => "ECC de varios bits",
        7 => "CRC",
        8 => "Checksum",
        9 => "Otro",
        _ => "No disponible"
    };

    private static string MapArrayUse(int code) => code switch
    {
        1 => "Otro",
        2 => "Desconocido",
        3 => "Memoria del sistema",
        4 => "Memoria de vídeo",
        5 => "Memoria flash",
        6 => "RAM no volátil",
        7 => "Memoria caché",
        _ => "No disponible"
    };

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