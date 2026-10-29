namespace RamTracking.Services;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RamTracking.Models;

/// <summary>
/// Servicio que lee información de memoria mediante llamadas SMBIOS (System Management BIOS).
/// Usa GetSystemFirmwareTable P/Invoke para acceder a los datos raw de firmware.
/// </summary>
public class SmbiosService : ISmbiosService
{
    // P/Invoke para GetSystemFirmwareTable
    // https://docs.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getsystemfirmwareTable
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetSystemFirmwareTable(
        uint pFirmwareTableHandler,
        uint pFirmwareTableClass,
        IntPtr pFirmwareTableBuffer,
        uint pFirmwareTableBufferLength);

    private const uint FirmwareTableHandler = 0x00000000; // SMBIOS
    // "RSMB" como uint (four-character code): R=0x52, S=0x53, M=0x4D, B=0x42
    // En little-endian: 0x424D5352
    private const uint SMBIOSClass = 0x424D5352;

    // Offset para Type 16 (Physical Memory Array) y Type 17 (Memory Device)
    private const int OFFSET_TYPE_16 = 16;
    private const int OFFSET_TYPE_17 = 17;

    // Tamaños aproximados de las estructuras SMBIOS (varían según la implementación)
    private const int SMBIOS_HEADER_SIZE = 4; // Type + Length + Handle
    private const int SMBIOS_TYPE_16_SIZE = 29; // Tamaño estándar Type 16
    private const int SMBIOS_TYPE_17_SIZE = 38; // Tamaño estándar Type 17

    /// <summary>
    /// Lee una estructura SMBIOS específica por tipo e índice
    /// </summary>
    /// <param name="type">Tipo de estructura (16 = Physical Memory Array, 17 = Memory Device)</param>
    /// <param name="index">Número de instancia (0, 1, 2, ...)</param>
    /// <returns>Diccionario con los campos leídos, o null si no hay datos</returns>
    public Dictionary<string, object?>? LeerEstructuraSMBIOS(byte type, int index)
    {
        // Primero intentamos obtener el número total de estructuras
        uint bufferSize = 4096; // Tamaño inicial de buffer
        IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);

