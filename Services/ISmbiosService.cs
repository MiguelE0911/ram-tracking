using RamTracking.Models;

namespace RamTracking.Services;

/// <summary>
/// Servicio para leer información de memoria vía SMBIOS (System Management BIOS)
/// Usa GetSystemFirmwareTable P/Invoke para acceder a tablas SMBIOS raw
/// </summary>
public interface ISmbiosService
{
    /// <summary>
    /// Lee un tipo de estructura SMBIOS específico (ej. Type 17 para memoria, Type 16 para arrays)
    /// </summary>
    /// <param name="type">Tipo de estructura SMBIOS (16 = Physical Memory Array, 17 = Memory Device)</param>
    /// <param name="index">Índice de la estructura (0-based)</param>
    /// <returns>Diccionario con los campos leídos, o null si no hay datos</returns>
    Dictionary<string, object?>? LeerEstructuraSMBIOS(byte type, int index);

    /// <summary>
    /// Intenta leer información de Type 17 (Memory Device) primero, y si falla, usa WMI como fallback
    /// </summary>
    /// <returns>MemoryModule con datos SMBIOS, o null si no hay información disponible</returns>
    MemoryModule? LeerMemoryDeviceSMBIOS();

    /// <summary>
    /// Intenta leer información de Type 16 (Physical Memory Array) primero, y si falla, usa WMI como fallback
    /// </summary>
    /// <returns>MemorySlotDetail con datos del array, o null si no hay información disponible</returns>
    MemorySlotDetail? LeerPhysicalMemoryArraySMBIOS();
}