namespace RamTracking.Helpers;

/// <summary>
/// Utilidades matemáticas para cálculos de memoria RAM.
/// Contiene fórmulas teóricas y derivadas a partir de datos SMBIOS/WMI.
/// </summary>
public static class MemoryMath
    {
        /// <summary>
        /// Calcula el ancho de banda teórico de transferencia de memoria.
        /// Fórmula: MT/s × (DataWidth / 8) × Número de Canales
        /// </summary>
        /// <param name="mtS">Velocidad en MT/s (ej: 3200)</param>
        /// <param name="dataWidth">Ancho de datos en bits (ej: 64 para DDR4 unchannel, 128 para dual-channel)</param>
        /// <param name="channels">Número de canales de memoria (1, 2, 4)</param>
        /// <returns>Ancho de banda en GB/s</returns>
        public static double CalcularAnchoBandaTeorico(int mtS, int dataWidth, int channels)
        {
            // (MT/s) × (bits / 8) → bytes/s → / (1024^3) → GB/s
            // Ejemplo: 3200 × (128 / 8) × 2 = 3200 × 16 × 2 = 102,400 MB/s = 100 GB/s
            // En el equipo de pruebas: 3200 × (64 / 8) × 2 = 51,2 GB/s
            double bandwidthMBs = mtS * (dataWidth / 8.0) * channels;
            return Math.Round(bandwidthMBs / 1024.0, 2); // Convertir a GB/s (usando 1024)
        }

        /// <summary>
        /// Calcula la capacidad máxima teórica de ampliación basándose en los datos SMBIOS.
        /// </summary>
        /// <param name="currentCapacityGiB">Capacidad actual en GiB</param>
        /// <param name="maxCapacityGiB">Capacidad máxima reportada por SMBIOS en GiB</param>
        /// <param name="totalSlots">Número total de slots</param>
        /// <param name="occupiedSlots">Número de slots ocupados</param>
        /// <returns>Tupla con (capacidad_actual, capacidad_maximal,_margen_ampliacion)</returns>
        public static (double current, double maximum, double margin) CalcularMargenAmpliacion(
            double currentCapacityGiB,
            double maxCapacityGiB,
            int totalSlots,
            int occupiedSlots)
        {
            // Capacidad máxima teórica (usando todos los slots al máximo)
            double maximum = maxCapacityGiB * totalSlots;

            // Capacidad actualmente ocupada (estimada)
            double current = currentCapacityGiB;

            // Margen de ampliación disponible (50% de la diferencia entre máximo y actual)
            double margin = 0;
            if (maximum > current)
            {
                margin = Math.Round((maximum - current) * 0.5, 2); // 50% de margen
            }

            return (current, maximum, margin);
        }

        /// <summary>
        /// Determina si la memoria tiene ECC basándose en los width fields de SMBIOS.
        /// ECC está presente cuando TotalWidth > DataWidth.
        /// </summary>
        /// <param name="totalWidth">Total Width en bits (del SMBIOS Memory Device)</param>
        /// <param dataWidth">Data Width en bits (del SMBIOS Memory Device)</param>
        /// <returns>True si la memoria tiene ECC</returns>
        public static bool TieneECC(int totalWidth, int dataWidth)
        {
            return totalWidth > dataWidth;
        }

        /// <summary>
        /// Calcula la capacidad reservada por hardware basándose en la diferencia
        /// entre Installed y Usable reported por el sistema operativo.
        /// </summary>
        /// <param name="installedGiB">Memoria instalada total en GiB</param>
        /// <param name="usableGiB">Memoria usable reportada por el SO en GiB</param>
        /// <returns>Memoria reservada por hardware en GiB</returns>
        public static double CalcularReservaHardware(double installedGiB, double usableGiB)
        {
            return Math.Round(installedGiB - usableGiB, 2);
        }

        /// <summary>
        /// Calcula el rango de rango de velocidad soportada basándose en los límites SMBIOS.
        /// </summary>
        /// <param name="minSpeedMTs">Velocidad mínima configurada en MT/s</param>
        /// <param name="maxSpeedMTs">Velocidad máxima configurada en MT/s</param>
        /// <param name="currentSpeedMTs">Velocidad actualmente configurada en MT/s</param>
        /// <returns>Cadena describiendo el rango y si el actual está dentro</returns>
        public static string RangoVelocidad(int minSpeedMTs, int maxSpeedMTs, int currentSpeedMTs)
        {
            bool dentroRango = currentSpeedMTs >= minSpeedMTs && currentSpeedMTs <= maxSpeedMTs;
            return $"Rango: {minSpeedMTs} - {maxSpeedMTs} MT/s · Actual: {currentSpeedMTs} MT/s {(dentroRango ? "✓" : "✗ fuera de rango")}";
        }
    }