        try
        {
            // Llamada para obtener el tamaño necesario
            bool success = GetSystemFirmwareTable(
                FirmwareTableHandler,
                SMBIOSClass,
                buffer,
                bufferSize);

            if (!success)
            {
                // SMBIOS no disponible en este sistema
                return null;
            }

            // Parsear las estructuras encontradas
            // La estructura SMBIOS comienza con un header: Type (1 byte), Length (1 byte), Handle (2 bytes)
            // Luego siguen los datos variable según el tipo

            int offset = 0;

            while (offset < bufferSize - SMBIOS_HEADER_SIZE)
            {
                // Leer el tipo de la estructura actual
                byte structureType = Marshal.ReadByte(buffer, offset);

                // Si llegamos al final o el tipo es 0 (marcador de fin), salir
                if (structureType == 0)
                    break;

                // Leer la longitud
                byte structureLength = Marshal.ReadByte(buffer, offset + 1);

                // Leer el handle
                ushort handle = (ushort)Marshal.ReadInt16(buffer, offset + 2);

                // Si el tipo coincide con el solicitado
                if (structureType == type)
                {
                    // Intentar leer datos según el tipo
                    var fields = ParsearEstructuraSMBIOS(buffer, offset, structureType, structureLength);

                    if (fields != null)
                    {
                        // Añadir el índice si es necesario
                        fields["InstanceIndex"] = index;
                        return fields; // Retornar el primero encontrado
                    }
                }

                // Avanzar al siguiente estructura (offset = current + length)
                offset += structureLength;
            }

            return null;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>
    /// Intenta leer información de Type 17 (Memory Device) usando SMBIOS,
    /// y si falla, retorna null para que el llamador use WMI como fallback.
    /// </summary>
    /// <returns>MemoryModule con datos SMBIOS, o null si no hay información disponible</returns>
    public MemoryModule? LeerMemoryDeviceSMBIOS()
    {
        // Intentar leer estructura Type 17 (índice 0 = primer módulo encontrado)
        var estructura = LeerEstructuraSMBIOS(17, 0);

        if (estructura == null)
            return null;

        // Helper para obtener valor entero seguro
        int ObtenerEntero(Dictionary<string, object?> dic, string clave, int defaultValue = 0)
        {
            if (dic.TryGetValue(clave, out var val) && val != null)
                return Convert.ToInt32(val);
            return defaultValue;
        }

        // Helper para obtener string seguro
        string ObtenerString(Dictionary<string, object?> dic, string clave)
        {
            if (dic.TryGetValue(clave, out var val) && val != null)
                return val.ToString() ?? "";
            return "";
        }

        // Mapear MemoryType code a string (SMBIOS Type 17 codes)
        string memoryType = ObtenerEntero(estructura, "MemoryType") switch
        {
            20 => "DDR",
            21 => "DDR2",
            24 => "DDR3",
            26 => "DDR4",
            27 => "DDR5",
            38 => "LPDDR",
            39 => "LPDDR2",
            40 => "LPDDR3",
            41 => "HBM",
            42 => "HBM2",
            43 => "HBM3",
            _ => "No disponible"
        };

        // Mapear FormFactor code a string
        string formFactor = ObtenerEntero(estructura, "FormFactor") switch
        {
            8 => "DIMM",
            9 => "TSOP",
            10 => "RIMM",
            11 => "SRIMM",
            12 => "SODIMM",
            13 => "SRIMM",
            25 => "LRDIMM",
            _ => "No disponible"
        };

        // Obtener tamaño (campo Size en bytes)
        long sizeBytes = ObtenerEntero(estructura, "Size");

        // Si size es 0 o negativo, no hay datos útiles
        if (sizeBytes <= 0)
            return null;

        // Calcular GiB
        double sizeGiB = Math.Round(sizeBytes / (1024.0 * 1024.0 * 1024.0), 2);

        // Obtener DeviceLocator (nombre del slot)
        string deviceLocator = ObtenerString(estructura, "DeviceLocator");

        // Obtener otras propiedades
        string manufacturer = ObtenerString(estructura, "Manufacturer");
        string partNumber = ObtenerString(estructura, "PartNumber");
        string serial = ObtenerString(estructura, "SerialNumber");
        string assetTag = ObtenerString(estructura, "AssetTag");

        // Velocidad configurada (en MT/s)
        int configuredSpeed = ObtenerEntero(estructura, "ConfiguredSpeed");

        // Crear y retornar MemoryModule
        MemoryModule modulo = new MemoryModule(
            deviceLocator,              // BankLabel
            sizeGiB,                    // CapacityGiB
            string.IsNullOrEmpty(manufacturer) ? "No disponible" : manufacturer,  // Manufacturer
            string.IsNullOrEmpty(partNumber) ? "No disponible" : partNumber,      // PartNumber
            memoryType,                 // MemoryType
            configuredSpeed,            // ConfiguredSpeedMTs
            string.IsNullOrEmpty(serial) ? "No disponible" : serial,              // SerialNumber
            string.IsNullOrEmpty(formFactor) ? "No disponible" : formFactor,      // FormFactor
            deviceLocator               // WmiTag
        );

        return modulo;
    }

    /// <summary>
    /// Intenta leer información de Type 16 (Physical Memory Array) usando SMBIOS,
    /// y si falla, retorna null para que el llamador use WMI como fallback.
    /// </summary>
    /// <returns>MemorySlotDetail con datos del array, o null si no hay información disponible</returns>
    public MemorySlotDetail? LeerPhysicalMemoryArraySMBIOS()
    {
        // Intentar leer estructura Type 16 (índice 0)
        var estructura = LeerEstructuraSMBIOS(16, 0);

        if (estructura == null)
            return null;

        // Helper para obtener entero seguro
        int ObtenerEntero(Dictionary<string, object?> dic, string clave, int defaultValue = 0)
        {
            if (dic.TryGetValue(clave, out var val) && val != null)
                return Convert.ToInt32(val);
            return defaultValue;
        }

        // Helper para obtener string seguro
        string ObtenerString(Dictionary<string, object?> dic, string clave)
        {
            if (dic.TryGetValue(clave, out var val) && val != null)
                return val.ToString() ?? "";
            return "";
        }

        // Obtener capacidad máxima (MaxCapacityEx viene en KB según SMBIOS)
        double maxCapacityKB = ObtenerEntero(estructura, "MaxCapacityEx");
        double maxCapacityGiB = maxCapacityKB > 0 ? Math.Round(maxCapacityKB / (1024.0 * 1024.0), 2) : 0;

        // Obtener número total de dispositivos
        int totalDevices = ObtenerEntero(estructura, "TotalMemoryDevices");

        // Mapear Error Correction Type
        string ecc = ObtenerEntero(estructura, "MemoryErrorCorrection") switch
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

        // Mapear Array Use
        string use = ObtenerEntero(estructura, "ArrayUse") switch
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

        // Calcular capacidad por slot
        double perSlotGiB = totalDevices > 0 && maxCapacityGiB > 0
            ? Math.Round(maxCapacityGiB / totalDevices, 2)
            : 0;

        // Crear y retornar MemorySlotDetail
        MemorySlotDetail detalle = new MemorySlotDetail(
            Module: null, // Se asociará después por el llamador
            DeviceLocator: "",
            ArrayDevice: "SMBIOS",
            ArrayMaxCapacityGiB: maxCapacityGiB,
            SlotsInArray: totalDevices,
            PerSlotCapacityGiB: perSlotGiB,
            ArrayErrorCorrection: ecc,
            ArrayUse: use,
            SupportedType: "SMBIOS",
            TypeDetail: "Leído desde SMBIOS Type 16",
            MinVoltage: 0,
            MaxVoltage: 0,
            ConfiguredVoltage: 0
        );

        return detalle;
    }

    /// <summary>
    /// Parsea la estructura SMBIOS según su tipo
    /// </summary>
    private Dictionary<string, object?>? ParsearEstructuraSMBIOS(IntPtr buffer, int offset, byte structureType, byte structureLength)
    {
        var campos = new Dictionary<string, object?>();

        switch (structureType)
        {
            case 16: // Physical Memory Array
                if (structureLength >= SMBIOS_TYPE_16_SIZE)
                {
                    ushort handlingCapacity = (ushort)Marshal.ReadInt16(buffer, offset + 0);
                    ushort errorCorrectionType = (ushort)Marshal.ReadInt16(buffer, offset + 2);
                    ushort maxMemorySize = (ushort)Marshal.ReadInt16(buffer, offset + 4);
                    ushort memoryErrorConfig = (ushort)Marshal.ReadInt16(buffer, offset + 6);
                    ushort maxMemoryDevices = (ushort)Marshal.ReadInt16(buffer, offset + 10);
                    ushort totalMemoryDevices = (ushort)Marshal.ReadInt16(buffer, offset + 12);

                    campos["MaxCapacityEx"] = maxMemorySize;
                    campos["MemoryErrorCorrection"] = errorCorrectionType;
                    campos["MaxMemoryDevices"] = maxMemoryDevices;
                    campos["TotalMemoryDevices"] = totalMemoryDevices;
                    campos["ArrayUse"] = MapearArrayUse(errorCorrectionType);
                }
                break;

            case 17: // Memory Device
                if (structureLength >= SMBIOS_TYPE_17_SIZE)
                {
                    ushort arrayHandle = (ushort)Marshal.ReadInt16(buffer, offset + 0);
                    ushort errorInfoHandle = (ushort)Marshal.ReadInt16(buffer, offset + 2);
                    ushort model = (ushort)Marshal.ReadInt16(buffer, offset + 4);
                    ushort manufacturer = (ushort)Marshal.ReadInt16(buffer, offset + 6);
                    ushort serialNumber = (ushort)Marshal.ReadInt16(buffer, offset + 8);
                    ushort assetTag = (ushort)Marshal.ReadInt16(buffer, offset + 10);
                    ushort partNumber = (ushort)Marshal.ReadInt16(buffer, offset + 12);
                    ushort rank = (ushort)Marshal.ReadInt16(buffer, offset + 14);
                    ushort minVoltage = (ushort)Marshal.ReadInt16(buffer, offset + 16);
                    ushort maxVoltage = (ushort)Marshal.ReadInt16(buffer, offset + 18);
                    ushort configuredVoltage = (ushort)Marshal.ReadInt16(buffer, offset + 20);
                    ushort minSpeed = (ushort)Marshal.ReadInt16(buffer, offset + 22);
                    ushort maxSpeed = (ushort)Marshal.ReadInt16(buffer, offset + 24);
                    ushort configuredSpeed = (ushort)Marshal.ReadInt16(buffer, offset + 26);
                    ushort minCAS = (ushort)Marshal.ReadInt16(buffer, offset + 28);
                    ushort maxCAS = (ushort)Marshal.ReadInt16(buffer, offset + 30);
                    ushort memoryType = (ushort)Marshal.ReadInt16(buffer, offset + 32);
                    ushort busWidth = (ushort)Marshal.ReadInt16(buffer, offset + 34);
                    ushort formFactor = (ushort)Marshal.ReadInt16(buffer, offset + 36);
                    ushort deviceLocator = (ushort)Marshal.ReadInt16(buffer, offset + 38);
                    ushort bankLocator = (ushort)Marshal.ReadInt16(buffer, offset + 40);

                    campos["ArrayHandle"] = arrayHandle;
                    campos["ErrorInformationHandle"] = errorInfoHandle;
                    campos["Model"] = model;
                    campos["Manufacturer"] = manufacturer;
                    campos["SerialNumber"] = serialNumber;
                    campos["AssetTag"] = assetTag;
                    campos["PartNumber"] = partNumber;
                    campos["Rank"] = rank;
                    campos["MinVoltage"] = minVoltage;
                    campos["MaxVoltage"] = maxVoltage;
                    campos["ConfiguredVoltage"] = configuredVoltage;
                    campos["MinSpeed"] = minSpeed;
                    campos["MaxSpeed"] = maxSpeed;
                    campos["ConfiguredSpeed"] = configuredSpeed;
                    campos["MinCAS"] = minCAS;
                    campos["MaxCAS"] = maxCAS;
                    campos["MemoryType"] = memoryType;
                    campos["BusWidth"] = busWidth;
                    campos["FormFactor"] = formFactor;
                    campos["DeviceLocator"] = deviceLocator;
                    campos["BankLocator"] = bankLocator;
                }
                break;
        }

        return campos.Count > 0 ? campos : null;
    }

    /// <summary>
    /// Mapea código de Error Correction SMBIOS a string
    /// </summary>
    private static string MapearErrorCorrection(int code) => code switch
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

    /// <summary>
    /// Mapea código de Array Use SMBIOS a string
    /// </summary>
    private static string MapearArrayUse(int code) => code switch
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

    /// <summary>
    /// Mapea código de MemoryType SMBIOS a string (compatibilidad con códigos antiguos y nuevos)
    /// </summary>
    private static string MapearMemoryTypeSMBIOS(int typeCode)
    {
        // Códigos SMBIOS Type 17 para Memory Type
        // 20=DDR, 21=DDR2, 24=DDR3, 26=DDR4, 27=DDR5, 38=LPDDR, 39=LPDDR2, 40=LPDDR3, 41=HBM, 42=HBM2, 43=HBM3
        return typeCode switch
        {
            20 => "DDR",
            21 => "DDR2",
            24 => "DDR3",
            26 => "DDR4",
            27 => "DDR5",
            38 => "LPDDR",
            39 => "LPDDR2",
            40 => "LPDDR3",
            41 => "HBM",
            42 => "HBM2",
            43 => "HBM3",
            _ => "No disponible"
        };
    }

    /// <summary>
    /// Mapea código de FormFactor SMBIOS a string
    /// </summary>
    private static string MapearFormFactorSMBIOS(int code)
    {
        // Códigos SMBIOS Form Factor: 8=DIMM, 9=TSOP, 10=RIMM, 11=SRIMM, 12=SODIMM, 13=SRIMM, 25=LRDIMM, 29=SODIMM 200-pin, 32=SODIMM 144-pin
        return code switch
        {
            8 => "DIMM",
            9 => "TSOP",
            10 => "RIMM",
            11 => "SRIMM",
            12 => "SODIMM",
            13 => "SRIMM",
            25 => "LRDIMM",
            29 => "SODIMM 200-pin",
            32 => "SODIMM 144-pin",
            _ => "No disponible"
        };
    }
}