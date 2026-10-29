namespace RamTracking.Models;

/// <summary>
/// Detalles de un slot de memoria individual y del banco físico (arreglo) al que pertenece
/// </summary>
public record MemorySlotDetail(
    MemoryModule Module,            // Módulo seleccionado en la tabla
    string DeviceLocator,           // Posición física: "ChannelA-DIMM0", "BANK 0"
    string ArrayDevice,             // Tag del banco físico: "Physical Memory Array 0"
    double ArrayMaxCapacityGiB,     // Capacidad máxima TOTAL del banco (SMBIOS)
    int SlotsInArray,               // Cantidad de slots del banco con módulo instalado
    double PerSlotCapacityGiB,      // Capacidad máxima estimada por slot (banco ÷ slots ocupados)
    string ArrayErrorCorrection,    // Corrección de errores del banco: Ninguna, ECC, Paridad...
    string ArrayUse,                // Uso del banco: "Memoria del sistema", caché, etc.
    string SupportedType,           // Tipo de RAM soportado (según módulo instalado): DDR4, DDR5...
    string TypeDetail,              // Detalle del tipo: "SDRAM (síncrona), sin buffer (unbuffered)"
    double MinVoltage,              // Voltaje mínimo (V)
    double MaxVoltage,              // Voltaje máximo (V)
    double ConfiguredVoltage        // Voltaje configurado actual (V)
);
