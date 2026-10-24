namespace RamTracking.Services;

using RamTracking.Models;

public interface IMemoryService
{
    Task<MemoryInfo> GetMemoryInfoAsync();
    Task<SystemInfo> GetSystemInfoAsync();
}