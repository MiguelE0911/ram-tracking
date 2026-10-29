namespace RamTracking.Services;

using System.Management;
using RamTracking.Models;

/// <summary>
/// Servicio para obtener información de memoria del sistema via WMI
/// </summary>
public class MemoryService : IMemoryService
{
    public async Task<MemoryInfo> GetMemoryInfoAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                // Win32_OperatingSystem para memoria disponible y total visible
                using var osSearcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
                var osCollection = osSearcher.Get();
                
                double totalVisibleKB = 0;
                double freePhysicalKB = 0;
                
                foreach (ManagementObject obj in osCollection)
                {
                    totalVisibleKB = Convert.ToDouble(obj["TotalVisibleMemorySize"]);
                    freePhysicalKB = Convert.ToDouble(obj["FreePhysicalMemory"]);
                    break;
                }

                // Win32_ComputerSystem para memoria física total instalada
                using var csSearcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                var csCollection = csSearcher.Get();
                
                double totalPhysicalBytes = 0;
                foreach (ManagementObject obj in csCollection)
                {
                    totalPhysicalBytes = Convert.ToDouble(obj["TotalPhysicalMemory"]);
                    break;
                }

                // Conversiones: KB -> GiB, Bytes -> GiB
                const double kbToGib = 1024.0 * 1024.0;      // 1 GiB = 1024*1024 KB
                const double bytesToGib = 1024.0 * 1024.0 * 1024.0; // 1 GiB = 1024^3 bytes

                double totalGiB = totalPhysicalBytes > 0 
                    ? totalPhysicalBytes / bytesToGib 
                    : totalVisibleKB / kbToGib;
                    
                double usableGiB = totalVisibleKB / kbToGib;
                double availableGiB = freePhysicalKB / kbToGib;
                double usedGiB = usableGiB - availableGiB;
                double usagePercent = usableGiB > 0 ? (usedGiB / usableGiB) * 100.0 : 0;

                // Redondear a 2 decimales para presentación
                return new MemoryInfo(
                    TotalGiB: Math.Round(totalGiB, 2),
                    UsableGiB: Math.Round(usableGiB, 2),
                    UsedGiB: Math.Round(usedGiB, 2),
                    AvailableGiB: Math.Round(availableGiB, 2),
                    UsagePercent: Math.Round(usagePercent, 1)
                );
            }
            catch
            {
                return MemoryInfo.Empty;
            }
        });
    }

    public async Task<SystemInfo> GetSystemInfoAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                string computerName = Environment.MachineName;
                string os = "No disponible";
                string processor = "No disponible";
                string architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";

                // OS info
                using var osSearcher = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in osSearcher.Get())
                {
                    os = $"{obj["Caption"]} {obj["Version"]}".Trim();
                    break;
                }

                // Processor info
                using var cpuSearcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
                foreach (ManagementObject obj in cpuSearcher.Get())
                {
                    processor = obj["Name"]?.ToString()?.Trim() ?? "No disponible";
                    break;
                }

                return new SystemInfo(
                    string.IsNullOrWhiteSpace(computerName) ? "No disponible" : computerName,
                    string.IsNullOrWhiteSpace(os) ? "No disponible" : os,
                    string.IsNullOrWhiteSpace(processor) ? "No disponible" : processor,
                    architecture);
            }
            catch
            {
                return new SystemInfo(
                    string.IsNullOrWhiteSpace(Environment.MachineName) ? "No disponible" : Environment.MachineName,
                    "No disponible",
                    "No disponible",
                    Environment.Is64BitOperatingSystem ? "x64" : "x86");
            }
        });
    }
}