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
            "SELECT Tag, BankLabel, Capacity, Manufacturer, PartNumber, Speed, SMBIOSMemoryType, MemoryType, SerialNumber, FormFactor FROM Win32_PhysicalMemory");
        
        int index = 0;
        foreach (ManagementObject obj in searcher.Get())
        {
            string bankLabel = obj["BankLabel"]?.ToString()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(bankLabel)) bankLabel = $"Slot {index}";
            double capacityBytes = Convert.ToDouble(obj["Capacity"] ?? 0);
            double capacityGib = capacityBytes / (1024.0 * 1024.0 * 1024.0);
            
            string manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "No disponible";
            string partNumber = obj["PartNumber"]?.ToString()?.Trim() ?? "No disponible";
            int speedMHz = Convert.ToInt32(obj["Speed"] ?? 0);
            // SMBIOSMemoryType es el campo fiable en equipos modernos; MemoryType suele venir en 0.
            int smbiosTypeCode = Convert.ToInt32(obj["SMBIOSMemoryType"] ?? 0);
            int legacyTypeCode = Convert.ToInt32(obj["MemoryType"] ?? 0);
            string serial = obj["SerialNumber"]?.ToString()?.Trim() ?? "No disponible";
            int formFactorCode = Convert.ToInt32(obj["FormFactor"] ?? 0);
            // Nota: el identificador de Win32_PhysicalMemory es "Tag", no "DeviceID"
            string wmiTag = obj["Tag"]?.ToString()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(manufacturer)) manufacturer = "No disponible";
            if (string.IsNullOrWhiteSpace(partNumber)) partNumber = "No disponible";
            if (string.IsNullOrWhiteSpace(serial)) serial = "No disponible";

            string memoryType = smbiosTypeCode > 0
                ? MapMemoryType(smbiosTypeCode)
                : MapMemoryType(legacyTypeCode);
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
                    if (supportedType == "No disponible" && module.MemoryType != "No disponible")
                        supportedType = module.MemoryType;

                    // --- Bancos físicos de memoria (arreglos) ---
                    var arrays = new List<(string Tag, double MaxGiB, string Ecc, string Use, int MemoryDevices)>();
                    using (var arraySearcher = new ManagementObjectSearcher(
                        "SELECT Tag, MaxCapacityEx, MemoryDevices, MemoryErrorCorrection, Use FROM Win32_PhysicalMemoryArray"))
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
                                MapArrayUse(Convert.ToInt32(obj["Use"] ?? 0)),
                                Convert.ToInt32(obj["MemoryDevices"] ?? 0)));   // Nuevo: número de dispositivos por array
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

                    // Slots totales según SMBIOS (MemoryDevices) vs slots ocupados (módulos instalados)
                    int totalSlots = array.MemoryDevices > 0 ? array.MemoryDevices : allDeviceIds.Count;
                    int slotsInArray = totalSlots;

                    double perSlotGiB = array.MaxGiB > 0 && totalSlots > 0
                        ? Math.Round(array.MaxGiB / totalSlots, 2)
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
        // Tabla CIM para TypeDetail (Win32_PhysicalMemory SMBIOS)
        // 1 Other, 2 Unknown, 4 Fast-paged, 8 Static column,
        // 16 Pseudo-static, 32 RAMBus, 64 Synchronous,
        // 128 CMOS, 256 EDO, 512 Window DRAM,
        // 1024 Cache DRAM, 2048 Non-volatile,
        // 4096 Registered, 8192 Unbuffered, 16384 LRDIMM
        if (bits == 0) return "No disponible";

        var flags = new List<string>();
        if ((bits & 1) != 0) flags.Add("Other");
        if ((bits & 2) != 0) flags.Add("Unknown");
        if ((bits & 4) != 0) flags.Add("Fast-paged");
        if ((bits & 8) != 0) flags.Add("Static column");
        if ((bits & 16) != 0) flags.Add("Pseudo-static");
        if ((bits & 32) != 0) flags.Add("RAMBus");
        if ((bits & 64) != 0) flags.Add("Synchronous");
        if ((bits & 128) != 0) flags.Add("CMOS");
        if ((bits & 256) != 0) flags.Add("EDO");
        if ((bits & 512) != 0) flags.Add("Window DRAM");
        if ((bits & 1024) != 0) flags.Add("Cache DRAM");
        if ((bits & 2048) != 0) flags.Add("Non-volatile");
        if ((bits & 4096) != 0) flags.Add("Registered");
        if ((bits & 8192) != 0) flags.Add("Unbuffered");
        if ((bits & 16384) != 0) flags.Add("LRDIMM");

        return flags.Count > 0 ? string.Join(", ", flags) : "No disponible";
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
            16 => "DDR",
            17 => "DDR2",
            18 => "DDR2 FB-DIMM",
            19 => "DDR3",
            20 => "DDR4",
            21 => "DDR5",
            22 => "LPDDR",
            23 => "LPDDR2",
            24 => "LPDDR3",
            27 => "HBM",
            28 => "HBM2",
            29 => "HBM3",
            _ => "No disponible"
        };
    }

    private static string MapFormFactor(int formFactorCode)
    {
        return formFactorCode switch
        {
            8 => "DIMM",
            9 => "TSOP",
            10 => "RIMM",
            11 => "SRIMM",
            12 => "SODIMM",
            13 => "SRIMM",
            _ => "No disponible"
        };
    }
}